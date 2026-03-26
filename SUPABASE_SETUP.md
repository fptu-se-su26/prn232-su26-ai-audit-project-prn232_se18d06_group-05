# Hướng dẫn Setup Supabase cho TripMate

## 📋 Tổng quan

TripMate sử dụng Supabase làm backend với các tính năng:
- Authentication (JWT-based)
- PostgreSQL Database
- Realtime subscriptions
- Storage (future)

## 🔐 Thông tin kết nối

```
Project URL: https://nvbvvowyjzylllswhynv.supabase.co
Anon Key: sb_publishable_ZbSsVM4M0xZJa4PyobDMkw_cazDhnr2
```

## 🗄️ Database Schema

### 1. Bảng `profiles` (User Profiles)

```sql
-- Create profiles table
CREATE TABLE profiles (
  id UUID REFERENCES auth.users ON DELETE CASCADE PRIMARY KEY,
  email TEXT UNIQUE NOT NULL,
  full_name TEXT,
  phone TEXT,
  avatar_url TEXT,
  role TEXT DEFAULT 'traveler' CHECK (role IN ('traveler', 'guide', 'admin')),
  created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
  updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Enable RLS
ALTER TABLE profiles ENABLE ROW LEVEL SECURITY;

-- Policies
CREATE POLICY "Public profiles are viewable by everyone"
  ON profiles FOR SELECT
  USING (true);

CREATE POLICY "Users can update own profile"
  ON profiles FOR UPDATE
  USING (auth.uid() = id);

CREATE POLICY "Users can insert own profile"
  ON profiles FOR INSERT
  WITH CHECK (auth.uid() = id);
```

### 2. Bảng `tours` (Tour Listings)

```sql
-- Create tours table
CREATE TABLE tours (
  id UUID DEFAULT gen_random_uuid() PRIMARY KEY,
  guide_id UUID REFERENCES profiles(id) ON DELETE CASCADE NOT NULL,
  title TEXT NOT NULL,
  description TEXT,
  location TEXT NOT NULL,
  price DECIMAL(10, 2) NOT NULL,
  duration_hours INTEGER NOT NULL,
  max_participants INTEGER DEFAULT 10,
  images TEXT[], -- Array of image URLs
  rating DECIMAL(3, 2) DEFAULT 0,
  total_reviews INTEGER DEFAULT 0,
  status TEXT DEFAULT 'active' CHECK (status IN ('active', 'inactive', 'archived')),
  created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
  updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Enable RLS
ALTER TABLE tours ENABLE ROW LEVEL SECURITY;

-- Policies
CREATE POLICY "Tours are viewable by everyone"
  ON tours FOR SELECT
  USING (status = 'active');

CREATE POLICY "Guides can create tours"
  ON tours FOR INSERT
  WITH CHECK (auth.uid() = guide_id AND (
    SELECT role FROM profiles WHERE id = auth.uid()
  ) IN ('guide', 'admin'));

CREATE POLICY "Guides can update own tours"
  ON tours FOR UPDATE
  USING (auth.uid() = guide_id);

CREATE POLICY "Guides can delete own tours"
  ON tours FOR DELETE
  USING (auth.uid() = guide_id);
```

### 3. Bảng `bookings` (Tour Bookings)

```sql
-- Create bookings table
CREATE TABLE bookings (
  id UUID DEFAULT gen_random_uuid() PRIMARY KEY,
  tour_id UUID REFERENCES tours(id) ON DELETE CASCADE NOT NULL,
  user_id UUID REFERENCES profiles(id) ON DELETE CASCADE NOT NULL,
  booking_date DATE NOT NULL,
  participants INTEGER NOT NULL DEFAULT 1,
  total_price DECIMAL(10, 2) NOT NULL,
  status TEXT DEFAULT 'pending' CHECK (status IN ('pending', 'confirmed', 'completed', 'cancelled')),
  notes TEXT,
  created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
  updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Enable RLS
ALTER TABLE bookings ENABLE ROW LEVEL SECURITY;

-- Policies
CREATE POLICY "Users can view own bookings"
  ON bookings FOR SELECT
  USING (auth.uid() = user_id OR auth.uid() IN (
    SELECT guide_id FROM tours WHERE id = tour_id
  ));

CREATE POLICY "Users can create bookings"
  ON bookings FOR INSERT
  WITH CHECK (auth.uid() = user_id);

CREATE POLICY "Users can update own bookings"
  ON bookings FOR UPDATE
  USING (auth.uid() = user_id);

CREATE POLICY "Guides can update tour bookings"
  ON bookings FOR UPDATE
  USING (auth.uid() IN (
    SELECT guide_id FROM tours WHERE id = tour_id
  ));
```

### 4. Bảng `reviews` (Tour Reviews)

```sql
-- Create reviews table
CREATE TABLE reviews (
  id UUID DEFAULT gen_random_uuid() PRIMARY KEY,
  tour_id UUID REFERENCES tours(id) ON DELETE CASCADE NOT NULL,
  user_id UUID REFERENCES profiles(id) ON DELETE CASCADE NOT NULL,
  booking_id UUID REFERENCES bookings(id) ON DELETE CASCADE,
  rating INTEGER NOT NULL CHECK (rating >= 1 AND rating <= 5),
  comment TEXT,
  created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
  updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
  UNIQUE(booking_id) -- One review per booking
);

-- Enable RLS
ALTER TABLE reviews ENABLE ROW LEVEL SECURITY;

-- Policies
CREATE POLICY "Reviews are viewable by everyone"
  ON reviews FOR SELECT
  USING (true);

CREATE POLICY "Users can create reviews for completed bookings"
  ON reviews FOR INSERT
  WITH CHECK (
    auth.uid() = user_id AND
    EXISTS (
      SELECT 1 FROM bookings
      WHERE id = booking_id
      AND user_id = auth.uid()
      AND status = 'completed'
    )
  );

CREATE POLICY "Users can update own reviews"
  ON reviews FOR UPDATE
  USING (auth.uid() = user_id);
```

## 🔄 Functions & Triggers

### Auto-update `updated_at` timestamp

```sql
-- Function to update updated_at
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
  NEW.updated_at = NOW();
  RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Apply to all tables
CREATE TRIGGER update_profiles_updated_at
  BEFORE UPDATE ON profiles
  FOR EACH ROW
  EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_tours_updated_at
  BEFORE UPDATE ON tours
  FOR EACH ROW
  EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_bookings_updated_at
  BEFORE UPDATE ON bookings
  FOR EACH ROW
  EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_reviews_updated_at
  BEFORE UPDATE ON reviews
  FOR EACH ROW
  EXECUTE FUNCTION update_updated_at_column();
```

### Auto-create profile on signup

```sql
-- Function to create profile on signup
CREATE OR REPLACE FUNCTION public.handle_new_user()
RETURNS TRIGGER AS $$
BEGIN
  INSERT INTO public.profiles (id, email, full_name)
  VALUES (
    NEW.id,
    NEW.email,
    NEW.raw_user_meta_data->>'full_name'
  );
  RETURN NEW;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- Trigger on auth.users
CREATE TRIGGER on_auth_user_created
  AFTER INSERT ON auth.users
  FOR EACH ROW
  EXECUTE FUNCTION public.handle_new_user();
```

### Update tour rating on review

```sql
-- Function to update tour rating
CREATE OR REPLACE FUNCTION update_tour_rating()
RETURNS TRIGGER AS $$
BEGIN
  UPDATE tours
  SET
    rating = (
      SELECT AVG(rating)::DECIMAL(3,2)
      FROM reviews
      WHERE tour_id = NEW.tour_id
    ),
    total_reviews = (
      SELECT COUNT(*)
      FROM reviews
      WHERE tour_id = NEW.tour_id
    )
  WHERE id = NEW.tour_id;
  RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Trigger on reviews
CREATE TRIGGER update_tour_rating_on_review
  AFTER INSERT OR UPDATE OR DELETE ON reviews
  FOR EACH ROW
  EXECUTE FUNCTION update_tour_rating();
```

## 📊 Sample Data (Optional)

```sql
-- Insert sample tours (after creating a guide user)
INSERT INTO tours (guide_id, title, description, location, price, duration_hours, max_participants)
VALUES
  (
    'YOUR_GUIDE_USER_ID',
    'Khám phá Hà Nội Phố Cổ',
    'Tour tham quan khu phố cổ Hà Nội với hướng dẫn viên địa phương',
    'Hà Nội',
    500000,
    4,
    15
  ),
  (
    'YOUR_GUIDE_USER_ID',
    'Vịnh Hạ Long 1 ngày',
    'Khám phá kỳ quan thiên nhiên thế giới Vịnh Hạ Long',
    'Quảng Ninh',
    1500000,
    8,
    20
  );
```

## 🔔 Realtime Setup

Enable realtime for bookings table:

```sql
-- Enable realtime for bookings
ALTER PUBLICATION supabase_realtime ADD TABLE bookings;
```

## ✅ Checklist

- [ ] Tạo tất cả các bảng (profiles, tours, bookings, reviews)
- [ ] Enable Row Level Security cho tất cả bảng
- [ ] Tạo policies cho từng bảng
- [ ] Tạo functions và triggers
- [ ] Enable realtime cho bảng bookings
- [ ] Test authentication flow
- [ ] Insert sample data (optional)

## 🔗 Useful Links

- [Supabase Dashboard](https://supabase.com/dashboard/project/nvbvvowyjzylllswhynv)
- [Supabase Docs](https://supabase.com/docs)
- [PostgreSQL Docs](https://www.postgresql.org/docs/)

## 📝 Notes

- Tất cả timestamps sử dụng `TIMESTAMP WITH TIME ZONE`
- RLS (Row Level Security) được enable cho tất cả bảng
- Sử dụng UUID cho primary keys
- Foreign keys có `ON DELETE CASCADE` để tự động xóa dữ liệu liên quan
