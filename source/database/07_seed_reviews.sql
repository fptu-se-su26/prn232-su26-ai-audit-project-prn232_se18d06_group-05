-- Seed dummy reviews for testing the home page
-- Need to get an existing traveler_id, guide_profile_id, and booking_id.

DO $$
DECLARE
    v_traveler_id uuid;
    v_guide_id uuid;
    v_booking_id1 uuid;
    v_booking_id2 uuid;
    v_booking_id3 uuid;
BEGIN
    -- Get a random traveler (user with role 'traveler')
    SELECT id INTO v_traveler_id FROM profiles WHERE role = 'traveler' LIMIT 1;
    
    -- Get a random guide
    SELECT id INTO v_guide_id FROM guide_profiles LIMIT 1;

    -- For reviews, each review needs a UNIQUE booking_id because of the reviews_booking_id_key constraint.
    IF v_traveler_id IS NOT NULL AND v_guide_id IS NOT NULL THEN
        -- Create Booking 1
        INSERT INTO bookings (
            id, traveler_id, guide_profile_id, experience_package_id, 
            booking_date, start_time, guest_count, total_amount, platform_fee, guide_earnings, status, created_at, updated_at
        ) VALUES (
            gen_random_uuid(), v_traveler_id, v_guide_id, '00000000-0000-0000-0000-000000000000',
            now(), now(), 2, 500000, 50000, 450000, 2, now(), now()
        ) RETURNING id INTO v_booking_id1;

        -- Insert Review 1
        INSERT INTO reviews (id, booking_id, traveler_id, guide_profile_id, rating, comment, created_at)
        VALUES (
            gen_random_uuid(), v_booking_id1, v_traveler_id, v_guide_id, 5, 
            'Incredible experience! The guide was extremely knowledgeable and matched perfectly with what we wanted.', now() - INTERVAL '1 day'
        );

        -- Create Booking 2
        INSERT INTO bookings (
            id, traveler_id, guide_profile_id, experience_package_id, 
            booking_date, start_time, guest_count, total_amount, platform_fee, guide_earnings, status, created_at, updated_at
        ) VALUES (
            gen_random_uuid(), v_traveler_id, v_guide_id, '00000000-0000-0000-0000-000000000000',
            now(), now(), 2, 500000, 50000, 450000, 2, now(), now()
        ) RETURNING id INTO v_booking_id2;

        -- Insert Review 2
        INSERT INTO reviews (id, booking_id, traveler_id, guide_profile_id, rating, comment, created_at)
        VALUES (
            gen_random_uuid(), v_booking_id2, v_traveler_id, v_guide_id, 5, 
            'Best trip ever. Food was amazing and we saw places no tourists know about.', now() - INTERVAL '3 days'
        );

        -- Create Booking 3
        INSERT INTO bookings (
            id, traveler_id, guide_profile_id, experience_package_id, 
            booking_date, start_time, guest_count, total_amount, platform_fee, guide_earnings, status, created_at, updated_at
        ) VALUES (
            gen_random_uuid(), v_traveler_id, v_guide_id, '00000000-0000-0000-0000-000000000000',
            now(), now(), 2, 500000, 50000, 450000, 2, now(), now()
        ) RETURNING id INTO v_booking_id3;

        -- Insert Review 3
        INSERT INTO reviews (id, booking_id, traveler_id, guide_profile_id, rating, comment, created_at)
        VALUES (
            gen_random_uuid(), v_booking_id3, v_traveler_id, v_guide_id, 4, 
            'Very patient guide. Explained the culture in deep detail. A truly authentic experience.', now() - INTERVAL '5 days'
        );
    END IF;
END $$;
