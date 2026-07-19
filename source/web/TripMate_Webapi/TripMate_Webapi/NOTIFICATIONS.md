# TripMate notifications

## One-time setup

1. Run [`Infrastructure/Sql/notifications.sql`](Infrastructure/Sql/notifications.sql) in the Supabase SQL editor.
2. Set `App:BaseUrl` (or `App__BaseUrl`) in production so email action buttons use the public site URL.
3. Scheduled 24-hour/2-hour reminders and expired-payment notices are enabled by default. Set `Notifications__RemindersEnabled=false` to disable the worker.

The application service role creates notifications. Authenticated reads, updates, and deletes are protected by Supabase RLS and also filter on `user_id` in the API.

## User API

- `GET /api/notifications?limit=30&before=<cursor>&unreadOnly=false`
- `GET /api/notifications/unread-count`
- `PATCH /api/notifications/{id}/read`
- `POST /api/notifications/read-all`
- `DELETE /api/notifications/{id}`

Admin-only operations:

- `POST /api/notifications/admin/announce`
- `POST /api/notifications/admin/send` (support/system types only)
- `POST /api/notifications/admin/support-ticket-update`

Realtime clients connect to `/hubs/notifications` and receive `NotificationReceived` messages. Database persistence remains the source of truth when a client is offline.

## Canonical types

Types are defined in `DTOs/Notification/NotificationTypes.cs`:

- Booking: `booking.awaiting_guide`, `booking.confirmed`, `booking.declined`, `booking.cancelled`, `booking.reminder`, `booking.completed`
- Money: `payment.succeeded`, `payment.failed`, `refund.processed`, `payout.released`, `payout.failed`
- Communication: `message.received`, `review.received`, `review.requested`
- Account/admin: `guide.application_submitted`, `guide.application_approved`, `guide.application_rejected`, `cancellation.review_required`, `support.ticket_updated`, `voucher.issued`, `system.announcement`

Every business event supplies a stable `dedupe_key`. Replaying a webhook, retrying a request, or running multiple reminder scans therefore does not create duplicate rows or duplicate critical emails.
