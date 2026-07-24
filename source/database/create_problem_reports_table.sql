-- ==========================================
-- CREATE PROBLEM REPORTS TABLE
-- ==========================================
DROP TABLE IF EXISTS public.problem_reports CASCADE;

CREATE TABLE public.problem_reports (
  id uuid NOT NULL DEFAULT gen_random_uuid(),
  user_id uuid NOT NULL,
  type text NOT NULL, -- 'booking', 'payment', 'account', 'technical', 'other'
  booking_id uuid,
  title text NOT NULL,
  description text NOT NULL,
  image_url text,
  status text NOT NULL DEFAULT 'pending', -- 'pending', 'resolved'
  admin_comment text,
  created_at timestamp with time zone DEFAULT now(),
  updated_at timestamp with time zone DEFAULT now(),
  CONSTRAINT problem_reports_pkey PRIMARY KEY (id),
  CONSTRAINT problem_reports_user_fkey FOREIGN KEY (user_id) REFERENCES public.profiles(id) ON DELETE CASCADE,
  CONSTRAINT problem_reports_booking_fkey FOREIGN KEY (booking_id) REFERENCES public.bookings(id) ON DELETE SET NULL
);

-- Bật tính năng RLS trên bảng problem_reports
ALTER TABLE public.problem_reports ENABLE ROW LEVEL SECURITY;

-- Tạo các policies RLS
CREATE POLICY "Users can insert their own reports" ON public.problem_reports 
  FOR INSERT WITH CHECK (auth.uid() = user_id);

CREATE POLICY "Users can view their own reports" ON public.problem_reports 
  FOR SELECT USING (auth.uid() = user_id);

CREATE POLICY "Admin can perform all operations on reports" ON public.problem_reports 
  FOR ALL USING (
    EXISTS (SELECT 1 FROM public.profiles WHERE id = auth.uid() AND role = 'admin')
  );

-- Cấp quyền truy cập cho authenticated users và anon
GRANT SELECT, INSERT ON public.problem_reports TO authenticated;
GRANT SELECT, INSERT, UPDATE, DELETE ON public.problem_reports TO service_role;
