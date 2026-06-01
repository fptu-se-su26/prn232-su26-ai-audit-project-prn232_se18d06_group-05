# Requirements Document

## Introduction

Hệ thống Personality-Based Guide Matching cho phép travelers tìm kiếm và được gợi ý các guides có tính cách tương đồng dựa trên kết quả khảo sát MBTI 20 câu hỏi. Hiện tại, kết quả khảo sát chỉ được lưu trong localStorage của trình duyệt. Tính năng này sẽ mở rộng để lưu trữ dữ liệu vào database Supabase, tính toán độ tương đồng tính cách, và hiển thị danh sách guides được recommend dựa trên personality matching score, số lượng reviews, và điểm đánh giá trung bình.

## Glossary

- **Traveler**: Người dùng có vai trò du khách trong hệ thống TripMate
- **Guide**: Người dùng có vai trò hướng dẫn viên trong hệ thống TripMate
- **Personality_Profile**: Hồ sơ tính cách bao gồm 5 chiều đo: extroversion, planning, adventure, cultural, social (mỗi chiều có giá trị từ 0-100)
- **Survey_Result**: Kết quả khảo sát MBTI gồm 20 câu trả lời (mỗi câu có giá trị từ 1-5)
- **Compatibility_Score**: Điểm số đo lường độ tương đồng tính cách giữa Traveler và Guide (giá trị từ 0-100)
- **Personality_API**: API service xử lý các thao tác liên quan đến personality profile và matching
- **Supabase_Database**: PostgreSQL database được quản lý bởi Supabase
- **Recommended_Guides_List**: Danh sách các guides được sắp xếp theo Compatibility_Score, số reviews, và rating
- **LocalStorage**: Bộ nhớ cục bộ của trình duyệt lưu trữ dữ liệu tạm thời

## Requirements

### Requirement 1: Persist Survey Results to Database

**User Story:** As a traveler, I want my personality survey results to be saved to the database, so that I can access my profile from any device and receive personalized guide recommendations.

#### Acceptance Criteria

1. WHEN a Traveler submits the personality survey, THE Personality_API SHALL validate that all 20 answers are provided
2. WHEN a Traveler submits the personality survey, THE Personality_API SHALL validate that each answer value is between 1 and 5 inclusive
3. WHEN the survey data is valid, THE Personality_API SHALL calculate the five personality dimension scores (extroversion, planning, adventure, cultural, social) from the 20 answers
4. WHEN the personality dimension scores are calculated, THE Personality_API SHALL ensure each dimension score is between 0 and 100 inclusive
5. WHEN the personality dimension scores are valid, THE Personality_API SHALL save the Survey_Result and Personality_Profile to the Supabase_Database
6. WHEN saving to the database, THE Personality_API SHALL associate the Personality_Profile with the authenticated Traveler's user ID
7. IF a Personality_Profile already exists for the Traveler, THEN THE Personality_API SHALL update the existing record instead of creating a new one
8. WHEN the save operation succeeds, THE Personality_API SHALL return a success response with the saved Personality_Profile
9. IF the save operation fails, THEN THE Personality_API SHALL return an error response with a descriptive error message
10. WHEN the Personality_Profile is saved successfully, THE Personality_API SHALL clear the survey data from LocalStorage

### Requirement 2: Database Schema for Personality Profiles

**User Story:** As a system administrator, I want a well-structured database schema for personality profiles, so that data integrity is maintained and queries are efficient.

#### Acceptance Criteria

1. THE Supabase_Database SHALL contain a table named "traveler_personality" with columns: id, traveler_id, extroversion, planning, adventure, cultural, social, answers, created_at, updated_at
2. THE Supabase_Database SHALL enforce that traveler_id is a foreign key referencing the profiles table
3. THE Supabase_Database SHALL enforce that each personality dimension (extroversion, planning, adventure, cultural, social) is an integer between 0 and 100
4. THE Supabase_Database SHALL enforce that answers is stored as JSONB containing an array of 20 integers
5. THE Supabase_Database SHALL enforce a unique constraint on traveler_id to prevent duplicate personality profiles
6. THE Supabase_Database SHALL automatically set created_at to the current timestamp when a record is inserted
7. THE Supabase_Database SHALL automatically update updated_at to the current timestamp when a record is modified
8. THE Supabase_Database SHALL contain a table named "guide_personality" with the same structure as "traveler_personality" but with guide_id instead of traveler_id
9. THE Supabase_Database SHALL create indexes on traveler_id and guide_id columns for query performance

### Requirement 3: Calculate Personality Compatibility Score

**User Story:** As a traveler, I want the system to calculate how compatible my personality is with each guide, so that I can find guides who match my travel style.

#### Acceptance Criteria

1. WHEN calculating compatibility between a Traveler and a Guide, THE Personality_API SHALL retrieve both Personality_Profiles from the Supabase_Database
2. WHEN both Personality_Profiles are retrieved, THE Personality_API SHALL calculate the absolute difference for each of the five personality dimensions
3. WHEN the differences are calculated, THE Personality_API SHALL compute the Compatibility_Score using the formula: 100 - (sum of absolute differences / 5)
4. WHEN the Compatibility_Score is computed, THE Personality_API SHALL ensure the score is between 0 and 100 inclusive
5. WHEN a Guide does not have a Personality_Profile, THE Personality_API SHALL assign a default Compatibility_Score of 50
6. WHEN calculating compatibility for multiple Guides, THE Personality_API SHALL process all calculations within 2 seconds for up to 100 Guides
7. THE Personality_API SHALL cache Compatibility_Scores for 1 hour to improve performance for repeated requests

### Requirement 4: Retrieve Recommended Guides

**User Story:** As a traveler, I want to see a list of recommended guides sorted by compatibility, so that I can choose the best guide for my trip.

#### Acceptance Criteria

1. WHEN a Traveler requests recommended guides, THE Personality_API SHALL verify that the Traveler has completed the personality survey
2. IF the Traveler has not completed the survey, THEN THE Personality_API SHALL return an error indicating survey completion is required
3. WHEN the Traveler has completed the survey, THE Personality_API SHALL retrieve all active Guides from the Supabase_Database
4. WHEN Guides are retrieved, THE Personality_API SHALL calculate the Compatibility_Score for each Guide
5. WHEN Compatibility_Scores are calculated, THE Personality_API SHALL retrieve the review count and average rating for each Guide
6. WHEN all data is collected, THE Personality_API SHALL sort the Recommended_Guides_List by Compatibility_Score (descending), then by review count (descending), then by average rating (descending)
7. WHEN the list is sorted, THE Personality_API SHALL return the top 20 Guides in the Recommended_Guides_List
8. WHERE a pagination parameter is provided, THE Personality_API SHALL return the specified page of results with 20 Guides per page
9. WHEN returning the Recommended_Guides_List, THE Personality_API SHALL include for each Guide: id, name, profile_image, bio, Compatibility_Score, review_count, average_rating, and personality dimensions

### Requirement 5: Display Recommended Guides UI

**User Story:** As a traveler, I want to see recommended guides with their compatibility scores in an attractive interface, so that I can easily compare and select a guide.

#### Acceptance Criteria

1. WHEN a Traveler clicks the "Khám phá tour" button, THE TripMate_Web_UI SHALL request the Recommended_Guides_List from the Personality_API
2. WHEN the Recommended_Guides_List is received, THE TripMate_Web_UI SHALL display each Guide in a card layout with their profile image, name, bio, Compatibility_Score, review count, and average rating
3. WHEN displaying the Compatibility_Score, THE TripMate_Web_UI SHALL show a visual indicator (progress bar or percentage) with color coding: green for scores above 80, yellow for scores 60-80, orange for scores below 60
4. WHEN displaying the Recommended_Guides_List, THE TripMate_Web_UI SHALL show a badge indicating "Best Match" for the Guide with the highest Compatibility_Score
5. WHEN a Traveler has not completed the personality survey, THE TripMate_Web_UI SHALL display a prompt to complete the survey before showing recommendations
6. WHEN the Recommended_Guides_List is empty, THE TripMate_Web_UI SHALL display a message indicating no guides are currently available
7. WHEN displaying Guides without a Personality_Profile, THE TripMate_Web_UI SHALL show a neutral compatibility indicator and a note that the Guide has not completed their personality profile
8. WHEN a Traveler clicks on a Guide card, THE TripMate_Web_UI SHALL navigate to the Guide's detailed profile page

### Requirement 6: Sort and Filter Recommended Guides

**User Story:** As a traveler, I want to sort and filter the recommended guides list, so that I can find guides based on different criteria.

#### Acceptance Criteria

1. WHEN viewing the Recommended_Guides_List, THE TripMate_Web_UI SHALL provide sorting options: "Best Match" (Compatibility_Score), "Most Reviews", "Highest Rating"
2. WHEN a Traveler selects a sorting option, THE TripMate_Web_UI SHALL re-sort the Recommended_Guides_List according to the selected criterion
3. WHEN sorting by "Best Match", THE TripMate_Web_UI SHALL sort by Compatibility_Score descending, then by review count descending
4. WHEN sorting by "Most Reviews", THE TripMate_Web_UI SHALL sort by review count descending, then by Compatibility_Score descending
5. WHEN sorting by "Highest Rating", THE TripMate_Web_UI SHALL sort by average rating descending, then by Compatibility_Score descending
6. WHERE a filter for minimum Compatibility_Score is provided, THE TripMate_Web_UI SHALL only display Guides with Compatibility_Score greater than or equal to the specified value
7. WHEN filters or sorting are applied, THE TripMate_Web_UI SHALL update the display within 500 milliseconds

### Requirement 7: Handle Guides Without Personality Profiles

**User Story:** As a traveler, I want to see all available guides even if they haven't completed their personality survey, so that I have more options to choose from.

#### Acceptance Criteria

1. WHEN retrieving Guides for recommendations, THE Personality_API SHALL include Guides who have not completed their personality survey
2. WHEN a Guide does not have a Personality_Profile, THE Personality_API SHALL assign a default Compatibility_Score of 50
3. WHEN returning Guides without Personality_Profiles, THE Personality_API SHALL include a flag "has_personality_profile" set to false
4. WHEN displaying Guides without Personality_Profiles, THE TripMate_Web_UI SHALL show a neutral compatibility indicator (50%)
5. WHEN displaying Guides without Personality_Profiles, THE TripMate_Web_UI SHALL show a message "Personality profile not available"
6. WHEN sorting the Recommended_Guides_List, THE TripMate_Web_UI SHALL place Guides without Personality_Profiles after Guides with profiles when sorted by Compatibility_Score

### Requirement 8: API Endpoint for Survey Submission

**User Story:** As a developer, I want a well-documented API endpoint for survey submission, so that I can integrate the personality survey with the backend.

#### Acceptance Criteria

1. THE Personality_API SHALL expose an endpoint POST /api/personality/submit that accepts survey results
2. WHEN the endpoint receives a request, THE Personality_API SHALL validate that the request contains a valid authentication token
3. WHEN the endpoint receives a request, THE Personality_API SHALL validate that the request body contains an "answers" array with exactly 20 integer values between 1 and 5
4. IF validation fails, THEN THE Personality_API SHALL return HTTP status 400 with a JSON error message describing the validation failure
5. WHEN validation succeeds, THE Personality_API SHALL process the survey and save the Personality_Profile
6. WHEN the save operation succeeds, THE Personality_API SHALL return HTTP status 200 with a JSON response containing the saved Personality_Profile
7. IF the save operation fails due to a database error, THEN THE Personality_API SHALL return HTTP status 500 with a JSON error message
8. THE Personality_API SHALL log all survey submissions with timestamp, user ID, and success/failure status

### Requirement 9: API Endpoint for Recommended Guides

**User Story:** As a developer, I want a well-documented API endpoint for retrieving recommended guides, so that I can display personalized recommendations in the UI.

#### Acceptance Criteria

1. THE Personality_API SHALL expose an endpoint GET /api/personality/recommended-guides that returns recommended guides
2. WHEN the endpoint receives a request, THE Personality_API SHALL validate that the request contains a valid authentication token for a Traveler
3. WHEN the endpoint receives a request, THE Personality_API SHALL accept optional query parameters: page (integer), limit (integer, max 50), min_compatibility (integer 0-100)
4. IF the authenticated user is not a Traveler, THEN THE Personality_API SHALL return HTTP status 403 with an error message
5. IF the Traveler has not completed the personality survey, THEN THE Personality_API SHALL return HTTP status 400 with an error message indicating survey completion is required
6. WHEN the request is valid, THE Personality_API SHALL return HTTP status 200 with a JSON response containing the Recommended_Guides_List and pagination metadata
7. WHEN returning the response, THE Personality_API SHALL include total count, current page, total pages, and has_next_page in the pagination metadata
8. THE Personality_API SHALL complete the request within 3 seconds for up to 100 Guides

### Requirement 10: Migration Script for Existing Survey Data

**User Story:** As a system administrator, I want to migrate existing survey data from localStorage to the database, so that travelers don't lose their personality profiles.

#### Acceptance Criteria

1. THE Personality_API SHALL expose an endpoint POST /api/personality/migrate that accepts survey data from localStorage
2. WHEN the endpoint receives a request, THE Personality_API SHALL validate that the request contains a valid authentication token
3. WHEN the endpoint receives a request, THE Personality_API SHALL validate that the request body contains a valid Personality_Profile with all five dimensions and the answers array
4. IF a Personality_Profile already exists for the user, THEN THE Personality_API SHALL skip the migration and return a message indicating the profile already exists
5. WHEN the migration data is valid and no profile exists, THE Personality_API SHALL save the Personality_Profile to the Supabase_Database
6. WHEN the migration succeeds, THE Personality_API SHALL return HTTP status 200 with a success message
7. IF the migration fails, THEN THE Personality_API SHALL return an appropriate error status and message without modifying existing data
8. THE TripMate_Web_UI SHALL automatically attempt to migrate localStorage data when a Traveler logs in and has survey data in localStorage but not in the database
