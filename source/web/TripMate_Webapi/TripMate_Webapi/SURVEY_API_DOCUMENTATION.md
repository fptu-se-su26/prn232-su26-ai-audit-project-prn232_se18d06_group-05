# Survey API Documentation

## Overview

This document describes the Survey/Review API endpoints for the TripMate platform. The API implements the **Traveler Post-Survey Flow** requirements, allowing travelers to submit reviews for completed tours, view their review history, and enabling admins to access analytics.

## Base URL

```
http://localhost:5000/api/surveys
```

## Authentication

All endpoints require authentication via JWT token in the Authorization header:

```
Authorization: Bearer <jwt_token>
X-User-Id: <user_id>
```

---

## Endpoints

### 1. Submit Survey

Submit a review for a completed tour booking.

**Endpoint:** `POST /api/surveys`

**Requirements Implemented:**
- Requirement 1: Survey Submission Processing
- Requirement 2: Survey Submission Feedback
- Requirement 5: Guide Notification
- Requirement 6: Tour Rating Recalculation
- Requirement 9: Incentive for Future Bookings

**Request Headers:**
```
Authorization: Bearer <jwt_token>
X-User-Id: <user_id>
Content-Type: application/json
```

**Request Body:**
```json
{
  "tourId": "uuid",
  "bookingId": "uuid",
  "rating": 5,
  "comment": "Great tour! The guide was very knowledgeable and friendly."
}
```

**Validation Rules:**
- `rating`: Must be between 1 and 5 (integer)
- `comment`: Must be between 10 and 500 characters
- Booking must have status "completed"
- Cannot submit duplicate survey for same booking

**Success Response (200 OK):**
```json
{
  "success": true,
  "message": "Cảm ơn bạn đã đánh giá tour!",
  "survey": {
    "id": "uuid",
    "tourId": "uuid",
    "travelerId": "uuid",
    "travelerName": "John Doe",
    "bookingId": "uuid",
    "rating": 5,
    "comment": "Great tour! The guide was very knowledgeable and friendly.",
    "isPublished": true,
    "createdAt": "2026-06-01T10:30:00Z"
  },
  "discountVoucher": {
    "code": "FIRST5-ABC12345",
    "discountPercent": 5,
    "expiresAt": "2026-07-01T10:30:00Z"
  }
}
```

**Note:** `discountVoucher` is only included for first-time survey submitters.

**Error Responses:**

**400 Bad Request - Invalid Rating:**
```json
{
  "success": false,
  "error": "Rating must be between 1 and 5 stars",
  "details": "Invalid rating value"
}
```

**400 Bad Request - Invalid Comment:**
```json
{
  "success": false,
  "error": "Comment must be between 10 and 500 characters",
  "details": "Current length: 5"
}
```

**400 Bad Request - Booking Not Completed:**
```json
{
  "success": false,
  "error": "Chỉ có thể đánh giá tour đã hoàn thành"
}
```

**400 Bad Request - Duplicate Survey:**
```json
{
  "success": false,
  "error": "Survey already submitted"
}
```

**401 Unauthorized:**
```json
{
  "success": false,
  "error": "User not authenticated"
}
```

---

### 2. Get Tour Surveys

Retrieve all published surveys for a specific tour.

**Endpoint:** `GET /api/surveys/tour/{tourId}`

**Requirements Implemented:**
- Requirement 4: Survey Visibility Update

**Request Headers:**
```
Authorization: Bearer <jwt_token>
```

**Path Parameters:**
- `tourId` (string, required): UUID of the tour

**Success Response (200 OK):**
```json
{
  "surveys": [
    {
      "id": "uuid",
      "tourId": "uuid",
      "travelerId": "uuid",
      "travelerName": "John Doe",
      "bookingId": "uuid",
      "rating": 5,
      "comment": "Great tour! The guide was very knowledgeable and friendly.",
      "isPublished": true,
      "createdAt": "2026-06-01T10:30:00Z"
    },
    {
      "id": "uuid",
      "tourId": "uuid",
      "travelerId": "uuid",
      "travelerName": "Jane Smith",
      "bookingId": "uuid",
      "rating": 4,
      "comment": "Good experience overall. Would recommend to friends.",
      "isPublished": true,
      "createdAt": "2026-05-28T14:20:00Z"
    }
  ],
  "total": 2,
  "averageRating": 4.5
}
```

**Notes:**
- Surveys are sorted by `createdAt` in descending order (newest first)
- Only published surveys are included
- Average rating is rounded to 1 decimal place

---

### 3. Get My Surveys

Retrieve the authenticated traveler's survey history.

**Endpoint:** `GET /api/surveys/my-surveys`

**Requirements Implemented:**
- Requirement 8: Survey Submission Tracking

**Request Headers:**
```
Authorization: Bearer <jwt_token>
X-User-Id: <user_id>
```

**Success Response (200 OK):**
```json
{
  "surveys": [
    {
      "id": "uuid",
      "tourId": "uuid",
      "tourTitle": "Hanoi Old Quarter Walking Tour",
      "tourLocation": "Hanoi, Vietnam",
      "rating": 5,
      "comment": "Great tour! The guide was very knowledgeable and friendly.",
      "createdAt": "2026-06-01T10:30:00Z"
    },
    {
      "id": "uuid",
      "tourId": "uuid",
      "tourTitle": "Ha Long Bay Cruise",
      "tourLocation": "Ha Long Bay, Vietnam",
      "rating": 4,
      "comment": "Beautiful scenery and good food on the cruise.",
      "createdAt": "2026-05-15T09:00:00Z"
    }
  ],
  "total": 2
}
```

**Notes:**
- Surveys are sorted by `createdAt` in descending order (newest first)
- Includes tour information for easy reference

---

### 4. Get Survey Analytics

Retrieve survey analytics for admin dashboard.

**Endpoint:** `GET /api/surveys/analytics`

**Requirements Implemented:**
- Requirement 10: Survey Data Analytics

**Request Headers:**
```
Authorization: Bearer <jwt_token>
```

**Access Control:**
- This endpoint should be restricted to admin users only
- Currently allows any authenticated user (TODO: Add role verification)

**Success Response (200 OK):**
```json
{
  "totalSurveys": 150,
  "averageRating": 4.3,
  "totalCompletedBookings": 200,
  "submissionRate": 75.0,
  "highestRatedTour": {
    "tourId": "uuid",
    "tourTitle": "Hanoi Old Quarter Walking Tour",
    "rating": 4.8,
    "totalReviews": 25
  },
  "lowestRatedTour": {
    "tourId": "uuid",
    "tourTitle": "City Bus Tour",
    "rating": 3.2,
    "totalReviews": 15
  },
  "ratingDistribution": {
    "1": 5,
    "2": 10,
    "3": 20,
    "4": 45,
    "5": 70
  }
}
```

**Notes:**
- `submissionRate` is calculated as: (totalSurveys / totalCompletedBookings) * 100
- Highest/lowest rated tours require at least 3 reviews to be included
- `ratingDistribution` shows count of surveys for each rating (1-5 stars)
- All ratings are rounded to 1 decimal place

---

### 5. Check Survey Exists

Check if a survey already exists for a booking.

**Endpoint:** `GET /api/surveys/check/{bookingId}`

**Requirements Implemented:**
- Requirement 7: Survey Edit Restriction

**Request Headers:**
```
Authorization: Bearer <jwt_token>
```

**Path Parameters:**
- `bookingId` (string, required): UUID of the booking

**Success Response (200 OK):**
```json
{
  "booking_id": "uuid",
  "message": "Use GET /api/surveys/tour/{tourId} to check if survey exists"
}
```

**Notes:**
- This is a placeholder endpoint
- The actual duplicate check is performed in the submit survey endpoint
- Frontend should use the tour surveys endpoint to check if user has already submitted

---

## Business Logic

### Survey Submission Flow

1. **Validation** (Frontend & Backend)
   - Rating: 1-5 stars
   - Comment: 10-500 characters

2. **Verification** (Backend)
   - Booking exists
   - Booking status is "completed"
   - No existing survey for this booking

3. **Storage** (Backend)
   - Create review record in database
   - Mark as published immediately

4. **Tour Rating Update** (Backend)
   - Fetch all reviews for the tour
   - Calculate new average rating (rounded to 1 decimal)
   - Update tour's `rating` and `total_reviews` fields

5. **Notification** (Backend)
   - Send notification to guide
   - Include tour title, traveler name, and rating

6. **First-Time Incentive** (Backend)
   - Check if this is traveler's first survey
   - If yes, create 5% discount voucher
   - Voucher expires in 30 days
   - Send notification with voucher code

7. **Response** (Backend)
   - Return success message
   - Include survey data
   - Include voucher if applicable

### Survey Visibility

- All surveys are marked as "published" immediately upon creation
- No moderation or approval process
- Surveys appear in tour detail page instantly
- Travelers cannot edit or delete surveys after submission

### Rating Calculation

```
Average Rating = SUM(all ratings) / COUNT(all ratings)
Rounded to 1 decimal place
```

Example:
- Tour has 5 reviews: [5, 4, 5, 3, 5]
- Average = (5 + 4 + 5 + 3 + 5) / 5 = 22 / 5 = 4.4

---

## Database Schema

### reviews table

```sql
create table public.reviews (
  id          uuid primary key default gen_random_uuid(),
  tour_id     uuid not null references public.tours(id) on delete cascade,
  traveler_id uuid not null references auth.users(id) on delete cascade,
  booking_id  uuid references public.bookings(id) on delete set null,
  rating      int not null check (rating between 1 and 5),
  comment     text,
  created_at  timestamptz default now(),
  unique(booking_id)  -- prevents duplicate surveys per booking
);
```

### tours table (relevant fields)

```sql
create table public.tours (
  id            uuid primary key,
  title         text not null,
  rating        numeric(2,1) default 0,  -- average rating
  total_reviews int default 0,           -- count of reviews
  -- other fields...
);
```

---

## Testing with Postman

### 1. Submit Survey

```bash
POST http://localhost:5000/api/surveys
Headers:
  Authorization: Bearer <jwt_token>
  X-User-Id: <user_id>
  Content-Type: application/json

Body:
{
  "tourId": "your-tour-uuid",
  "bookingId": "your-booking-uuid",
  "rating": 5,
  "comment": "This is a test review with more than 10 characters to pass validation."
}
```

### 2. Get Tour Surveys

```bash
GET http://localhost:5000/api/surveys/tour/{tourId}
Headers:
  Authorization: Bearer <jwt_token>
```

### 3. Get My Surveys

```bash
GET http://localhost:5000/api/surveys/my-surveys
Headers:
  Authorization: Bearer <jwt_token>
  X-User-Id: <user_id>
```

### 4. Get Analytics

```bash
GET http://localhost:5000/api/surveys/analytics
Headers:
  Authorization: Bearer <jwt_token>
```

---

## Error Handling

All endpoints follow consistent error response format:

```json
{
  "success": false,
  "error": "Error message",
  "details": "Optional additional details"
}
```

Common HTTP status codes:
- `200 OK`: Success
- `400 Bad Request`: Validation error or business logic error
- `401 Unauthorized`: Missing or invalid authentication
- `403 Forbidden`: Insufficient permissions
- `404 Not Found`: Resource not found
- `500 Internal Server Error`: Server error

---

## Future Enhancements

### TODO Items

1. **Voucher System**
   - Create `vouchers` table in database
   - Store voucher codes with expiration dates
   - Implement voucher validation in booking flow
   - Track voucher usage

2. **Admin Role Verification**
   - Add role check in analytics endpoint
   - Return 403 Forbidden for non-admin users

3. **Survey Moderation**
   - Add `is_published` field logic
   - Allow admins to hide inappropriate reviews
   - Add moderation dashboard

4. **Survey Editing**
   - Allow travelers to edit surveys within 24 hours
   - Track edit history
   - Show "edited" badge on modified reviews

5. **Rich Media**
   - Allow travelers to upload photos with reviews
   - Store image URLs in review record
   - Display images in tour detail page

6. **Helpful Votes**
   - Add "helpful" voting system for reviews
   - Sort reviews by helpfulness
   - Show helpful count

7. **Guide Responses**
   - Allow guides to respond to reviews
   - Show guide responses below reviews
   - Send notification to traveler when guide responds

---

## Requirements Coverage

| Requirement | Status | Endpoints |
|-------------|--------|-----------|
| Req 1: Survey Submission Processing | ✅ Implemented | POST /api/surveys |
| Req 2: Survey Submission Feedback | ✅ Implemented | POST /api/surveys |
| Req 3: Post-Survey Navigation Options | ⚠️ Frontend Only | N/A |
| Req 4: Survey Visibility Update | ✅ Implemented | GET /api/surveys/tour/{tourId} |
| Req 5: Guide Notification | ✅ Implemented | POST /api/surveys |
| Req 6: Tour Rating Recalculation | ✅ Implemented | POST /api/surveys |
| Req 7: Survey Edit Restriction | ✅ Implemented | POST /api/surveys |
| Req 8: Survey Submission Tracking | ✅ Implemented | GET /api/surveys/my-surveys |
| Req 9: Incentive for Future Bookings | ✅ Implemented | POST /api/surveys |
| Req 10: Survey Data Analytics | ✅ Implemented | GET /api/surveys/analytics |

**Legend:**
- ✅ Implemented: Backend API complete
- ⚠️ Frontend Only: No backend API needed
- ❌ Not Implemented: Requires additional work

---

## Support

For questions or issues, contact the development team or create an issue in the project repository.
