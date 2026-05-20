-- ═══════════════════════════════════════════════════════════════════════════
-- Chat Tables Migration
-- ═══════════════════════════════════════════════════════════════════════════

-- ── Conversations ─────────────────────────────────────────────────────────────
create table if not exists public.conversations (
  id          uuid primary key default gen_random_uuid(),
  traveler_id uuid not null references auth.users(id) on delete cascade,
  guide_id    uuid not null references auth.users(id) on delete cascade,
  booking_id  uuid references public.bookings(id) on delete set null,
  created_at  timestamptz default now(),
  unique(traveler_id, guide_id, booking_id)
);

-- Enable RLS
alter table public.conversations enable row level security;

-- Drop existing policies if they exist
drop policy if exists "Participants can view conversation" on public.conversations;
drop policy if exists "Traveler can create conversation" on public.conversations;

-- Create policies
create policy "Participants can view conversation"
  on public.conversations for select
  using (auth.uid() = traveler_id or auth.uid() = guide_id);

create policy "Anyone can create conversation"
  on public.conversations for insert
  with check (auth.uid() = traveler_id or auth.uid() = guide_id);

-- ── Messages ──────────────────────────────────────────────────────────────────
create table if not exists public.messages (
  id              uuid primary key default gen_random_uuid(),
  conversation_id uuid not null references public.conversations(id) on delete cascade,
  sender_id       uuid not null references auth.users(id) on delete cascade,
  content         text not null,
  is_read         boolean default false,
  created_at      timestamptz default now()
);

-- Enable RLS
alter table public.messages enable row level security;

-- Drop existing policies if they exist
drop policy if exists "Participants can read messages" on public.messages;
drop policy if exists "Participants can send messages" on public.messages;

-- Create policies
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

-- ── Indexes ───────────────────────────────────────────────────────────────────
create index if not exists conversations_traveler_idx on public.conversations(traveler_id);
create index if not exists conversations_guide_idx on public.conversations(guide_id);
create index if not exists conversations_booking_idx on public.conversations(booking_id);
create index if not exists messages_conversation_idx on public.messages(conversation_id);
create index if not exists messages_created_at_idx on public.messages(created_at);

-- ── Test data (optional) ──────────────────────────────────────────────────────
-- Uncomment below to insert test data

/*
-- Insert test conversation (replace UUIDs with actual user IDs)
insert into public.conversations (traveler_id, guide_id, booking_id) 
values (
  '00000000-0000-0000-0000-000000000001'::uuid,  -- Replace with actual traveler ID
  '00000000-0000-0000-0000-000000000002'::uuid,  -- Replace with actual guide ID
  null
) on conflict do nothing;

-- Insert test message
insert into public.messages (conversation_id, sender_id, content)
select 
  c.id,
  c.traveler_id,
  'Xin chào! Tôi muốn hỏi về tour này.'
from public.conversations c
limit 1
on conflict do nothing;
*/