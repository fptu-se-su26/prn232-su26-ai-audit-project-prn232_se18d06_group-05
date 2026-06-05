-- ═══════════════════════════════════════════════════════════════════════════
-- Supabase Storage Setup for Guide Certificates
-- ═══════════════════════════════════════════════════════════════════════════

-- ── 1. Create Storage Bucket ─────────────────────────────────────────────────

INSERT INTO storage.buckets (id, name, public, file_size_limit, allowed_mime_types)
VALUES (
  'guide-certificates',
  'guide-certificates',
  false, -- Private bucket
  10485760, -- 10MB limit
  ARRAY['application/pdf']::text[] -- Only PDF files
)
ON CONFLICT (id) DO NOTHING;

-- ── 2. Storage Policies ──────────────────────────────────────────────────────

-- Policy: Guides can upload their own certificates
CREATE POLICY "Guides can upload own certificates"
ON storage.objects FOR INSERT
WITH CHECK (
  bucket_id = 'guide-certificates' AND
  auth.uid()::text = (storage.foldername(name))[1]
);

-- Policy: Guides can read their own certificates
CREATE POLICY "Guides can read own certificates"
ON storage.objects FOR SELECT
USING (
  bucket_id = 'guide-certificates' AND
  auth.uid()::text = (storage.foldername(name))[1]
);

-- Policy: Admins can read all certificates
CREATE POLICY "Admins can read all certificates"
ON storage.objects FOR SELECT
USING (
  bucket_id = 'guide-certificates' AND
  EXISTS (
    SELECT 1 FROM public.profiles
    WHERE id = auth.uid() AND role = 'admin'
  )
);

-- Policy: Guides can update their own certificates
CREATE POLICY "Guides can update own certificates"
ON storage.objects FOR UPDATE
USING (
  bucket_id = 'guide-certificates' AND
  auth.uid()::text = (storage.foldername(name))[1]
);

-- Policy: Guides can delete their own certificates
CREATE POLICY "Guides can delete own certificates"
ON storage.objects FOR DELETE
USING (
  bucket_id = 'guide-certificates' AND
  auth.uid()::text = (storage.foldername(name))[1]
);

-- ── 3. Update Profiles Table ─────────────────────────────────────────────────

-- Add certificate_url column if not exists
ALTER TABLE public.profiles
ADD COLUMN IF NOT EXISTS certificate_url TEXT;

-- Add comment
COMMENT ON COLUMN public.profiles.certificate_url 
IS 'URL của chứng chỉ hướng dẫn viên trong Supabase Storage';

-- Add index for faster queries
CREATE INDEX IF NOT EXISTS idx_profiles_certificate_url 
ON public.profiles(certificate_url) 
WHERE certificate_url IS NOT NULL;

-- ── 4. Add Guide-Specific Columns ────────────────────────────────────────────

-- Experience
ALTER TABLE public.profiles
ADD COLUMN IF NOT EXISTS experience TEXT;

COMMENT ON COLUMN public.profiles.experience 
IS 'Kinh nghiệm của hướng dẫn viên (ví dụ: "0-1", "1-3", "3-5", "5-10", "10+")';

-- Specialization
ALTER TABLE public.profiles
ADD COLUMN IF NOT EXISTS specialization TEXT;

COMMENT ON COLUMN public.profiles.specialization 
IS 'Chuyên môn của hướng dẫn viên (ví dụ: "cultural", "adventure", "nature")';

-- Languages
ALTER TABLE public.profiles
ADD COLUMN IF NOT EXISTS languages TEXT;

COMMENT ON COLUMN public.profiles.languages 
IS 'Các ngôn ngữ mà hướng dẫn viên có thể giao tiếp';

-- Bio
ALTER TABLE public.profiles
ADD COLUMN IF NOT EXISTS bio TEXT;

COMMENT ON COLUMN public.profiles.bio 
IS 'Giới thiệu bản thân của hướng dẫn viên';

-- Status (for guide approval workflow)
ALTER TABLE public.profiles
ADD COLUMN IF NOT EXISTS status TEXT DEFAULT 'active';

COMMENT ON COLUMN public.profiles.status 
IS 'Trạng thái tài khoản: "pending" (chờ duyệt), "active" (đã duyệt), "suspended" (bị đình chỉ)';

-- ── 5. Create Index ───────────────────────────────────────────────────────────

CREATE INDEX IF NOT EXISTS idx_profiles_status 
ON public.profiles(status);

CREATE INDEX IF NOT EXISTS idx_profiles_role_status 
ON public.profiles(role, status);

-- ── 6. Update RLS Policies ───────────────────────────────────────────────────

-- Allow users to update their own profile (including certificate_url)
DROP POLICY IF EXISTS "Users can update own profile" ON public.profiles;

CREATE POLICY "Users can update own profile"
ON public.profiles FOR UPDATE
USING (auth.uid() = id)
WITH CHECK (auth.uid() = id);

-- Allow admins to update any profile (for approval)
DROP POLICY IF EXISTS "Admins can update any profile" ON public.profiles;

CREATE POLICY "Admins can update any profile"
ON public.profiles FOR UPDATE
USING (
  EXISTS (
    SELECT 1 FROM public.profiles
    WHERE id = auth.uid() AND role = 'admin'
  )
);

-- ══════════════════════════════════════════════════════════════════════════════
-- Verification Queries
-- ══════════════════════════════════════════════════════════════════════════════

-- Check if bucket exists
SELECT * FROM storage.buckets WHERE name = 'guide-certificates';

-- Check storage policies
SELECT * FROM pg_policies 
WHERE tablename = 'objects' AND schemaname = 'storage';

-- Check new columns in profiles table
SELECT column_name, data_type, column_default, is_nullable
FROM information_schema.columns
WHERE table_schema = 'public' 
  AND table_name = 'profiles'
  AND column_name IN ('certificate_url', 'experience', 'specialization', 'languages', 'bio', 'status');

-- ══════════════════════════════════════════════════════════════════════════════
-- Sample Data (Optional - for testing)
-- ══════════════════════════════════════════════════════════════════════════════

-- Update existing guide profile with sample data
-- UPDATE public.profiles
-- SET 
--   experience = '3-5',
--   specialization = 'cultural',
--   languages = 'Tiếng Việt, English',
--   bio = 'Hướng dẫn viên chuyên về du lịch văn hóa với 4 năm kinh nghiệm',
--   status = 'active'
-- WHERE email = 'guide@test.com';
