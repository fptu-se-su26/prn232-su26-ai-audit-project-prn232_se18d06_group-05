-- Simple update to profiles table - Add new columns only
-- Run this script to add new fields for enhanced registration

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

-- Success message
SELECT 'Profiles table updated successfully!' as message;