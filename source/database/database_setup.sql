-- ==========================================
-- 1. XÓA CÁC BẢNG CŨ (CASCADE)
-- ==========================================
DROP TABLE IF EXISTS public.chat_messages CASCADE;
DROP TABLE IF EXISTS public.messages CASCADE;
DROP TABLE IF EXISTS public.conversations CASCADE;
DROP TABLE IF EXISTS public.reviews CASCADE;
DROP TABLE IF EXISTS public.ledger_entries CASCADE;
DROP TABLE IF EXISTS public.payments CASCADE;
DROP TABLE IF EXISTS public.bookings CASCADE;
DROP TABLE IF EXISTS public.guide_availability CASCADE;
DROP TABLE IF EXISTS public.tour_availability CASCADE;
DROP TABLE IF EXISTS public.experience_packages CASCADE;
DROP TABLE IF EXISTS public.guide_tours CASCADE;
DROP TABLE IF EXISTS public.tour_templates CASCADE;
DROP TABLE IF EXISTS public.guide_certificates CASCADE;
DROP TABLE IF EXISTS public.guide_profiles CASCADE;
DROP TABLE IF EXISTS public.admin_notifications CASCADE;
DROP TABLE IF EXISTS public.user_personalities CASCADE;
DROP TABLE IF EXISTS public.survey_options CASCADE;
DROP TABLE IF EXISTS public.survey_questions CASCADE;
DROP TABLE IF EXISTS public.profiles CASCADE;


ALTER TABLE public.bookings
DROP COLUMN payment_reference,
DROP COLUMN payment_method;

CREATE EXTENSION IF NOT EXISTS pgcrypto;

-- ==========================================
-- 2. KHỞI TẠO CẤU TRÚC DATABASE THEO SPEC MỚI
-- ==========================================

-- 1. Bảng Profiles (Base User - Identity)
CREATE TABLE public.profiles (
  id uuid NOT NULL,
  email text NOT NULL UNIQUE,
  full_name text NOT NULL,
  role text NOT NULL DEFAULT 'traveler'::text CHECK (role = ANY (ARRAY['traveler'::text, 'guide'::text, 'admin'::text])),
  phone_number text,
  avatar_url text,
  is_active boolean DEFAULT true,
  publication_status text NOT NULL DEFAULT 'published',
  created_at timestamp with time zone DEFAULT now(),
  updated_at timestamp with time zone DEFAULT now(),
  updated_at timestamp with time zone DEFAULT now(),
  CONSTRAINT profiles_pkey PRIMARY KEY (id),
  CONSTRAINT profiles_id_fkey FOREIGN KEY (id) REFERENCES auth.users(id) ON DELETE CASCADE
);

-- 2. Bảng Guide Profiles (1:1 với Profiles)
CREATE TABLE public.guide_profiles (
  id uuid NOT NULL DEFAULT gen_random_uuid(),
  user_id uuid NOT NULL UNIQUE,
  bio text NOT NULL,
  languages text[] DEFAULT '{}'::text[],
  specialties text[] DEFAULT '{}'::text[],
  city_area text,
  price_per_hour numeric(12,2) DEFAULT 0,
  is_verified boolean DEFAULT false,
  verified_at timestamp with time zone,
  average_rating numeric(3,2) DEFAULT 0.00,
  total_reviews integer DEFAULT 0,
  hidden_gems_urls text[] DEFAULT '{}'::text[],
  cover_photo_url text,
  created_at timestamp with time zone DEFAULT now(),
  updated_at timestamp with time zone DEFAULT now(),
  CONSTRAINT guide_profiles_pkey PRIMARY KEY (id),
  CONSTRAINT guide_profiles_user_fkey FOREIGN KEY (user_id) REFERENCES public.profiles(id) ON DELETE CASCADE
);

-- 3. Bảng Chứng chỉ hướng dẫn viên (Dành cho Admin duyệt)
CREATE TABLE public.guide_certificates (
  id uuid NOT NULL DEFAULT gen_random_uuid(),
  guide_profile_id uuid NOT NULL,
  certificate_name text NOT NULL,
  file_url text NOT NULL,
  status text DEFAULT 'pending'::text,
  created_at timestamp with time zone DEFAULT now(),
  CONSTRAINT guide_certificates_pkey PRIMARY KEY (id),
  CONSTRAINT guide_certificates_guide_fkey FOREIGN KEY (guide_profile_id) REFERENCES public.guide_profiles(id) ON DELETE CASCADE
);

-- 4. Bảng Gói Trải Nghiệm (Experience Packages)
CREATE TABLE public.experience_packages (
  id uuid NOT NULL DEFAULT gen_random_uuid(),
  guide_profile_id uuid NOT NULL,
  title text NOT NULL,
  description text NOT NULL,
  duration_hours numeric(4,1) NOT NULL,
  price_per_session numeric(12,2) NOT NULL,
  price_per_person numeric(12,2) DEFAULT 0,
  included_guest_count integer NOT NULL DEFAULT 1,
  max_group_size integer DEFAULT 6,
  included_items text[] DEFAULT '{}'::text[],
  tags text[] DEFAULT '{}'::text[],
  is_active boolean DEFAULT true,
  created_at timestamp with time zone DEFAULT now(),
  CONSTRAINT experience_packages_pkey PRIMARY KEY (id),
  CONSTRAINT exp_pkg_included_guest_count_check CHECK (included_guest_count >= 1),
  CONSTRAINT exp_pkg_max_group_size_check CHECK (max_group_size >= included_guest_count),
  CONSTRAINT exp_pkg_price_check CHECK (price_per_session >= 0 AND COALESCE(price_per_person, 0) >= 0),
  CONSTRAINT exp_pkg_publication_status_check CHECK (publication_status IN ('draft', 'published', 'hidden')),
  CONSTRAINT exp_pkg_guide_fkey FOREIGN KEY (guide_profile_id) REFERENCES public.guide_profiles(id) ON DELETE CASCADE
);

-- 5. Bảng Ngày Nghỉ/Bận (Guide Availability - Blocklist)
CREATE TABLE public.guide_availability (
  id uuid NOT NULL DEFAULT gen_random_uuid(),
  guide_profile_id uuid NOT NULL,
  unavailable_date date NOT NULL,
  reason text,
  created_at timestamp with time zone DEFAULT now(),
  CONSTRAINT guide_availability_pkey PRIMARY KEY (id),
  CONSTRAINT guide_avail_guide_fkey FOREIGN KEY (guide_profile_id) REFERENCES public.guide_profiles(id) ON DELETE CASCADE
);

-- 6. Bảng Bookings (State Machine: 0=Pending, 1=Confirmed, 2=Completed, 3=Cancelled)
CREATE TABLE public.bookings (
  id uuid NOT NULL DEFAULT gen_random_uuid(),
  traveler_id uuid NOT NULL,
  guide_profile_id uuid NOT NULL,
  experience_package_id uuid NOT NULL,
  booking_date date NOT NULL,
  start_time time with time zone NOT NULL,
  guest_count integer DEFAULT 1,
  total_amount numeric(14,2) NOT NULL,
  platform_fee numeric(14,2) NOT NULL,
  guide_earnings numeric(14,2) NOT NULL,
  status smallint NOT NULL DEFAULT 0,
  payment_reference text,
  payment_method text,
  escrow_released boolean DEFAULT false,
  traveler_notes text,
  guide_response_at timestamp with time zone,
  cancel_reason text,
  created_at timestamp with time zone DEFAULT now(),
  updated_at timestamp with time zone DEFAULT now(),
  CONSTRAINT bookings_pkey PRIMARY KEY (id),
  CONSTRAINT bookings_traveler_fkey FOREIGN KEY (traveler_id) REFERENCES public.profiles(id) ON DELETE CASCADE,
  CONSTRAINT bookings_guide_fkey FOREIGN KEY (guide_profile_id) REFERENCES public.guide_profiles(id) ON DELETE CASCADE,
  CONSTRAINT bookings_pkg_fkey FOREIGN KEY (experience_package_id) REFERENCES public.experience_packages(id) ON DELETE CASCADE
);

-- 7. Bảng Lịch Sử Giao Dịch (Ledger Entries - Phục vụ Admin & Guide thu nhập)
CREATE TABLE public.ledger_entries (
  id uuid NOT NULL DEFAULT gen_random_uuid(),
  booking_id uuid NOT NULL,
  user_id uuid NOT NULL,
  type text NOT NULL, -- 'EARNING', 'FEE', 'REFUND'
  amount numeric(14,2) NOT NULL,
  created_at timestamp with time zone DEFAULT now(),
  CONSTRAINT ledger_entries_pkey PRIMARY KEY (id),
  CONSTRAINT ledger_booking_fkey FOREIGN KEY (booking_id) REFERENCES public.bookings(id) ON DELETE CASCADE
);

-- 8. Bảng Tin Nhắn (Chat Messages - BIGSERIAL tối ưu hiệu suất)
CREATE TABLE public.chat_messages (
  id bigint NOT NULL GENERATED ALWAYS AS IDENTITY,
  booking_id uuid NOT NULL,
  sender_id uuid NOT NULL,
  receiver_id uuid NOT NULL,
  message_text text NOT NULL,
  is_read boolean DEFAULT false,
  sent_at timestamp with time zone DEFAULT now(),
  edited_at timestamp with time zone,
  CONSTRAINT chat_messages_pkey PRIMARY KEY (id),
  CONSTRAINT chat_booking_fkey FOREIGN KEY (booking_id) REFERENCES public.bookings(id) ON DELETE CASCADE,
  CONSTRAINT chat_sender_fkey FOREIGN KEY (sender_id) REFERENCES public.profiles(id) ON DELETE CASCADE,
  CONSTRAINT chat_receiver_fkey FOREIGN KEY (receiver_id) REFERENCES public.profiles(id) ON DELETE CASCADE
);

-- 9. Bảng Reviews
CREATE TABLE public.reviews (
  id uuid NOT NULL DEFAULT gen_random_uuid(),
  booking_id uuid NOT NULL UNIQUE,
  guide_profile_id uuid NOT NULL,
  traveler_id uuid NOT NULL,
  rating integer NOT NULL CHECK (rating >= 1 AND rating <= 5),
  comment text,
  created_at timestamp with time zone DEFAULT now(),
  CONSTRAINT reviews_pkey PRIMARY KEY (id),
  CONSTRAINT reviews_booking_fkey FOREIGN KEY (booking_id) REFERENCES public.bookings(id) ON DELETE CASCADE,
  CONSTRAINT reviews_guide_fkey FOREIGN KEY (guide_profile_id) REFERENCES public.guide_profiles(id) ON DELETE CASCADE,
  CONSTRAINT reviews_traveler_fkey FOREIGN KEY (traveler_id) REFERENCES public.profiles(id) ON DELETE CASCADE
);

-- 10-13. Khảo Sát & Thông Báo Admin (Giữ nguyên theo logic cũ của bạn)
CREATE TABLE public.admin_notifications (
  id uuid NOT NULL DEFAULT gen_random_uuid(),
  type text NOT NULL DEFAULT 'guide_application',
  title text NOT NULL,
  message text NOT NULL,
  guide_id uuid,
  guide_name text,
  guide_email text,
  is_read boolean DEFAULT false,
  created_at timestamp with time zone DEFAULT now(),
  read_at timestamp with time zone,
  CONSTRAINT admin_notifications_pkey PRIMARY KEY (id)
);

CREATE TABLE public.survey_questions (
  id uuid NOT NULL DEFAULT gen_random_uuid(),
  question_text text NOT NULL,
  target_role text DEFAULT 'both'::text,
  created_at timestamp with time zone DEFAULT now(),
  CONSTRAINT survey_questions_pkey PRIMARY KEY (id)
);

CREATE TABLE public.survey_options (
  id uuid NOT NULL DEFAULT gen_random_uuid(),
  question_id uuid NOT NULL,
  option_text text NOT NULL,
  personality_tag text NOT NULL,
  score integer DEFAULT 1,
  CONSTRAINT survey_options_pkey PRIMARY KEY (id),
  CONSTRAINT survey_options_question_fkey FOREIGN KEY (question_id) REFERENCES public.survey_questions(id) ON DELETE CASCADE
);

CREATE TABLE public.user_personalities (
  id uuid NOT NULL DEFAULT gen_random_uuid(),
  user_id uuid NOT NULL,
  personality_tag text NOT NULL,
  score numeric NOT NULL,
  updated_at timestamp with time zone DEFAULT now(),
  CONSTRAINT user_personalities_pkey PRIMARY KEY (id),
  CONSTRAINT user_personalities_user_fkey FOREIGN KEY (user_id) REFERENCES public.profiles(id) ON DELETE CASCADE,
  CONSTRAINT user_personalities_user_tag_unique UNIQUE (user_id, personality_tag)
);

CREATE TABLE public.payments (
  id uuid NOT NULL DEFAULT gen_random_uuid(),
  
  booking_id uuid NOT NULL,
  payer_id uuid NOT NULL, -- traveler
  
  amount numeric(14,2) NOT NULL,
  currency text DEFAULT 'VND',
  
  payment_method text NOT NULL, 
  -- 'stripe', 'momo', 'vnpay', 'paypal', 'cash'

  status text NOT NULL DEFAULT 'pending',
  -- pending | succeeded | failed | refunded | cancelled

  provider_transaction_id text, -- id từ cổng thanh toán

  payment_intent text, -- optional (Stripe intent / similar)
  
  paid_at timestamp with time zone,
  
  metadata jsonb DEFAULT '{}'::jsonb,

  created_at timestamp with time zone DEFAULT now(),
  updated_at timestamp with time zone DEFAULT now(),

  CONSTRAINT payments_pkey PRIMARY KEY (id),

  CONSTRAINT payments_booking_fkey
    FOREIGN KEY (booking_id) REFERENCES public.bookings(id) ON DELETE CASCADE,

    FOREIGN KEY (payer_id) REFERENCES public.profiles(id) ON DELETE CASCADE
);

-- ==========================================
-- 15. SAVED GUIDES (M1, M3: Traveler Bookmarks)
-- ==========================================
CREATE TABLE public.saved_guides (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    traveler_id UUID NOT NULL REFERENCES public.profiles(id) ON DELETE CASCADE,
    guide_profile_id UUID NOT NULL REFERENCES public.guide_profiles(id) ON DELETE CASCADE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT timezone('utc'::text, now()) NOT NULL,
    UNIQUE(traveler_id, guide_profile_id)
);

-- ==========================================
-- 3. CẬP NHẬT TRIGGER TẠO USER (AUTH -> PROFILES)
-- ==========================================
CREATE OR REPLACE FUNCTION public.handle_new_user()
RETURNS trigger AS $$
BEGIN
  INSERT INTO public.profiles (id, email, full_name, avatar_url, role, is_active)
  VALUES (
    new.id, 
    new.email, 
    coalesce(new.raw_user_meta_data->>'full_name', 'Người dùng mới'), 
    new.raw_user_meta_data->>'avatar_url', 
    coalesce(new.raw_user_meta_data->>'role', 'traveler'), 
    true
  )
  ON CONFLICT (id) DO UPDATE SET
    full_name = EXCLUDED.full_name,
    role = EXCLUDED.role;
  RETURN NEW;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

DROP TRIGGER IF EXISTS on_auth_user_created ON auth.users;
CREATE TRIGGER on_auth_user_created
  AFTER INSERT ON auth.users
  FOR EACH ROW EXECUTE FUNCTION public.handle_new_user();

-- ==========================================
-- 4. BẬT BẢO MẬT RLS (ROW LEVEL SECURITY)
-- ==========================================

-- Bật tính năng RLS trên toàn bộ các bảng hiện tại
ALTER TABLE public.profiles ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.guide_profiles ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.guide_certificates ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.experience_packages ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.guide_availability ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.bookings ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.ledger_entries ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.chat_messages ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.reviews ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.admin_notifications ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.survey_questions ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.survey_options ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.survey_options ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.user_personalities ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.saved_guides ENABLE ROW LEVEL SECURITY;

-- ------------------------------------------
-- POLICIES CHO BẢNG PROFILES & GUIDE PROFILES
-- ------------------------------------------
CREATE POLICY "Public profiles are viewable by everyone" ON public.profiles FOR SELECT USING (true);
CREATE POLICY "Users can update own profile" ON public.profiles FOR UPDATE USING (auth.uid() = id);
CREATE POLICY "Cho phép tạo profile mới khi đăng ký" ON public.profiles FOR INSERT WITH CHECK (auth.uid() = id);

CREATE POLICY "Public guide profiles viewable by everyone" ON public.guide_profiles FOR SELECT USING (true);
CREATE POLICY "Guides can update own guide profile" ON public.guide_profiles FOR UPDATE USING (auth.uid() = user_id);

-- ------------------------------------------
-- POLICIES CHO GÓI TRẢI NGHIỆM & LỊCH TRỐNG (CÔNG KHAI)
-- ------------------------------------------
CREATE POLICY "experience_packages_select_public" ON public.experience_packages FOR SELECT USING (true);
CREATE POLICY "guide_availability_select_public" ON public.guide_availability FOR SELECT USING (true);

-- Guide chỉ được quản lý (Thêm/Sửa/Xóa) gói tour và lịch của chính mình
CREATE POLICY "guide_manage_own_packages" ON public.experience_packages FOR ALL USING (
  EXISTS (SELECT 1 FROM public.guide_profiles gp WHERE gp.id = experience_packages.guide_profile_id AND gp.user_id = auth.uid())
);

CREATE POLICY "guide_manage_own_availability" ON public.guide_availability FOR ALL USING (
  EXISTS (SELECT 1 FROM public.guide_profiles gp WHERE gp.id = guide_availability.guide_profile_id AND gp.user_id = auth.uid())
);

-- ------------------------------------------
-- POLICIES CHO ĐẶT TOUR (BOOKINGS)
-- ------------------------------------------
CREATE POLICY "Khách hàng tự xem booking, Guide xem booking của họ" 
  ON public.bookings FOR SELECT USING (
    auth.uid() = traveler_id OR 
    EXISTS (
      SELECT 1 FROM public.guide_profiles gp 
      WHERE gp.id = bookings.guide_profile_id AND gp.user_id = auth.uid()
    )
  );

CREATE POLICY "Chỉ Traveler mới có quyền đặt tour" 
  ON public.bookings FOR INSERT WITH CHECK (auth.uid() = traveler_id);

CREATE POLICY "Guide có quyền cập nhật trạng thái booking"
  ON public.bookings FOR UPDATE USING (
    EXISTS (
      SELECT 1 FROM public.guide_profiles gp 
      WHERE gp.id = bookings.guide_profile_id AND gp.user_id = auth.uid()
    ) OR auth.uid() = traveler_id
  );

-- ------------------------------------------
-- POLICIES CHO PHẦN TIN NHẮN (CHAT)
-- ------------------------------------------
CREATE POLICY "Chat participants can view and send messages" 
  ON public.chat_messages FOR ALL USING (auth.uid() = sender_id OR auth.uid() = receiver_id);

-- ------------------------------------------
-- POLICIES CHO KHẢO SÁT TÍNH CÁCH (SURVEY)
-- ------------------------------------------
CREATE POLICY "Mọi người đăng nhập đều được xem câu hỏi và đáp án khảo sát" 
  ON public.survey_questions FOR SELECT USING (auth.role() = 'authenticated');
CREATE POLICY "Mọi người đăng nhập đều được xem câu trả lời lựa chọn" 
  ON public.survey_options FOR SELECT USING (auth.role() = 'authenticated');
CREATE POLICY "User được xem kết quả tính cách của mình và người khác để so khớp" 
  ON public.user_personalities FOR SELECT USING (true);
CREATE POLICY "User chỉ được điền/cập nhật kết quả tính cách của chính họ" 
  ON public.user_personalities FOR ALL USING (auth.uid() = user_id);

-- ------------------------------------------
-- POLICIES CHO SAVED GUIDES
-- ------------------------------------------
CREATE POLICY "Travelers can view their own saved guides" ON public.saved_guides FOR SELECT USING (auth.uid() = traveler_id);
CREATE POLICY "Travelers can insert their own saved guides" ON public.saved_guides FOR INSERT WITH CHECK (auth.uid() = traveler_id);
CREATE POLICY "Travelers can delete their own saved guides" ON public.saved_guides FOR DELETE USING (auth.uid() = traveler_id);

-- ------------------------------------------
-- POLICIES CHO ADMIN (NOTIFICATIONS & LEDGER)
-- ------------------------------------------
CREATE POLICY "Chỉ admin mới thao tác admin notifications" ON public.admin_notifications FOR ALL USING (
  EXISTS (SELECT 1 FROM public.profiles WHERE id = auth.uid() AND role = 'admin')
);

CREATE POLICY "Admin xem toàn bộ ledger, Guide chỉ xem của mình" ON public.ledger_entries FOR SELECT USING (
  EXISTS (SELECT 1 FROM public.profiles WHERE id = auth.uid() AND role = 'admin') OR auth.uid() = user_id
);


-- ==========================================
-- 5. GRANT PERMISSIONS (QUAN TRỌNG)
-- ==========================================

-- Public tables (anon + authenticated có thể đọc)
GRANT SELECT ON public.profiles TO anon, authenticated;
GRANT SELECT ON public.guide_profiles TO anon, authenticated;
GRANT SELECT ON public.experience_packages TO anon, authenticated;
GRANT SELECT ON public.guide_availability TO anon, authenticated;
GRANT SELECT ON public.reviews TO anon, authenticated;
GRANT SELECT ON public.survey_questions TO anon, authenticated;
GRANT SELECT ON public.survey_options TO anon, authenticated;

-- Guide management (authenticated only)
GRANT INSERT, UPDATE, DELETE ON public.experience_packages TO authenticated;
GRANT INSERT, UPDATE, DELETE ON public.guide_availability TO authenticated;
GRANT SELECT, INSERT ON public.guide_certificates TO authenticated;

-- Bookings & Chat & Reviews (authenticated only)
GRANT SELECT, INSERT, UPDATE ON public.bookings TO authenticated;
GRANT SELECT, INSERT, UPDATE, DELETE ON public.chat_messages TO authenticated;
GRANT INSERT, UPDATE, DELETE ON public.reviews TO authenticated;

-- Survey/personality (authenticated only)
GRANT SELECT, INSERT, UPDATE, DELETE ON public.user_personalities TO authenticated;

-- Admin & Ledger (authenticated only)
GRANT SELECT, INSERT, UPDATE ON public.admin_notifications TO authenticated;
GRANT SELECT, INSERT ON public.ledger_entries TO authenticated;

-- Saved Guides (authenticated only)
GRANT SELECT, INSERT, DELETE ON public.saved_guides TO authenticated;
