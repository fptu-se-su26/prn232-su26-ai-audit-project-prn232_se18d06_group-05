# TripMate - Personality Survey Flow

## 📋 Tổng quan

Hệ thống khảo sát tính cách MBTI được thiết kế để giúp du khách tìm được hướng dẫn viên phù hợp nhất với tính cách của họ.

## 🔄 Flow hoạt động

### 1. Đăng ký tài khoản
```
User → Register Page → Chọn role "Du khách" → Submit
```

### 2. Đăng nhập lần đầu
```
User → Login Page → Đăng nhập thành công
  ↓
Check: role === 'traveler' && !surveyCompleted
  ↓
YES → Redirect to /personality-survey.html
NO  → Redirect to /index.html
```

### 3. Hoàn thành khảo sát
```
Personality Survey Page
  ↓
Welcome Screen (giới thiệu)
  ↓
20 câu hỏi đánh giá tính cách
  ↓
Completion Screen
  ↓
Submit → Save to localStorage → Redirect to /index.html
```

### 4. Lần đăng nhập tiếp theo
```
User → Login → Check surveyCompleted === 'true'
  ↓
Redirect to /index.html (bỏ qua survey)
```

## 📊 Cấu trúc khảo sát

### Số lượng câu hỏi: 20
### Thang đo: 1-5 (Likert Scale)
- 1: Hoàn toàn không đồng ý
- 2: Không đồng ý
- 3: Trung lập
- 4: Đồng ý
- 5: Hoàn toàn đồng ý

### Các chiều đánh giá:

1. **Extroversion (Hướng ngoại)**
   - Câu hỏi: 1, 4, 18
   - Đánh giá: Mức độ thích giao tiếp, gặp gỡ người mới

2. **Planning (Lập kế hoạch)**
   - Câu hỏi: 2, 7, 10
   - Đánh giá: Mức độ chuẩn bị và tổ chức

3. **Adventure (Phiêu lưu)**
   - Câu hỏi: 3, 6, 12
   - Đánh giá: Mức độ thích mạo hiểm và khám phá

4. **Cultural (Văn hóa)**
   - Câu hỏi: 5, 11, 20
   - Đánh giá: Quan tâm đến văn hóa, lịch sử

5. **Social (Xã hội)**
   - Câu hỏi: 8, 14, 17
   - Đánh giá: Mức độ tương tác xã hội

## 💾 Dữ liệu lưu trữ

### LocalStorage Keys:

```javascript
{
  "surveyCompleted": "true",
  "personalityProfile": {
    "extroversion": 80,      // 0-100
    "planning": 60,          // 0-100
    "adventure": 90,         // 0-100
    "cultural": 70,          // 0-100
    "social": 85,            // 0-100
    "answers": [5,4,5,3,4,5,3,5,4,2,4,5,3,4,3,5,4,2,5,4]
  }
}
```

## 🎯 Sử dụng kết quả

### Matching Algorithm (Tương lai)

Kết quả khảo sát sẽ được sử dụng để:

1. **Gợi ý hướng dẫn viên phù hợp**
   - So sánh profile của traveler với profile của guide
   - Tính điểm tương đồng
   - Hiển thị top matches

2. **Cá nhân hóa trải nghiệm**
   - Gợi ý tour phù hợp với tính cách
   - Điều chỉnh nội dung hiển thị
   - Tùy chỉnh giao diện

3. **Phân tích và báo cáo**
   - Thống kê xu hướng người dùng
   - Cải thiện thuật toán matching
   - Tối ưu trải nghiệm

## 🔧 Technical Implementation

### Frontend
- **Framework**: Vanilla JavaScript + Tailwind CSS
- **Storage**: LocalStorage
- **Validation**: Client-side validation
- **UX**: Progress bar, smooth transitions

### Backend (Tương lai)
- **API Endpoint**: `POST /api/personality/submit`
- **Database**: Lưu vào bảng `traveler_personality`
- **Processing**: Tính toán điểm số và phân loại

### Database Schema (Đề xuất)

```sql
CREATE TABLE traveler_personality (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  traveler_id UUID NOT NULL REFERENCES profiles(id),
  extroversion INTEGER CHECK (extroversion >= 0 AND extroversion <= 100),
  planning INTEGER CHECK (planning >= 0 AND planning <= 100),
  adventure INTEGER CHECK (adventure >= 0 AND adventure <= 100),
  cultural INTEGER CHECK (cultural >= 0 AND cultural <= 100),
  social INTEGER CHECK (social >= 0 AND social <= 100),
  answers JSONB,
  created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
  updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
  UNIQUE(traveler_id)
);
```

## 📱 UI/UX Features

### Welcome Screen
- Giới thiệu mục đích khảo sát
- Thời gian hoàn thành: 3-5 phút
- 3 benefits cards
- CTA button: "Bắt đầu khảo sát"

### Survey Screen
- Progress bar ở top
- Question counter
- 5-point Likert scale với màu sắc
- Previous/Next navigation
- Disabled state cho chưa trả lời

### Completion Screen
- Success icon
- Thank you message
- CTA button: "Hoàn tất và khám phá"

## 🎨 Design System

### Colors
- Primary: Orange (#FF6B35)
- Secondary: Blue (#004E89)
- Success: Green
- Neutral: Gray scale

### Components
- Glass morphism cards
- Material Icons
- Smooth animations
- Responsive grid

## 🚀 Future Enhancements

1. **Backend Integration**
   - Save to database
   - API endpoints
   - Real-time sync

2. **Advanced Matching**
   - ML-based recommendations
   - Collaborative filtering
   - Personality compatibility score

3. **Retake Survey**
   - Allow users to retake after 6 months
   - Track personality changes
   - Compare results

4. **Analytics Dashboard**
   - Admin view of user personalities
   - Trends and insights
   - Guide matching success rate

## 📝 Testing Checklist

- [ ] Survey appears for new travelers
- [ ] Survey skipped for returning travelers
- [ ] Survey skipped for guides
- [ ] All 20 questions display correctly
- [ ] Progress bar updates
- [ ] Previous/Next buttons work
- [ ] Can't proceed without answering
- [ ] Data saved to localStorage
- [ ] Redirect to home after completion
- [ ] Responsive on mobile
- [ ] Smooth animations
- [ ] No console errors

## 🔒 Privacy & Security

- Dữ liệu chỉ lưu trên client (LocalStorage)
- Không gửi lên server (hiện tại)
- User có thể xóa dữ liệu bất kỳ lúc nào
- Tuân thủ GDPR (khi triển khai backend)

## 📞 Support

Nếu có vấn đề với khảo sát:
1. Xóa localStorage và thử lại
2. Kiểm tra console log
3. Liên hệ support team
