-- ==========================================
-- 1. XÓA CÁC BẢNG CŨ (DROP TABLES CŨ)
-- ==========================================
DROP TABLE IF EXISTS public.messages CASCADE;
DROP TABLE IF EXISTS public.conversations CASCADE;
DROP TABLE IF EXISTS public.reviews CASCADE;
DROP TABLE IF EXISTS public.payments CASCADE;
DROP TABLE IF EXISTS public.bookings CASCADE;
DROP TABLE IF EXISTS public.tour_availability CASCADE;
DROP TABLE IF EXISTS public.guide_tours CASCADE;
DROP TABLE IF EXISTS public.tour_templates CASCADE;
DROP TABLE IF EXISTS public.guide_certificates CASCADE;
DROP TABLE IF EXISTS public.user_personalities CASCADE;
DROP TABLE IF EXISTS public.survey_options CASCADE;
DROP TABLE IF EXISTS public.survey_questions CASCADE;
DROP TABLE IF EXISTS public.profiles CASCADE;

-- Bật extension pgcrypto để phục vụ mã hóa mật khẩu tự động cho Supabase Auth
CREATE EXTENSION IF NOT EXISTS pgcrypto;

-- ==========================================
-- 2. KHỞI TẠO CẤU TRÚC DATABASE MỚI (TỐI ƯU)
-- ==========================================

-- Bảng Profiles (Đã dọn dẹp, đồng bộ thông tin)
CREATE TABLE public.profiles (
  id uuid NOT NULL,
  email text NOT NULL UNIQUE,
  full_name text,
  phone text,
  avatar_url text,
  role text DEFAULT 'traveler'::text CHECK (role = ANY (ARRAY['traveler'::text, 'guide'::text, 'admin'::text])),
  experience text,
  specialization text,
  languages text,
  bio text,
  status text DEFAULT 'active'::text,
  created_at timestamp with time zone DEFAULT now(),
  updated_at timestamp with time zone DEFAULT now(),
  CONSTRAINT profiles_pkey PRIMARY KEY (id),
  CONSTRAINT profiles_id_fkey FOREIGN KEY (id) REFERENCES auth.users(id) ON DELETE CASCADE
);

-- Bảng Chứng chỉ hướng dẫn viên
CREATE TABLE public.guide_certificates (
  id uuid NOT NULL DEFAULT gen_random_uuid(),
  guide_id uuid NOT NULL,
  certificate_name text NOT NULL,
  file_url text NOT NULL,
  status text DEFAULT 'pending'::text CHECK (status = ANY (ARRAY['pending'::text, 'verified'::text, 'rejected'::text])),
  created_at timestamp with time zone DEFAULT now(),
  CONSTRAINT guide_certificates_pkey PRIMARY KEY (id),
  CONSTRAINT guide_certificates_guide_id_fkey FOREIGN KEY (guide_id) REFERENCES public.profiles(id) ON DELETE CASCADE
);

-- Bảng Khung mẫu Tour (Admin tạo)
CREATE TABLE public.tour_templates (
  id uuid NOT NULL DEFAULT gen_random_uuid(),
  title text NOT NULL,
  description text,
  location text NOT NULL,
  images text[] DEFAULT '{}'::text[],
  created_at timestamp with time zone DEFAULT now(),
  CONSTRAINT tour_templates_pkey PRIMARY KEY (id)
);

-- Bảng Chi tiết Tour của từng Guide bán
CREATE TABLE public.guide_tours (
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
  CONSTRAINT guide_tours_template_fkey FOREIGN KEY (tour_template_id) REFERENCES public.tour_templates(id) ON DELETE CASCADE,
  CONSTRAINT guide_tours_guide_fkey FOREIGN KEY (guide_id) REFERENCES public.profiles(id) ON DELETE CASCADE
);

-- Bảng Lịch trình & Chỗ trống (Quản lý theo ngày)
CREATE TABLE public.tour_availability (
  id uuid NOT NULL DEFAULT gen_random_uuid(),
  guide_tour_id uuid NOT NULL,
  date date NOT NULL,
  remaining_slots integer NOT NULL,
  CONSTRAINT tour_availability_pkey PRIMARY KEY (id),
  CONSTRAINT tour_availability_guide_tour_id_fkey FOREIGN KEY (guide_tour_id) REFERENCES public.guide_tours(id) ON DELETE CASCADE
);

-- Bảng Đặt Tour (Liên kết chặt chẽ với ngày trống qua tour_availability_id)
CREATE TABLE public.bookings (
  id uuid NOT NULL DEFAULT gen_random_uuid(),
  tour_availability_id uuid NOT NULL,
  traveler_id uuid NOT NULL,
  guests integer NOT NULL DEFAULT 1 CHECK (guests >= 1),
  total_price numeric NOT NULL,
  status text DEFAULT 'pending'::text CHECK (status = ANY (ARRAY['pending'::text, 'confirmed'::text, 'completed'::text, 'cancelled'::text])),
  note text,
  created_at timestamp with time zone DEFAULT now(),
  CONSTRAINT bookings_pkey PRIMARY KEY (id),
  CONSTRAINT bookings_availability_fkey FOREIGN KEY (tour_availability_id) REFERENCES public.tour_availability(id) ON DELETE CASCADE,
  CONSTRAINT bookings_traveler_id_fkey FOREIGN KEY (traveler_id) REFERENCES public.profiles(id) ON DELETE CASCADE
);

-- Bảng Thanh toán
CREATE TABLE public.payments (
  id uuid NOT NULL DEFAULT gen_random_uuid(),
  booking_id uuid NOT NULL UNIQUE,
  amount numeric NOT NULL,
  payment_method text NOT NULL,
  status text DEFAULT 'pending'::text,
  created_at timestamp with time zone DEFAULT now(),
  CONSTRAINT payments_pkey PRIMARY KEY (id),
  CONSTRAINT payments_booking_id_fkey FOREIGN KEY (booking_id) REFERENCES public.bookings(id) ON DELETE CASCADE
);

-- Bảng Đánh giá chất lượng (Đồng bộ cột traveler_id)
CREATE TABLE public.reviews (
  id uuid NOT NULL DEFAULT gen_random_uuid(),
  guide_tour_id uuid NOT NULL,
  traveler_id uuid NOT NULL,
  booking_id uuid UNIQUE,
  rating integer NOT NULL CHECK (rating >= 1 AND rating <= 5),
  comment text,
  created_at timestamp with time zone DEFAULT now(),
  CONSTRAINT reviews_pkey PRIMARY KEY (id),
  CONSTRAINT reviews_guide_tour_id_fkey FOREIGN KEY (guide_tour_id) REFERENCES public.guide_tours(id) ON DELETE CASCADE,
  CONSTRAINT reviews_traveler_id_fkey FOREIGN KEY (traveler_id) REFERENCES public.profiles(id) ON DELETE CASCADE,
  CONSTRAINT reviews_booking_id_fkey FOREIGN KEY (booking_id) REFERENCES public.bookings(id) ON DELETE SET NULL
);

-- Bảng Cuộc hội thoại
CREATE TABLE public.conversations (
  id uuid NOT NULL DEFAULT gen_random_uuid(),
  traveler_id uuid NOT NULL,
  guide_id uuid NOT NULL,
  booking_id uuid UNIQUE,
  created_at timestamp with time zone DEFAULT now(),
  CONSTRAINT conversations_pkey PRIMARY KEY (id),
  CONSTRAINT conversations_traveler_id_fkey FOREIGN KEY (traveler_id) REFERENCES public.profiles(id) ON DELETE CASCADE,
  CONSTRAINT conversations_guide_id_fkey FOREIGN KEY (guide_id) REFERENCES public.profiles(id) ON DELETE CASCADE,
  CONSTRAINT conversations_booking_id_fkey FOREIGN KEY (booking_id) REFERENCES public.bookings(id) ON DELETE SET NULL
);

-- Bảng Tin nhắn chi tiết
CREATE TABLE public.messages (
  id uuid NOT NULL DEFAULT gen_random_uuid(),
  conversation_id uuid NOT NULL,
  sender_id uuid NOT NULL,
  content text NOT NULL,
  is_read boolean DEFAULT false,
  created_at timestamp with time zone DEFAULT now(),
  CONSTRAINT messages_pkey PRIMARY KEY (id),
  CONSTRAINT messages_conversation_id_fkey FOREIGN KEY (conversation_id) REFERENCES public.conversations(id) ON DELETE CASCADE,
  CONSTRAINT messages_sender_id_fkey FOREIGN KEY (sender_id) REFERENCES public.profiles(id) ON DELETE CASCADE
);

-- Bảng Khảo sát: Câu hỏi
CREATE TABLE public.survey_questions (
  id uuid NOT NULL DEFAULT gen_random_uuid(),
  question_text text NOT NULL,
  target_role text DEFAULT 'both'::text CHECK (target_role = ANY (ARRAY['traveler'::text, 'guide'::text, 'both'::text])),
  created_at timestamp with time zone DEFAULT now(),
  CONSTRAINT survey_questions_pkey PRIMARY KEY (id)
);

-- Bảng Khảo sát: Các phương án lựa chọn
CREATE TABLE public.survey_options (
  id uuid NOT NULL DEFAULT gen_random_uuid(),
  question_id uuid NOT NULL,
  option_text text NOT NULL,
  personality_tag text NOT NULL,
  score integer DEFAULT 1,
  CONSTRAINT survey_options_pkey PRIMARY KEY (id),
  CONSTRAINT survey_options_question_fkey FOREIGN KEY (question_id) REFERENCES public.survey_questions(id) ON DELETE CASCADE
);

-- Bảng Khảo sát: Kết quả tính cách của User
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

-- ==========================================
-- 3. HƯỚNG DẪN TẠO TÀI KHOẢN MẪU
-- ==========================================
-- KHÔNG tạo user trực tiếp vào auth.users bằng SQL (crypt hash không tương thích với Supabase GoTrue)
-- Thay vào đó, tạo user qua Supabase Dashboard: Authentication → Users → Add user
--
-- Tài khoản cần tạo (password: 12345678):
--   admin@tripmate.com
--   traveler@tripmate.com
--   guide@tripmate.com
--
-- Sau khi tạo xong, chạy SQL bên dưới để set roles:

-- Set roles (chạy sau khi đã tạo user qua Dashboard)
-- UPDATE public.profiles SET role = 'admin' WHERE email = 'admin@tripmate.com';
-- UPDATE public.profiles SET role = 'guide'  WHERE email = 'guide@tripmate.com';
-- UPDATE public.profiles SET phone = '0111222333' WHERE email = 'admin@tripmate.com';
-- UPDATE public.profiles SET phone = '0999888777' WHERE email = 'traveler@tripmate.com';
-- UPDATE public.profiles SET phone = '0555444333' WHERE email = 'guide@tripmate.com';


-- 1. Tạo hoặc cập nhật lại Trigger hàm xử lý
CREATE OR REPLACE FUNCTION public.handle_new_user()
RETURNS trigger AS $$
BEGIN
  INSERT INTO public.profiles (id, email, full_name, avatar_url, role, status)
  VALUES (
    new.id, 
    new.email, 
    new.raw_user_meta_data->>'full_name', 
    new.raw_user_meta_data->>'avatar_url', 
    coalesce(new.raw_user_meta_data->>'role', 'traveler'), 
    'active'
  )
  ON CONFLICT (id) DO UPDATE SET
    full_name = EXCLUDED.full_name,
    role = EXCLUDED.role;
  RETURN NEW;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- 2. Đảm bảo trigger đã được gắn vào bảng auth.users
DROP TRIGGER IF EXISTS on_auth_user_created ON auth.users;
CREATE TRIGGER on_auth_user_created
  AFTER INSERT ON auth.users
  FOR EACH ROW EXECUTE FUNCTION public.handle_new_user();


-- ==========================================
-- 4. THIẾT LẬP BẢO MẬT ROW LEVEL SECURITY (RLS)
-- ==========================================

-- Bật tính năng RLS trên toàn bộ các bảng quan trọng
ALTER TABLE public.profiles ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.guide_certificates ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.tour_templates ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.guide_tours ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.tour_availability ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.bookings ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.payments ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.reviews ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.conversations ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.messages ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.survey_questions ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.survey_options ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.user_personalities ENABLE ROW LEVEL SECURITY;

-- ------------------------------------------
-- POLICIES CHO BẢNG PROFILES
-- ------------------------------------------
CREATE POLICY "Cho phép tất cả mọi người đọc profile của nhau" 
  ON public.profiles FOR SELECT USING (true);

CREATE POLICY "Chính chủ mới được cập nhật profile của mình" 
  ON public.profiles FOR UPDATE USING (auth.uid() = id);

-- ------------------------------------------
-- POLICIES CHO BẢNG TOUR & AVAILABILITY (CÔNG KHAI)
-- ------------------------------------------
CREATE POLICY "guide_tours_select_public"
  ON public.guide_tours FOR SELECT USING (true);

CREATE POLICY "tour_availability_select_public"
  ON public.tour_availability FOR SELECT USING (true);

-- Guide chỉ INSERT/UPDATE/DELETE tour của chính mình
CREATE POLICY "guide_tours_insert_own"
  ON public.guide_tours FOR INSERT
  WITH CHECK (auth.uid() = guide_id);

CREATE POLICY "guide_tours_update_own"
  ON public.guide_tours FOR UPDATE
  USING (auth.uid() = guide_id);

CREATE POLICY "guide_tours_delete_own"
  ON public.guide_tours FOR DELETE
  USING (auth.uid() = guide_id);

-- Admin quản lý tour_templates
CREATE POLICY "tour_templates_select_public"
  ON public.tour_templates FOR SELECT USING (true);

CREATE POLICY "tour_templates_write_admin"
  ON public.tour_templates FOR INSERT
  WITH CHECK (
    EXISTS (SELECT 1 FROM public.profiles WHERE id = auth.uid() AND role = 'admin')
  );

CREATE POLICY "tour_templates_update_admin"
  ON public.tour_templates FOR UPDATE
  USING (
    EXISTS (SELECT 1 FROM public.profiles WHERE id = auth.uid() AND role = 'admin')
  );

-- ------------------------------------------
-- POLICIES CHO ĐẶT TOUR (BOOKINGS) & THANH TOÁN
-- ------------------------------------------
CREATE POLICY "Khách hàng tự xem booking, Guide xem booking đặt tour của họ" 
  ON public.bookings FOR SELECT USING (
    auth.uid() = traveler_id OR 
    EXISTS (
      SELECT 1 FROM public.tour_availability ta
      JOIN public.guide_tours gt ON ta.guide_tour_id = gt.id
      WHERE ta.id = bookings.tour_availability_id AND gt.guide_id = auth.uid()
    )
  );

CREATE POLICY "Chỉ người dùng là Traveler mới có quyền đặt tour" 
  ON public.bookings FOR INSERT WITH CHECK (auth.uid() = traveler_id);

-- ------------------------------------------
-- POLICIES CHO PHẦN TIN NHẮN (CHAT)
-- ------------------------------------------
-- 1. Xóa policy bị lỗi cú pháp trước
DROP POLICY IF EXISTS "Chỉ thành viên trong nhóm mới đọc/gửi được tin nhắn" ON public.messages;

-- 2. Tạo lại với cú pháp chuẩn xác (Dùng OR thay cho ||)
CREATE POLICY "Chỉ thành viên trong nhóm mới đọc/gửi được tin nhắn" 
  ON public.messages FOR ALL USING (
    EXISTS (
      SELECT 1 FROM public.conversations 
      WHERE id = messages.conversation_id 
        AND (traveler_id = auth.uid() OR guide_id = auth.uid())
    )
  );

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

-- ==========================================
-- 5. GRANT PERMISSIONS (QUAN TRỌNG)
-- RLS chỉ kiểm soát row-level, cần grant table-level permissions riêng
-- ==========================================

-- Public tables (anon + authenticated có thể đọc)
GRANT SELECT ON public.guide_tours TO anon, authenticated;
GRANT SELECT ON public.tour_templates TO anon, authenticated;
GRANT SELECT ON public.tour_availability TO anon, authenticated;
GRANT SELECT ON public.profiles TO anon, authenticated;
GRANT SELECT ON public.reviews TO anon, authenticated;
GRANT SELECT ON public.survey_questions TO anon, authenticated;
GRANT SELECT ON public.survey_options TO anon, authenticated;

-- Guide tours management (authenticated only)
GRANT INSERT, UPDATE, DELETE ON public.guide_tours TO authenticated;
GRANT INSERT, UPDATE, DELETE ON public.tour_availability TO authenticated;

-- Bookings (authenticated only)
GRANT SELECT, INSERT, UPDATE ON public.bookings TO authenticated;

-- Payments (authenticated only)
GRANT SELECT, INSERT ON public.payments TO authenticated;

-- Reviews (authenticated only)
GRANT INSERT, UPDATE, DELETE ON public.reviews TO authenticated;

-- Chat (authenticated only)
GRANT SELECT, INSERT ON public.conversations TO authenticated;
GRANT SELECT, INSERT, UPDATE ON public.messages TO authenticated;

-- Guide certificates (authenticated only)
GRANT SELECT, INSERT ON public.guide_certificates TO authenticated;

-- Survey/personality (authenticated only)
GRANT SELECT, INSERT, UPDATE, DELETE ON public.user_personalities TO authenticated;
