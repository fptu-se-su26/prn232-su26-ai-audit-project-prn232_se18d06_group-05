-- Run this script once in the Supabase SQL editor before enabling notifications.
-- It is idempotent and safe to re-run.

create table if not exists public.notifications (
    id uuid primary key default gen_random_uuid(),
    user_id uuid not null references auth.users(id) on delete cascade,
    type text not null,
    title text not null,
    message text not null,
    data jsonb not null default '{}'::jsonb,
    action_url text null,
    is_read boolean not null default false,
    created_at timestamptz not null default now(),
    dedupe_key text null,
    constraint notifications_user_dedupe unique (user_id, dedupe_key)
);

create index if not exists notifications_user_created_idx
    on public.notifications (user_id, created_at desc);
create index if not exists notifications_user_unread_idx
    on public.notifications (user_id, is_read, created_at desc);

alter table public.notifications enable row level security;

drop policy if exists "Users can read their notifications" on public.notifications;
create policy "Users can read their notifications"
    on public.notifications for select to authenticated
    using (user_id = auth.uid());

drop policy if exists "Users can update their notifications" on public.notifications;
create policy "Users can update their notifications"
    on public.notifications for update to authenticated
    using (user_id = auth.uid())
    with check (user_id = auth.uid());

drop policy if exists "Users can delete their notifications" on public.notifications;
create policy "Users can delete their notifications"
    on public.notifications for delete to authenticated
    using (user_id = auth.uid());

-- Existing guide-application compatibility table.
create table if not exists public.admin_notifications (
    id uuid primary key default gen_random_uuid(),
    type text not null,
    title text not null,
    message text not null,
    guide_id uuid null,
    guide_name text null,
    guide_email text null,
    is_read boolean not null default false,
    created_at timestamptz not null default now()
);

alter table public.admin_notifications enable row level security;
drop policy if exists "Admins manage admin notifications" on public.admin_notifications;
create policy "Admins manage admin notifications"
    on public.admin_notifications for all to authenticated
    using (exists (
        select 1 from public.profiles p
        where p.id = auth.uid() and p.role = 'admin'
    ))
    with check (exists (
        select 1 from public.profiles p
        where p.id = auth.uid() and p.role = 'admin'
    ));

-- PostgREST needs a schema refresh after DDL changes.
notify pgrst, 'reload schema';
