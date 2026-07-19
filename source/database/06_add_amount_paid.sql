-- Add amount_paid column to track the 2-step payment process
ALTER TABLE bookings
ADD COLUMN IF NOT EXISTS amount_paid decimal(18,2) NOT NULL DEFAULT 0;
