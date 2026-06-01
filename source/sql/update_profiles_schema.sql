-- Update profiles table to add new fields for enhanced registration
-- Run this script to add new columns to the profiles table

-- Add phone number field
ALTER TABLE profiles 
ADD COLUMN IF NOT EXISTS phone_number VARCHAR(15);

-- Add guide-specific fields
ALTER TABLE profiles 
ADD COLUMN IF NOT EXISTS experience VARCHAR(50);

ALTER TABLE profiles 
ADD COLUMN IF NOT EXISTS specialization VARCHAR(100);

ALTER TABLE profiles 
ADD COLUMN IF NOT EXISTS languages TEXT;

ALTER TABLE profiles 
ADD COLUMN IF NOT EXISTS bio TEXT;

ALTER TABLE profiles 
ADD COLUMN IF NOT EXISTS certificate_path VARCHAR(500);

ALTER TABLE profiles 
ADD COLUMN IF NOT EXISTS status VARCHAR(20) DEFAULT 'active';

-- Add indexes for better performance
CREATE INDEX IF NOT EXISTS idx_profiles_phone_number ON profiles(phone_number);
CREATE INDEX IF NOT EXISTS idx_profiles_specialization ON profiles(specialization);
CREATE INDEX IF NOT EXISTS idx_profiles_status ON profiles(status);

-- Add comments for documentation
COMMENT ON COLUMN profiles.phone_number IS 'User phone number (10-11 digits)';
COMMENT ON COLUMN profiles.experience IS 'Guide experience level (0-1, 1-3, 3-5, 5-10, 10+ years)';
COMMENT ON COLUMN profiles.specialization IS 'Guide specialization (cultural, adventure, nature, food, etc.)';
COMMENT ON COLUMN profiles.languages IS 'Languages the guide can speak';
COMMENT ON COLUMN profiles.bio IS 'Guide bio/description (max 500 chars)';
COMMENT ON COLUMN profiles.certificate_path IS 'Path to uploaded certificate file';
COMMENT ON COLUMN profiles.status IS 'Account status: active, pending, suspended, rejected';

-- Update RLS policies to handle new status field
-- Drop existing policies if they exist to avoid conflicts
DO $$ 
BEGIN
    -- Drop policy if exists
    DROP POLICY IF EXISTS "Users can view own profile regardless of status" ON profiles;
    DROP POLICY IF EXISTS "Admins can update guide status" ON profiles;
EXCEPTION
    WHEN undefined_object THEN NULL;
END $$;

-- Allow guides to see their own pending status
CREATE POLICY "Users can view own profile regardless of status" ON profiles
    FOR SELECT USING (auth.uid()::text = id);

-- Allow admins to update guide status
CREATE POLICY "Admins can update guide status" ON profiles
    FOR UPDATE USING (
        EXISTS (
            SELECT 1 FROM profiles 
            WHERE id = auth.uid()::text 
            AND role = 'admin'
        )
    );

-- Create a view for guide applications (for admin dashboard)
DROP VIEW IF EXISTS guide_applications;

CREATE VIEW guide_applications AS
SELECT 
    id,
    full_name,
    email,
    phone_number,
    experience,
    specialization,
    languages,
    bio,
    certificate_path,
    status,
    created_at,
    updated_at
FROM profiles 
WHERE role = 'guide'
ORDER BY created_at DESC;

-- Grant access to the view
GRANT SELECT ON guide_applications TO authenticated;