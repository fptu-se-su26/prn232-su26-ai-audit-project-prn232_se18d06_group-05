-- ============================================
-- TRIPMATE DATABASE SETUP - UPDATED SCHEMA
-- ============================================
-- Run this script in Supabase SQL Editor
-- Dashboard: https://supabase.com/dashboard/project/nvbvvowyjzylllswhynv/editor
-- ============================================

-- ==========================================================
-- 1. CƠ SỞ (PROFILES)
-- ==========================================================
CREATE TABLE IF NOT EXISTS profiles (
  id uuid NOT NULL,
  email text NOT NULL UNIQUE,
  full_name text,
  phone text,
  avatar_url text,
  role text DEFAULT 'traveler'::text CHECK (role = ANY (ARRAY['traveler'::text, 'guide'::text, 'admin'::text])),
  created_at timestamp with time zone DEFAULT now(),
  updated_at timestamp with time zone DEFAULT now(),
  CONSTRAINT profiles_pkey PRIMARY KEY (id),
  CONSTRAINT profiles_id_fkey FOREIGN KEY (id) REFERENCES auth.users(id) ON DELETE CASCADE
);

-- ==========================================================
-- 2. DỊCH VỤ HƯỚNG DẪN VIÊN (CERTIFICATES)
-- ==========================================================
CREATE TABLE IF NOT EXISTS guide_certificates (
  id uuid NOT NULL DEFAULT gen_random_uuid(),
  guide_id uuid NOT NULL,
  certificate_name text NOT NULL,
  file_url text NOT NULL,
  status text DEFAULT 'pending'::text CHECK (status = ANY (ARRAY['pending'::text, 'verified'::text, 'rejected'::text])),
  created_at timestamp with time zone DEFAULT now(),
  CONSTRAINT guide_certificates_pkey PRIMARY KEY (id),
  CONSTRAINT guide_certificates_guide_id_fkey FOREIGN KEY (guide_id) REFERENCES profiles(id) ON DELETE CASCADE
);

-- ==========================================================
-- 3. CẤU TRÚC TOUR (TEMPLATES & GUIDE TOURS)
-- ==========================================================
CREATE TABLE IF NOT EXISTS tour_templates (
  id uuid NOT NULL DEFAULT gen_random_uuid(),
  title text NOT NULL,
  description text,
  location text NOT NULL,
  images text[] DEFAULT '{}',
  created_at timestamp with time zone DEFAULT now(),
  CONSTRAINT tour_templates_pkey PRIMARY KEY (id)
);

CREATE TABLE IF NOT EXISTS guide_tours (
  id uuid NOT NULL DEFAULT gen_random_uuid(),
  tour_template_id uuid NOT NULL,
  guide_id uuid NOT NULL,
  price numeric NOT NULL CHECK (price >= 0),
  duration_hours integer NOT NULL CHECK (duration_hours > 0),
  max_participants integer DEFAULT 10,
  status text DEFAULT 'active'::text CHECK (status = ANY (ARRAY['active'::text, 'inactive'::text])),
  rating numeric DEFAULT 0,
  total_reviews integer DEFAULT 0,
  CONSTRAINT guide_tours_pkey PRIMARY KEY (id),
  CONSTRAINT guide_tours_template_fkey FOREIGN KEY (tour_template_id) REFERENCES tour_templates(id) ON DELETE CASCADE,
  CONSTRAINT guide_tours_guide_fkey FOREIGN KEY (guide_id) REFERENCES profiles(id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS tour_availability (
  id uuid NOT NULL DEFAULT gen_random_uuid(),
  guide_tour_id uuid NOT NULL,
  date date NOT NULL,
  remaining_slots integer NOT NULL,
  CONSTRAINT tour_availability_pkey PRIMARY KEY (id),
  CONSTRAINT tour_availability_guide_tour_id_fkey FOREIGN KEY (guide_tour_id) REFERENCES guide_tours(id) ON DELETE CASCADE
);

-- ==========================================================
-- 4. GIAO DỊCH (BOOKINGS & PAYMENTS)
-- ==========================================================
CREATE TABLE IF NOT EXISTS bookings (
  id uuid NOT NULL DEFAULT gen_random_uuid(),
  guide_tour_id uuid NOT NULL,
  traveler_id uuid NOT NULL,
  tour_date date NOT NULL,
  guests integer NOT NULL DEFAULT 1 CHECK (guests >= 1),
  total_price numeric NOT NULL,
  status text DEFAULT 'pending'::text CHECK (status = ANY (ARRAY['pending'::text, 'confirmed'::text, 'completed'::text, 'cancelled'::text])),
  created_at timestamp with time zone DEFAULT now(),
  CONSTRAINT bookings_pkey PRIMARY KEY (id),
  CONSTRAINT bookings_guide_tour_id_fkey FOREIGN KEY (guide_tour_id) REFERENCES guide_tours(id),
  CONSTRAINT bookings_traveler_id_fkey FOREIGN KEY (traveler_id) REFERENCES profiles(id)
);

CREATE TABLE IF NOT EXISTS payments (
  id uuid NOT NULL DEFAULT gen_random_uuid(),
  booking_id uuid NOT NULL UNIQUE,
  amount numeric NOT NULL,
  payment_method text NOT NULL,
  status text DEFAULT 'pending'::text,
  created_at timestamp with time zone DEFAULT now(),
  CONSTRAINT payments_pkey PRIMARY KEY (id),
  CONSTRAINT payments_booking_id_fkey FOREIGN KEY (booking_id) REFERENCES bookings(id) ON DELETE CASCADE
);

-- ==========================================================
-- 5. TƯƠNG TÁC (REVIEWS & MESSAGING)
-- ==========================================================
CREATE TABLE IF NOT EXISTS reviews (
  id uuid NOT NULL DEFAULT gen_random_uuid(),
  guide_tour_id uuid NOT NULL,
  user_id uuid NOT NULL,
  booking_id uuid UNIQUE,
  rating integer NOT NULL CHECK (rating >= 1 AND rating <= 5),
  comment text,
  created_at timestamp with time zone DEFAULT now(),
  CONSTRAINT reviews_pkey PRIMARY KEY (id),
  CONSTRAINT reviews_guide_tour_id_fkey FOREIGN KEY (guide_tour_id) REFERENCES guide_tours(id),
  CONSTRAINT reviews_user_id_fkey FOREIGN KEY (user_id) REFERENCES profiles(id),
  CONSTRAINT reviews_booking_id_fkey FOREIGN KEY (booking_id) REFERENCES bookings(id)
);

CREATE TABLE IF NOT EXISTS conversations (
  id uuid NOT NULL DEFAULT gen_random_uuid(),
  traveler_id uuid NOT NULL,
  guide_id uuid NOT NULL,
  booking_id uuid UNIQUE,
  created_at timestamp with time zone DEFAULT now(),
  CONSTRAINT conversations_pkey PRIMARY KEY (id),
  CONSTRAINT conversations_traveler_id_fkey FOREIGN KEY (traveler_id) REFERENCES profiles(id),
  CONSTRAINT conversations_guide_id_fkey FOREIGN KEY (guide_id) REFERENCES profiles(id),
  CONSTRAINT conversations_booking_id_fkey FOREIGN KEY (booking_id) REFERENCES bookings(id) ON DELETE SET NULL
);

CREATE TABLE IF NOT EXISTS messages (
  id uuid NOT NULL DEFAULT gen_random_uuid(),
  conversation_id uuid NOT NULL,
  sender_id uuid NOT NULL,
  content text NOT NULL,
  is_read boolean DEFAULT false,
  created_at timestamp with time zone DEFAULT now(),
  CONSTRAINT messages_pkey PRIMARY KEY (id),
  CONSTRAINT messages_conversation_id_fkey FOREIGN KEY (conversation_id) REFERENCES conversations(id) ON DELETE CASCADE,
  CONSTRAINT messages_sender_id_fkey FOREIGN KEY (sender_id) REFERENCES profiles(id)
);

-- ==========================================================
-- 6. TRIGGER TỰ ĐỘNG HÓA
-- ==========================================================

-- Trigger tạo conversation tự động khi booking được confirm
CREATE OR REPLACE FUNCTION create_conversation_on_confirm()
RETURNS TRIGGER AS $$
BEGIN
  IF NEW.status = 'confirmed' THEN
    INSERT INTO conversations (traveler_id, guide_id, booking_id)
    SELECT NEW.traveler_id, gt.guide_id, NEW.id
    FROM guide_tours gt
    WHERE gt.id = NEW.guide_tour_id
    ON CONFLICT (booking_id) DO NOTHING;
  END IF;
  RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_auto_create_conversation
  AFTER UPDATE OF status ON bookings
  FOR EACH ROW
  EXECUTE FUNCTION create_conversation_on_confirm();

-- Trigger cập nhật rating cho guide_tours khi có review mới
CREATE OR REPLACE FUNCTION update_tour_rating()
RETURNS TRIGGER AS $$
BEGIN
  UPDATE guide_tours 
  SET 
    rating = (
      SELECT AVG(rating)::numeric 
      FROM reviews 
      WHERE guide_tour_id = NEW.guide_tour_id
    ),
    total_reviews = (
      SELECT COUNT(*) 
      FROM reviews 
      WHERE guide_tour_id = NEW.guide_tour_id
    )
  WHERE id = NEW.guide_tour_id;
  
  RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_update_tour_rating
  AFTER INSERT OR UPDATE OR DELETE ON reviews
  FOR EACH ROW
  EXECUTE FUNCTION update_tour_rating();

-- ==========================================================
-- 7. RLS (ROW LEVEL SECURITY) POLICIES
-- ==========================================================

-- Enable RLS
ALTER TABLE profiles ENABLE ROW LEVEL SECURITY;
ALTER TABLE guide_certificates ENABLE ROW LEVEL SECURITY;
ALTER TABLE tour_templates ENABLE ROW LEVEL SECURITY;
ALTER TABLE guide_tours ENABLE ROW LEVEL SECURITY;
ALTER TABLE tour_availability ENABLE ROW LEVEL SECURITY;
ALTER TABLE bookings ENABLE ROW LEVEL SECURITY;
ALTER TABLE payments ENABLE ROW LEVEL SECURITY;
ALTER TABLE reviews ENABLE ROW LEVEL SECURITY;
ALTER TABLE conversations ENABLE ROW LEVEL SECURITY;
ALTER TABLE messages ENABLE ROW LEVEL SECURITY;

-- Profiles policies
CREATE POLICY "Public profiles are viewable by everyone" ON profiles FOR SELECT USING (true);
CREATE POLICY "Users can insert their own profile" ON profiles FOR INSERT WITH CHECK (auth.uid() = id);
CREATE POLICY "Users can update own profile" ON profiles FOR UPDATE USING (auth.uid() = id);

-- Guide tours policies
CREATE POLICY "Tours are viewable by everyone" ON guide_tours FOR SELECT USING (true);
CREATE POLICY "Guides can insert their own tours" ON guide_tours FOR INSERT WITH CHECK (auth.uid() = guide_id);
CREATE POLICY "Guides can update their own tours" ON guide_tours FOR UPDATE USING (auth.uid() = guide_id);

-- Bookings policies
CREATE POLICY "Users can view their own bookings" ON bookings FOR SELECT USING (
  auth.uid() = traveler_id OR 
  auth.uid() IN (SELECT guide_id FROM guide_tours WHERE id = guide_tour_id)
);
CREATE POLICY "Users can insert their own bookings" ON bookings FOR INSERT WITH CHECK (auth.uid() = traveler_id);
CREATE POLICY "Users can update their own bookings" ON bookings FOR UPDATE USING (
  auth.uid() = traveler_id OR 
  auth.uid() IN (SELECT guide_id FROM guide_tours WHERE id = guide_tour_id)
);

-- Reviews policies
CREATE POLICY "Reviews are viewable by everyone" ON reviews FOR SELECT USING (true);
CREATE POLICY "Users can insert their own reviews" ON reviews FOR INSERT WITH CHECK (auth.uid() = user_id);
CREATE POLICY "Users can update their own reviews" ON reviews FOR UPDATE USING (auth.uid() = user_id);

-- Messages policies
CREATE POLICY "Users can view their own messages" ON messages FOR SELECT USING (
  auth.uid() = sender_id OR 
  auth.uid() IN (
    SELECT traveler_id FROM conversations WHERE id = conversation_id
    UNION
    SELECT guide_id FROM conversations WHERE id = conversation_id
  )
);
CREATE POLICY "Users can insert their own messages" ON messages FOR INSERT WITH CHECK (auth.uid() = sender_id);

-- ==========================================================
-- 8. SAMPLE DATA (OPTIONAL)
-- ==========================================================

-- Insert sample tour templates
INSERT INTO tour_templates (title, description, location, images) VALUES
('Khám phá Phố cổ Hà Nội', 'Tham quan các di tích lịch sử và thưởng thức ẩm thực đường phố', 'Hà Nội', ARRAY['https://example.com/hanoi1.jpg', 'https://example.com/hanoi2.jpg']),
('Tour Vịnh Hạ Long', 'Du thuyền qua các đảo đá vôi tuyệt đẹp', 'Quảng Ninh', ARRAY['https://example.com/halong1.jpg']),
('Phố đi bộ Nguyễn Huệ', 'Khám phá trung tâm Sài Gòn về đêm', 'TP.HCM', ARRAY['https://example.com/saigon1.jpg']),
('Hội An cổ kính', 'Tham quan phố cổ và làm đèn lồng', 'Hội An', ARRAY['https://example.com/hoian1.jpg'])
ON CONFLICT DO NOTHING;
  notes TEXT,
  created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
  updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- 1.4 Reviews Table
CREATE TABLE IF NOT EXISTS reviews (
  id UUID DEFAULT gen_random_uuid() PRIMARY KEY,
  tour_id UUID REFERENCES tours(id) ON DELETE CASCADE NOT NULL,
  user_id UUID REFERENCES profiles(id) ON DELETE CASCADE NOT NULL,
  booking_id UUID REFERENCES bookings(id) ON DELETE CASCADE,
  rating INTEGER NOT NULL CHECK (rating >= 1 AND rating <= 5),
  comment TEXT,
  created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
  updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
  UNIQUE(booking_id)
);

-- ============================================
-- 2. ENABLE ROW LEVEL SECURITY
-- ============================================

ALTER TABLE profiles ENABLE ROW LEVEL SECURITY;
ALTER TABLE tours ENABLE ROW LEVEL SECURITY;
ALTER TABLE bookings ENABLE ROW LEVEL SECURITY;
ALTER TABLE reviews ENABLE ROW LEVEL SECURITY;

-- ============================================
-- 3. CREATE POLICIES
-- ============================================

-- 3.1 Profiles Policies
CREATE POLICY "Public profiles are viewable by everyone"
  ON profiles FOR SELECT
  USING (true);

CREATE POLICY "Users can update own profile"
  ON profiles FOR UPDATE
  USING (auth.uid() = id);

CREATE POLICY "Users can insert own profile"
  ON profiles FOR INSERT
  WITH CHECK (auth.uid() = id);

-- 3.2 Tours Policies
CREATE POLICY "Tours are viewable by everyone"
  ON tours FOR SELECT
  USING (status = 'active');

CREATE POLICY "Guides can create tours"
  ON tours FOR INSERT
  WITH CHECK (auth.uid() = guide_id AND (
    SELECT role FROM profiles WHERE id = auth.uid()
  ) IN ('guide', 'admin'));

CREATE POLICY "Guides can update own tours"
  ON tours FOR UPDATE
  USING (auth.uid() = guide_id);

CREATE POLICY "Guides can delete own tours"
  ON tours FOR DELETE
  USING (auth.uid() = guide_id);

-- 3.3 Bookings Policies
CREATE POLICY "Users can view own bookings"
  ON bookings FOR SELECT
  USING (auth.uid() = user_id OR auth.uid() IN (
    SELECT guide_id FROM tours WHERE id = tour_id
  ));

CREATE POLICY "Users can create bookings"
  ON bookings FOR INSERT
  WITH CHECK (auth.uid() = user_id);

CREATE POLICY "Users can update own bookings"
  ON bookings FOR UPDATE
  USING (auth.uid() = user_id);

CREATE POLICY "Guides can update tour bookings"
  ON bookings FOR UPDATE
  USING (auth.uid() IN (
    SELECT guide_id FROM tours WHERE id = tour_id
  ));

-- 3.4 Reviews Policies
CREATE POLICY "Reviews are viewable by everyone"
  ON reviews FOR SELECT
  USING (true);

CREATE POLICY "Users can create reviews for completed bookings"
  ON reviews FOR INSERT
  WITH CHECK (
    auth.uid() = user_id AND
    EXISTS (
      SELECT 1 FROM bookings
      WHERE id = booking_id
      AND user_id = auth.uid()
      AND status = 'completed'
    )
  );

CREATE POLICY "Users can update own reviews"
  ON reviews FOR UPDATE
  USING (auth.uid() = user_id);

-- ============================================
-- 4. CREATE FUNCTIONS
-- ============================================

-- 4.1 Function to update updated_at timestamp
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
  NEW.updated_at = NOW();
  RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- 4.2 Function to create profile on signup
CREATE OR REPLACE FUNCTION public.handle_new_user()
RETURNS TRIGGER AS $$
BEGIN
  INSERT INTO public.profiles (id, email, full_name)
  VALUES (
    NEW.id,
    NEW.email,
    NEW.raw_user_meta_data->>'full_name'
  );
  RETURN NEW;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- 4.3 Function to update tour rating
CREATE OR REPLACE FUNCTION update_tour_rating()
RETURNS TRIGGER AS $$
BEGIN
  UPDATE tours
  SET
    rating = (
      SELECT AVG(rating)::DECIMAL(3,2)
      FROM reviews
      WHERE tour_id = COALESCE(NEW.tour_id, OLD.tour_id)
    ),
    total_reviews = (
      SELECT COUNT(*)
      FROM reviews
      WHERE tour_id = COALESCE(NEW.tour_id, OLD.tour_id)
    )
  WHERE id = COALESCE(NEW.tour_id, OLD.tour_id);
  RETURN COALESCE(NEW, OLD);
END;
$$ LANGUAGE plpgsql;

-- ============================================
-- 5. CREATE TRIGGERS
-- ============================================

-- 5.1 Triggers for updated_at
CREATE TRIGGER update_profiles_updated_at
  BEFORE UPDATE ON profiles
  FOR EACH ROW
  EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_tours_updated_at
  BEFORE UPDATE ON tours
  FOR EACH ROW
  EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_bookings_updated_at
  BEFORE UPDATE ON bookings
  FOR EACH ROW
  EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_reviews_updated_at
  BEFORE UPDATE ON reviews
  FOR EACH ROW
  EXECUTE FUNCTION update_updated_at_column();

-- 5.2 Trigger to create profile on signup
CREATE TRIGGER on_auth_user_created
  AFTER INSERT ON auth.users
  FOR EACH ROW
  EXECUTE FUNCTION public.handle_new_user();

-- 5.3 Trigger to update tour rating
CREATE TRIGGER update_tour_rating_on_review
  AFTER INSERT OR UPDATE OR DELETE ON reviews
  FOR EACH ROW
  EXECUTE FUNCTION update_tour_rating();

-- ============================================
-- 6. ENABLE REALTIME (Optional)
-- ============================================

-- Enable realtime for bookings table
ALTER PUBLICATION supabase_realtime ADD TABLE bookings;

-- ============================================
-- SETUP COMPLETE!
-- ============================================
-- Next steps:
-- 1. Test authentication by signing up a new user
-- 2. Check if profile is auto-created
-- 3. Verify RLS policies are working
-- ============================================
