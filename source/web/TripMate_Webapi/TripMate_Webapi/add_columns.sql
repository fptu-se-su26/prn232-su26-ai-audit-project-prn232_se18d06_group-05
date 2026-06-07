-- Chạy đoạn script này trong mục SQL Editor của Supabase:
ALTER TABLE public.profiles 
ADD COLUMN IF NOT EXISTS certificate_path TEXT,
ADD COLUMN IF NOT EXISTS admin_comment TEXT,
ADD COLUMN IF NOT EXISTS approved_at TIMESTAMPTZ,
ADD COLUMN IF NOT EXISTS rejected_at TIMESTAMPTZ;
