-- ============================================
-- TEMPORARY DISABLE RLS FOR DEVELOPMENT
-- ============================================
-- WARNING: This makes all data publicly accessible!
-- Only use this for development/testing purposes
-- ============================================

-- Disable RLS for tables that are causing permission issues
ALTER TABLE tour_templates DISABLE ROW LEVEL SECURITY;
ALTER TABLE guide_tours DISABLE ROW LEVEL SECURITY;
ALTER TABLE guide_certificates DISABLE ROW LEVEL SECURITY;
ALTER TABLE tour_availability DISABLE ROW LEVEL SECURITY;

-- Grant basic permissions to anon role
GRANT SELECT ON tour_templates TO anon;
GRANT SELECT ON guide_tours TO anon;
GRANT SELECT ON profiles TO anon;
GRANT SELECT ON reviews TO anon;

-- Grant permissions to authenticated role
GRANT ALL ON tour_templates TO authenticated;
GRANT ALL ON guide_tours TO authenticated;
GRANT ALL ON guide_certificates TO authenticated;
GRANT ALL ON tour_availability TO authenticated;

-- Grant usage on sequences
GRANT USAGE ON ALL SEQUENCES IN SCHEMA public TO authenticated;
GRANT USAGE ON ALL SEQUENCES IN SCHEMA public TO anon;

-- ============================================
-- TO RE-ENABLE RLS LATER, RUN:
-- ============================================
-- ALTER TABLE tour_templates ENABLE ROW LEVEL SECURITY;
-- ALTER TABLE guide_tours ENABLE ROW LEVEL SECURITY;
-- ALTER TABLE guide_certificates ENABLE ROW LEVEL SECURITY;
-- ALTER TABLE tour_availability ENABLE ROW LEVEL SECURITY;
-- ============================================