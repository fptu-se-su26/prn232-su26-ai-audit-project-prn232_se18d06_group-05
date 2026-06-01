-- Setup Guide Approval Workflow
-- Run this AFTER running update_profiles_simple.sql
-- This script sets up RLS policies and views for guide approval

-- Step 1: Create view for guide applications (for admin dashboard)
DROP VIEW IF EXISTS guide_applications CASCADE;

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

-- Step 2: Grant access to the view
GRANT SELECT ON guide_applications TO authenticated;
GRANT SELECT ON guide_applications TO anon;

-- Step 3: Check existing policies on profiles table
DO $$ 
DECLARE
    policy_exists boolean;
BEGIN
    -- Check if policy exists
    SELECT EXISTS (
        SELECT 1 FROM pg_policies 
        WHERE tablename = 'profiles' 
        AND policyname = 'Users can view own profile regardless of status'
    ) INTO policy_exists;
    
    -- Drop if exists
    IF policy_exists THEN
        DROP POLICY "Users can view own profile regardless of status" ON profiles;
    END IF;
    
    -- Check second policy
    SELECT EXISTS (
        SELECT 1 FROM pg_policies 
        WHERE tablename = 'profiles' 
        AND policyname = 'Admins can update guide status'
    ) INTO policy_exists;
    
    -- Drop if exists
    IF policy_exists THEN
        DROP POLICY "Admins can update guide status" ON profiles;
    END IF;
END $$;

-- Step 4: Create new policies
-- Allow users to view their own profile regardless of status
CREATE POLICY "Users can view own profile regardless of status" 
ON profiles
FOR SELECT 
USING (auth.uid()::text = id);

-- Allow admins to update any profile (including guide status)
CREATE POLICY "Admins can update guide status" 
ON profiles
FOR UPDATE 
USING (
    EXISTS (
        SELECT 1 FROM profiles 
        WHERE id = auth.uid()::text 
        AND role = 'admin'
    )
);

-- Step 5: Create helper function to approve guides
CREATE OR REPLACE FUNCTION approve_guide(guide_id TEXT)
RETURNS void
LANGUAGE plpgsql
SECURITY DEFINER
AS $$
BEGIN
    -- Check if caller is admin
    IF NOT EXISTS (
        SELECT 1 FROM profiles 
        WHERE id = auth.uid()::text 
        AND role = 'admin'
    ) THEN
        RAISE EXCEPTION 'Only admins can approve guides';
    END IF;
    
    -- Update guide status
    UPDATE profiles 
    SET status = 'active', updated_at = NOW()
    WHERE id = guide_id AND role = 'guide';
END;
$$;

-- Step 6: Create helper function to reject guides
CREATE OR REPLACE FUNCTION reject_guide(guide_id TEXT, reason TEXT DEFAULT NULL)
RETURNS void
LANGUAGE plpgsql
SECURITY DEFINER
AS $$
BEGIN
    -- Check if caller is admin
    IF NOT EXISTS (
        SELECT 1 FROM profiles 
        WHERE id = auth.uid()::text 
        AND role = 'admin'
    ) THEN
        RAISE EXCEPTION 'Only admins can reject guides';
    END IF;
    
    -- Update guide status
    UPDATE profiles 
    SET status = 'rejected', updated_at = NOW()
    WHERE id = guide_id AND role = 'guide';
    
    -- TODO: Send email notification with reason
END;
$$;

-- Success message
SELECT 'Guide approval workflow setup complete!' as message,
       'Use approve_guide(guide_id) or reject_guide(guide_id) functions' as usage;