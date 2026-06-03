# SQL Schema Updates - Guide Registration Enhancement

## 📋 Overview
Các script SQL để cập nhật database schema cho tính năng đăng ký hướng dẫn viên nâng cao.

## 🚀 Cách chạy (Recommended Order)

### Step 1: Update Profiles Table (REQUIRED)
Chạy script này để thêm các cột mới vào bảng `profiles`:

```sql
-- File: update_profiles_simple.sql
-- Thêm: phone_number, experience, specialization, languages, bio, certificate_path, status
```

**Cách chạy trong Supabase:**
1. Mở Supabase Dashboard
2. Vào **SQL Editor**
3. Copy nội dung file `update_profiles_simple.sql`
4. Paste và click **Run**
5. Kiểm tra message: "Profiles table updated successfully!"

**Kết quả:**
- ✅ 7 cột mới được thêm vào bảng profiles
- ✅ 3 indexes được tạo
- ✅ Comments được thêm cho documentation

---

### Step 2: Setup Guide Approval Workflow (OPTIONAL)
Chạy script này để setup RLS policies và helper functions:

```sql
-- File: setup_guide_approval_workflow.sql
-- Tạo: guide_applications view, RLS policies, approve/reject functions
```

**Cách chạy trong Supabase:**
1. Mở Supabase Dashboard
2. Vào **SQL Editor**
3. Copy nội dung file `setup_guide_approval_workflow.sql`
4. Paste và click **Run**
5. Kiểm tra message: "Guide approval workflow setup complete!"

**Kết quả:**
- ✅ View `guide_applications` được tạo
- ✅ RLS policies được cập nhật
- ✅ Functions `approve_guide()` và `reject_guide()` được tạo

---

## 📊 Database Schema Changes

### New Columns in `profiles` table

| Column | Type | Description | Required |
|---|---|---|---|
| `phone_number` | VARCHAR(15) | User phone number | All users |
| `experience` | VARCHAR(50) | Guide experience level | Guides only |
| `specialization` | VARCHAR(100) | Guide specialization | Guides only |
| `languages` | TEXT | Languages guide can speak | Guides only |
| `bio` | TEXT | Guide bio/description | Guides only |
| `certificate_path` | VARCHAR(500) | Path to certificate file | Guides only |
| `status` | VARCHAR(20) | Account status | All users |

### Status Values

| Status | Description | Who |
|---|---|---|
| `active` | Account is active | Travelers, Approved guides |
| `pending` | Waiting for approval | New guides |
| `rejected` | Application rejected | Rejected guides |
| `suspended` | Account suspended | Any user |

---

## 🔍 Verify Installation

### Check if columns exist:
```sql
SELECT column_name, data_type, character_maximum_length
FROM information_schema.columns
WHERE table_name = 'profiles'
AND column_name IN (
    'phone_number', 
    'experience', 
    'specialization', 
    'languages', 
    'bio', 
    'certificate_path', 
    'status'
)
ORDER BY column_name;
```

**Expected result:** 7 rows

### Check if indexes exist:
```sql
SELECT indexname, indexdef
FROM pg_indexes
WHERE tablename = 'profiles'
AND indexname LIKE 'idx_profiles_%';
```

**Expected result:** At least 3 indexes

### Check if view exists:
```sql
SELECT * FROM guide_applications LIMIT 1;
```

**Expected result:** No error (may be empty if no guides yet)

### Check if functions exist:
```sql
SELECT routine_name, routine_type
FROM information_schema.routines
WHERE routine_name IN ('approve_guide', 'reject_guide');
```

**Expected result:** 2 functions

---

## 🧪 Test Data

### Insert test guide (pending approval):
```sql
-- This will be done automatically via registration form
-- But you can test manually:

INSERT INTO profiles (
    id,
    email,
    full_name,
    phone_number,
    role,
    experience,
    specialization,
    languages,
    bio,
    certificate_path,
    status,
    created_at,
    updated_at
) VALUES (
    gen_random_uuid()::text,
    'test.guide@example.com',
    'Test Guide',
    '0901234567',
    'guide',
    '3-5',
    'cultural',
    'Tiếng Việt, English',
    'Experienced tour guide with passion for Vietnamese culture',
    '/uploads/certificates/test-cert.pdf',
    'pending',
    NOW(),
    NOW()
);
```

### View pending guides:
```sql
SELECT * FROM guide_applications 
WHERE status = 'pending'
ORDER BY created_at DESC;
```

### Approve a guide:
```sql
-- Replace 'guide-id-here' with actual guide ID
SELECT approve_guide('guide-id-here');
```

### Reject a guide:
```sql
-- Replace 'guide-id-here' with actual guide ID
SELECT reject_guide('guide-id-here', 'Certificate not valid');
```

---

## 🐛 Troubleshooting

### Error: "column already exists"
**Solution:** Column đã được thêm rồi, bỏ qua lỗi này hoặc chạy lại script (có `IF NOT EXISTS`)

### Error: "policy already exists"
**Solution:** Script sẽ tự động drop policy cũ trước khi tạo mới

### Error: "permission denied"
**Solution:** Đảm bảo bạn đang chạy với quyền admin trong Supabase

### Error: "syntax error at or near"
**Solution:** 
1. Copy từng phần script và chạy riêng
2. Kiểm tra version PostgreSQL (cần >= 12)
3. Đảm bảo không có ký tự đặc biệt trong copy/paste

---

## 📝 Rollback (If Needed)

### Remove new columns:
```sql
ALTER TABLE profiles 
DROP COLUMN IF EXISTS phone_number,
DROP COLUMN IF EXISTS experience,
DROP COLUMN IF EXISTS specialization,
DROP COLUMN IF EXISTS languages,
DROP COLUMN IF EXISTS bio,
DROP COLUMN IF EXISTS certificate_path,
DROP COLUMN IF EXISTS status;
```

### Remove indexes:
```sql
DROP INDEX IF EXISTS idx_profiles_phone_number;
DROP INDEX IF EXISTS idx_profiles_specialization;
DROP INDEX IF EXISTS idx_profiles_status;
```

### Remove view and functions:
```sql
DROP VIEW IF EXISTS guide_applications CASCADE;
DROP FUNCTION IF EXISTS approve_guide(TEXT);
DROP FUNCTION IF EXISTS reject_guide(TEXT, TEXT);
```

---

## ✅ Success Checklist

After running scripts, verify:

- [ ] 7 new columns added to profiles table
- [ ] 3 indexes created
- [ ] Comments added to columns
- [ ] guide_applications view created
- [ ] RLS policies updated
- [ ] approve_guide() function works
- [ ] reject_guide() function works
- [ ] Can register as guide via web form
- [ ] Guide status is 'pending' after registration
- [ ] Admin can view pending guides
- [ ] Admin can approve/reject guides

---

## 🔗 Related Files

- **Backend**: `Controllers/AuthApiController.cs`
- **Service**: `Services/SupabaseAuthService.cs`
- **Frontend**: `Views/Auth/Register.cshtml`
- **Documentation**: `ENHANCED_REGISTRATION_COMPLETE.md`

---

## 📞 Support

If you encounter issues:
1. Check Supabase logs in Dashboard
2. Verify PostgreSQL version
3. Check RLS policies are not conflicting
4. Review error messages carefully
5. Try running scripts step by step

---

**Last Updated**: June 1, 2026  
**Version**: 1.0  
**Status**: ✅ Ready to use