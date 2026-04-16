# Database Setup Guide

## Vấn đề hiện tại
App đang hiển thị lỗi: `Could not find the table 'public.conversations'` vì các bảng chat chưa được tạo trong Supabase.

## Cách khắc phục

### 1. Truy cập Supabase Dashboard
- Mở [Supabase Dashboard](https://supabase.com/dashboard)
- Chọn project TripMate của bạn

### 2. Chạy Migration Script
- Vào **SQL Editor** trong Supabase Dashboard
- Copy và paste nội dung từ file `web/TripMate_Webapi/migrations/003_chat_tables.sql`
- Nhấn **Run** để tạo các bảng cần thiết

### 3. Kiểm tra Tables
Sau khi chạy script, bạn sẽ có các bảng:
- `conversations` - Lưu cuộc trò chuyện giữa traveler và guide
- `messages` - Lưu tin nhắn trong mỗi conversation

### 4. Test API
Có thể test bằng debug endpoints:
- `GET /api/debug/db-test` - Test kết nối database
- `GET /api/debug/chat-test` - Test chat service
- `GET /api/debug/env` - Kiểm tra environment

### 5. Restart App
Sau khi setup database, restart Flutter app để thấy thay đổi.

## Schema Overview

```sql
-- Conversations: Cuộc trò chuyện giữa traveler và guide
conversations (
  id uuid PRIMARY KEY,
  traveler_id uuid REFERENCES auth.users(id),
  guide_id uuid REFERENCES auth.users(id), 
  booking_id uuid REFERENCES bookings(id),
  created_at timestamptz
)

-- Messages: Tin nhắn trong conversation
messages (
  id uuid PRIMARY KEY,
  conversation_id uuid REFERENCES conversations(id),
  sender_id uuid REFERENCES auth.users(id),
  content text,
  is_read boolean,
  created_at timestamptz
)
```

## Row Level Security (RLS)
- Chỉ participants (traveler + guide) mới xem được conversation và messages
- Chỉ participants mới gửi được tin nhắn
- Bảo mật dữ liệu người dùng

## Troubleshooting

### Lỗi "table not found"
- Chạy lại migration script
- Kiểm tra RLS policies đã được tạo chưa

### Lỗi "permission denied" 
- Kiểm tra user đã login chưa
- Kiểm tra RLS policies

### App vẫn hiển thị empty state
- Thử pull-to-refresh trong conversation list
- Kiểm tra network connection
- Xem logs trong browser developer tools