-- ============================================
-- FIX RLS POLICIES FOR NEW DATABASE SCHEMA
-- ============================================
-- Run this script in Supabase SQL Editor to fix permission issues
-- ============================================

-- ==========================================================
-- 1. TOUR TEMPLATES POLICIES (MISSING)
-- ==========================================================

-- Tour templates should be viewable by everyone
CREATE POLICY "Tour templates are viewable by everyone" 
ON tour_templates FOR SELECT 
USING (true);

-- Anyone can create tour templates (for now, can be restricted later)
CREATE POLICY "Anyone can create tour templates" 
ON tour_templates FOR INSERT 
WITH CHECK (true);

-- Anyone can update tour templates (for now, can be restricted later)
CREATE POLICY "Anyone can update tour templates" 
ON tour_templates FOR UPDATE 
USING (true);

-- ==========================================================
-- 2. GUIDE CERTIFICATES POLICIES (MISSING)
-- ==========================================================

-- Guide certificates are viewable by the guide owner and admins
CREATE POLICY "Guide certificates are viewable by owner" 
ON guide_certificates FOR SELECT 
USING (auth.uid() = guide_id OR EXISTS (
  SELECT 1 FROM profiles WHERE id = auth.uid() AND role = 'admin'
));

-- Guides can insert their own certificates
CREATE POLICY "Guides can insert their own certificates" 
ON guide_certificates FOR INSERT 
WITH CHECK (auth.uid() = guide_id);

-- Guides can update their own certificates
CREATE POLICY "Guides can update their own certificates" 
ON guide_certificates FOR UPDATE 
USING (auth.uid() = guide_id);

-- ==========================================================
-- 3. TOUR AVAILABILITY POLICIES (MISSING)
-- ==========================================================

-- Tour availability is viewable by everyone
CREATE POLICY "Tour availability is viewable by everyone" 
ON tour_availability FOR SELECT 
USING (true);

-- Guides can manage availability for their own tours
CREATE POLICY "Guides can manage their tour availability" 
ON tour_availability FOR INSERT 
WITH CHECK (auth.uid() IN (
  SELECT guide_id FROM guide_tours WHERE id = guide_tour_id
));

CREATE POLICY "Guides can update their tour availability" 
ON tour_availability FOR UPDATE 
USING (auth.uid() IN (
  SELECT guide_id FROM guide_tours WHERE id = guide_tour_id
));

CREATE POLICY "Guides can delete their tour availability" 
ON tour_availability FOR DELETE 
USING (auth.uid() IN (
  SELECT guide_id FROM guide_tours WHERE id = guide_tour_id
));

-- ==========================================================
-- 4. PAYMENTS POLICIES (MISSING)
-- ==========================================================

-- Users can view payments for their own bookings
CREATE POLICY "Users can view their own payments" 
ON payments FOR SELECT 
USING (
  auth.uid() IN (
    SELECT traveler_id FROM bookings WHERE id = booking_id
    UNION
    SELECT gt.guide_id FROM bookings b 
    JOIN guide_tours gt ON b.guide_tour_id = gt.id 
    WHERE b.id = booking_id
  )
);

-- System can create payments (this might need to be restricted to service role)
CREATE POLICY "System can create payments" 
ON payments FOR INSERT 
WITH CHECK (true);

-- Users can update payment status for their bookings
CREATE POLICY "Users can update their payment status" 
ON payments FOR UPDATE 
USING (
  auth.uid() IN (
    SELECT traveler_id FROM bookings WHERE id = booking_id
    UNION
    SELECT gt.guide_id FROM bookings b 
    JOIN guide_tours gt ON b.guide_tour_id = gt.id 
    WHERE b.id = booking_id
  )
);

-- ==========================================================
-- 5. CONVERSATIONS POLICIES (MISSING)
-- ==========================================================

-- Users can view conversations they are part of
CREATE POLICY "Users can view their conversations" 
ON conversations FOR SELECT 
USING (auth.uid() = traveler_id OR auth.uid() = guide_id);

-- System can create conversations (usually triggered automatically)
CREATE POLICY "System can create conversations" 
ON conversations FOR INSERT 
WITH CHECK (true);

-- ==========================================================
-- 6. FIX EXISTING POLICIES IF NEEDED
-- ==========================================================

-- Update bookings policies to work with new schema
DROP POLICY IF EXISTS "Users can view their own bookings" ON bookings;
CREATE POLICY "Users can view their own bookings" 
ON bookings FOR SELECT 
USING (
  auth.uid() = traveler_id OR 
  auth.uid() IN (SELECT guide_id FROM guide_tours WHERE id = guide_tour_id)
);

DROP POLICY IF EXISTS "Users can update their own bookings" ON bookings;
CREATE POLICY "Users can update their own bookings" 
ON bookings FOR UPDATE 
USING (
  auth.uid() = traveler_id OR 
  auth.uid() IN (SELECT guide_id FROM guide_tours WHERE id = guide_tour_id)
);

-- ==========================================================
-- 7. TEMPORARY BYPASS FOR DEVELOPMENT (OPTIONAL)
-- ==========================================================
-- Uncomment these lines if you want to temporarily disable RLS for development
-- WARNING: This makes all data publicly accessible!

-- ALTER TABLE tour_templates DISABLE ROW LEVEL SECURITY;
-- ALTER TABLE guide_tours DISABLE ROW LEVEL SECURITY;
-- ALTER TABLE guide_certificates DISABLE ROW LEVEL SECURITY;
-- ALTER TABLE tour_availability DISABLE ROW LEVEL SECURITY;

-- ==========================================================
-- 8. GRANT PERMISSIONS TO ANON AND AUTHENTICATED ROLES
-- ==========================================================

-- Grant basic permissions to anon role (for public access)
GRANT SELECT ON tour_templates TO anon;
GRANT SELECT ON guide_tours TO anon;
GRANT SELECT ON profiles TO anon;
GRANT SELECT ON reviews TO anon;

-- Grant permissions to authenticated role
GRANT ALL ON tour_templates TO authenticated;
GRANT ALL ON guide_tours TO authenticated;
GRANT ALL ON guide_certificates TO authenticated;
GRANT ALL ON tour_availability TO authenticated;
GRANT ALL ON bookings TO authenticated;
GRANT ALL ON payments TO authenticated;
GRANT ALL ON reviews TO authenticated;
GRANT ALL ON conversations TO authenticated;
GRANT ALL ON messages TO authenticated;
GRANT ALL ON profiles TO authenticated;

-- Grant usage on sequences
GRANT USAGE ON ALL SEQUENCES IN SCHEMA public TO authenticated;
GRANT USAGE ON ALL SEQUENCES IN SCHEMA public TO anon;

-- ==========================================================
-- SCRIPT COMPLETE
-- ==========================================================
-- After running this script, your API should be able to access the tables
-- Remember to test with both authenticated and anonymous access
-- ==========================================================