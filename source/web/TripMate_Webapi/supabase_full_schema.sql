-- ═══════════════════════════════════════════════════════════════════════════
-- TripMate Full Schema — chạy trong Supabase SQL Editor
-- ═══════════════════════════════════════════════════════════════════════════

-- ── Reviews ──────────────────────────────────────────────────────────────────
create table if not exists public.reviews (
  id          uuid primary key default gen_random_uuid(),
  tour_id     uuid not null references public.tours(id) on delete cascade,
  traveler_id uuid not null references auth.users(id) on delete cascade,
  booking_id  uuid references public.bookings(id) on delete set null,
  rating      int not null check (rating between 1 and 5),
  comment     text,
  created_at  timestamptz default now(),
  unique(booking_id)  -- mỗi booking chỉ review 1 lần
);

alter table public.reviews enable row level security;

create policy "Anyone can read reviews"
  on public.reviews for select using (true);

create policy "Traveler can create review for completed booking"
  on public.reviews for insert
  with check (auth.uid() = traveler_id);

-- ── Conversations ─────────────────────────────────────────────────────────────
create table if not exists public.conversations (
  id          uuid primary key default gen_random_uuid(),
  traveler_id uuid not null references auth.users(id) on delete cascade,
  guide_id    uuid not null references auth.users(id) on delete cascade,
  booking_id  uuid references public.bookings(id) on delete set null,
  created_at  timestamptz default now(),
  unique(traveler_id, guide_id, booking_id)
);

alter table public.conversations enable row level security;

create policy "Participants can view conversation"
  on public.conversations for select
  using (auth.uid() = traveler_id or auth.uid() = guide_id);

create policy "Traveler can create conversation"
  on public.conversations for insert
  with check (auth.uid() = traveler_id);

-- ── Messages ──────────────────────────────────────────────────────────────────
create table if not exists public.messages (
  id              uuid primary key default gen_random_uuid(),
  conversation_id uuid not null references public.conversations(id) on delete cascade,
  sender_id       uuid not null references auth.users(id) on delete cascade,
  content         text not null,
  is_read         boolean default false,
  created_at      timestamptz default now()
);

alter table public.messages enable row level security;

create policy "Participants can read messages"
  on public.messages for select
  using (
    exists (
      select 1 from public.conversations c
      where c.id = messages.conversation_id
        and (c.traveler_id = auth.uid() or c.guide_id = auth.uid())
    )
  );

create policy "Participants can send messages"
  on public.messages for insert
  with check (
    auth.uid() = sender_id and
    exists (
      select 1 from public.conversations c
      where c.id = messages.conversation_id
        and (c.traveler_id = auth.uid() or c.guide_id = auth.uid())
    )
  );

-- ── Notifications ─────────────────────────────────────────────────────────────
create table if not exists public.notifications (
  id         uuid primary key default gen_random_uuid(),
  user_id    uuid not null references auth.users(id) on delete cascade,
  type       text not null, -- 'booking_created' | 'booking_confirmed' | 'new_message'
  title      text not null,
  body       text not null,
  data       jsonb,
  is_read    boolean default false,
  created_at timestamptz default now()
);

alter table public.notifications enable row level security;

create policy "User can read own notifications"
  on public.notifications for select
  using (auth.uid() = user_id);

create policy "Service can insert notifications"
  on public.notifications for insert
  with check (true);

-- ── Index ─────────────────────────────────────────────────────────────────────
create index if not exists messages_conv_idx on public.messages(conversation_id);
create index if not exists notif_user_idx    on public.notifications(user_id, is_read);
create index if not exists reviews_tour_idx  on public.reviews(tour_id);
