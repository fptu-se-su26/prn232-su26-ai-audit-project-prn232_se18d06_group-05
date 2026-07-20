-- 1. Create the notifications table
CREATE TABLE public.notifications (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID REFERENCES public.profiles(id) ON DELETE CASCADE,
    title TEXT NOT NULL,
    message TEXT NOT NULL,
    type TEXT NOT NULL, -- 'booking_request', 'booking_confirmed', 'booking_cancelled', 'new_review', 'system'
    is_read BOOLEAN DEFAULT false,
    link_url TEXT,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT TIMEZONE('utc'::text, NOW())
);

-- 2. Enable RLS
ALTER TABLE public.notifications ENABLE ROW LEVEL SECURITY;

-- 3. Create policies
-- Users can read their own notifications
CREATE POLICY "Users can view their own notifications" 
ON public.notifications 
FOR SELECT 
USING (auth.uid() = user_id);

-- Only service role can insert/update/delete notifications (or we can use bypass RLS via backend)
CREATE POLICY "Service role has full access to notifications" 
ON public.notifications 
USING (true)
WITH CHECK (true);
