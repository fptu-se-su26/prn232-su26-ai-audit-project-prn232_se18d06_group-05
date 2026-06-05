-- ==========================================================
-- 1. CƠ SỞ (PROFILES)
-- ==========================================================
CREATE TABLE profiles (
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
CREATE TABLE guide_certificates (
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
CREATE TABLE tour_templates (
  id uuid NOT NULL DEFAULT gen_random_uuid(),
  title text NOT NULL,
  description text,
  location text NOT NULL,
  images text[] DEFAULT '{}',
  created_at timestamp with time zone DEFAULT now(),
  CONSTRAINT tour_templates_pkey PRIMARY KEY (id)
);

CREATE TABLE guide_tours (
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

CREATE TABLE tour_availability (
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
CREATE TABLE bookings (
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

CREATE TABLE payments (
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
CREATE TABLE reviews (
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

CREATE TABLE conversations (
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

CREATE TABLE messages (
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