using Microsoft.AspNetCore.Mvc;
using TripMate_WebAPI.Services;

namespace TripMate_Webapi.Controllers
{
    public class GuideController : Controller
    {
        private readonly TourService _tourService;
        private readonly BookingService _bookingService;
        private readonly ILogger<GuideController> _logger;

        public GuideController(
            TourService tourService,
            BookingService bookingService,
            ILogger<GuideController> logger)
        {
            _tourService = tourService;
            _bookingService = bookingService;
            _logger = logger;
        }

        // GET: /Guide/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                // Load guide's tours
                var tours = await _tourService.GetToursAsync();
                
                // Prepare view model
                var viewModel = new GuideDashboardViewModel
                {
                    GuideName = "Guide User", // TODO: Get from auth
                    GuideRole = "Tour Guide",
                    DateRange = "Last 30 days",
                    
                    // Metrics
                    TotalEarnings = 45000000, // TODO: Calculate from bookings
                    EarningsGrowth = 12.5m,
                    ActiveTours = tours.Count(),
                    TotalBookings = 24, // TODO: Get from bookings
                    BookingProgress = 75,
                    AverageRating = 4.8m,
                    
                    // Tours
                    MyTours = tours.Take(5).ToList(),
                    
                    // Recent bookings
                    RecentBookings = new List<GuideBookingItem>
                    {
                        new GuideBookingItem { TravelerName = "Nguyễn Văn A", TourName = "Hà Nội - Hạ Long", Date = "15/06/2026", Status = "Confirmed", Amount = 2500000 },
                        new GuideBookingItem { TravelerName = "Trần Thị B", TourName = "Sapa 3N2Đ", Date = "18/06/2026", Status = "Pending", Amount = 3200000 },
                        new GuideBookingItem { TravelerName = "Lê Văn C", TourName = "Đà Nẵng - Hội An", Date = "20/06/2026", Status = "Confirmed", Amount = 1800000 },
                    },
                    
                    // Recent activities
                    RecentActivities = new List<ActivityItem>
                    {
                        new ActivityItem { Icon = "person_add", Title = "New Booking", Description = "Nguyễn Văn A booked Hà Nội tour", TimeAgo = "2 hours ago", IconBgClass = "bg-green-100", IconTextClass = "text-green-600" },
                        new ActivityItem { Icon = "star", Title = "New Review", Description = "5-star review from Trần Thị B", TimeAgo = "5 hours ago", IconBgClass = "bg-yellow-100", IconTextClass = "text-yellow-600" },
                        new ActivityItem { Icon = "check_circle", Title = "Tour Completed", Description = "Sapa tour successfully completed", TimeAgo = "1 day ago", IconBgClass = "bg-blue-100", IconTextClass = "text-blue-600" },
                        new ActivityItem { Icon = "payments", Title = "Payment Received", Description = "₫2,500,000 from booking #1234", TimeAgo = "2 days ago", IconBgClass = "bg-green-100", IconTextClass = "text-green-600" },
                    }
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading guide dashboard");
                return View(new GuideDashboardViewModel());
            }
        }

        // GET: /Guide/Profile
        public IActionResult Profile()
        {
            var profileData = new
            {
                FullName = "Trần Minh",
                PhoneNumber = "+84901234567",
                CityArea = "Hội An",
                Bio = "Xin chào! Mình là Minh, sinh ra và lớn lên tại phố cổ Hội An. Với hơn 5 năm kinh nghiệm dẫn tour, mình đam mê chia sẻ những câu chuyện văn hóa, lịch sử và đặc biệt là ẩm thực địa phương ẩn giấu sau những con hẻm nhỏ mà ít du khách biết tới.",
                Languages = new[] { "Tiếng Việt", "Tiếng Anh" },
                Specialties = new[] { "Ẩm thực đường phố", "Văn hóa - Lịch sử" },
                BaseRate = 150000,
                Rating = 4.9m,
                ReviewsCount = 124,
                IsVerified = true,
                AvatarUrl = "/images/AVATAR.png",
                CoverUrl = "https://images.unsplash.com/photo-1596422846543-75c6fc197f0a?auto=format&fit=crop&q=80&w=1000"
            };
            ViewBag.Profile = profileData;
            return View();
        }

        // GET: /Guide/Calendar
        public IActionResult Calendar()
        {
            return View();
        }

        // GET: /Guide/MyTours
        public IActionResult MyTours()
        {
            // Mock data for UI
            var tours = new List<dynamic>
            {
                new { 
                    Id = 1, 
                    Name = "Bình minh Mỹ Sơn + Ẩm thực địa phương", 
                    Duration = 4.5, 
                    MaxGuests = 6, 
                    Price = 1200000, 
                    Tags = new[] { "food", "culture", "hidden-gems" },
                    Bookings = 12,
                    Rating = 4.9,
                    IsActive = true
                },
                new { 
                    Id = 2, 
                    Name = "Khám phá phố cổ Hội An về đêm", 
                    Duration = 3.0, 
                    MaxGuests = 10, 
                    Price = 850000, 
                    Tags = new[] { "nightlife", "photography" },
                    Bookings = 34,
                    Rating = 4.8,
                    IsActive = true
                },
                new { 
                    Id = 3, 
                    Name = "Đạp xe đồng quê & Làng rau Trà Quế", 
                    Duration = 5.0, 
                    MaxGuests = 4, 
                    Price = 950000, 
                    Tags = new[] { "nature", "culture" },
                    Bookings = 8,
                    Rating = 5.0,
                    IsActive = false
                }
            };
            
            ViewBag.Tours = tours;
            return View();
        }

        // GET: /Guide/CreateTour
        public IActionResult CreateTour()
        {
            return View();
        }

        // GET: /Guide/Bookings
        public IActionResult Bookings()
        {
            var bookings = new List<dynamic>
            {
                new {
                    Id = "BK-2026-001A",
                    TravelerName = "Trần Thị Bảo Châu",
                    TravelerAvatar = "/images/AVATAR.png",
                    TravelerRating = 4.8,
                    TravelerLocation = "TP.HCM",
                    TourName = "Bình minh Mỹ Sơn + Ẩm thực địa phương",
                    Date = "15/06/2026",
                    Time = "06:00",
                    Guests = 2,
                    TotalAmount = 1000000,
                    PlatformFee = 150000,
                    NetEarnings = 850000,
                    Note = "Chúng tôi muốn chụp ảnh hoàng hôn",
                    Status = "Pending",
                    SecondsRemaining = 3600 // For countdown
                },
                new {
                    Id = "BK-2026-002B",
                    TravelerName = "Nguyễn Văn A",
                    TravelerAvatar = "/images/AVATAR.png",
                    TravelerRating = 5.0,
                    TravelerLocation = "Hà Nội",
                    TourName = "Khám phá phố cổ Hội An về đêm",
                    Date = "16/06/2026",
                    Time = "18:00",
                    Guests = 4,
                    TotalAmount = 1500000,
                    PlatformFee = 225000,
                    NetEarnings = 1275000,
                    Note = "",
                    Status = "Confirmed",
                    SecondsRemaining = 0
                },
                new {
                    Id = "BK-2026-003C",
                    TravelerName = "Lê Thị C",
                    TravelerAvatar = "/images/AVATAR.png",
                    TravelerRating = 4.5,
                    TravelerLocation = "Đà Nẵng",
                    TourName = "Đạp xe đồng quê & Làng rau Trà Quế",
                    Date = "10/06/2026",
                    Time = "08:00",
                    Guests = 2,
                    TotalAmount = 950000,
                    PlatformFee = 142500,
                    NetEarnings = 807500,
                    Note = "",
                    Status = "Completed",
                    SecondsRemaining = 0
                }
            };
            ViewBag.Bookings = bookings;
            return View();
        }

        // GET: /Guide/Messages
        public IActionResult Messages()
        {
            // Mock data for UI
            var threads = new List<dynamic>
            {
                new {
                    Id = "t1",
                    TravelerName = "Trần Thị Bảo Châu",
                    TravelerAvatar = "/images/AVATAR.png",
                    TourName = "Bình minh Mỹ Sơn + Ẩm thực địa phương",
                    Date = "Thứ 7, 15/06/2026",
                    LastMessage = "Hẹn gặp lúc 6h nhé",
                    TimeAgo = "2 phút trước",
                    UnreadCount = 2,
                    IsLocked = false,
                    Messages = new List<dynamic>
                    {
                        new { Text = "Xin chào Guide Minh!", IsMine = false, Time = "08:15", IsRead = true },
                        new { Text = "Chào bạn Châu! Mình đã nhận được booking của bạn rồi nhé.", IsMine = true, Time = "08:16", IsRead = true },
                        new { Text = "Bạn có thể đón mình tại khách sạn ở phố cổ được không?", IsMine = false, Time = "08:18", IsRead = true },
                        new { Text = "Dạ được nhé, bạn cho mình xin địa chỉ cụ thể nha.", IsMine = true, Time = "08:20", IsRead = true },
                        new { Text = "Mình ở Mường Thanh Holiday, số 15 Âu Cơ", IsMine = false, Time = "09:00", IsRead = false },
                        new { Text = "Hẹn gặp lúc 6h nhé", IsMine = false, Time = "09:01", IsRead = false }
                    }
                },
                new {
                    Id = "t2",
                    TravelerName = "Nguyễn Văn Hùng Anh",
                    TravelerAvatar = "/images/AVATAR.png",
                    TourName = "Khám phá phố cổ Hội An về đêm",
                    Date = "Chủ Nhật, 16/06/2026",
                    LastMessage = "Cảm ơn bạn rất nhiều!",
                    TimeAgo = "Hôm qua",
                    UnreadCount = 0,
                    IsLocked = false,
                    Messages = new List<dynamic>
                    {
                        new { Text = "Cảm ơn bạn rất nhiều!", IsMine = false, Time = "10:00", IsRead = true }
                    }
                },
                new {
                    Id = "t3",
                    TravelerName = "Lê Hoàng Phúc",
                    TravelerAvatar = "/images/AVATAR.png",
                    TourName = "Đạp xe đồng quê & Làng rau Trà Quế",
                    Date = "Thứ 2, 17/06/2026",
                    LastMessage = "Booking chưa xác nhận",
                    TimeAgo = "1 giờ trước",
                    UnreadCount = 0,
                    IsLocked = true, // Locked because it's pending
                    Messages = new List<dynamic>()
                }
            };
            
            ViewBag.Threads = threads;
            return View();
        }

        // GET: /Guide/Earnings
        public IActionResult Earnings()
        {
            var stats = new
            {
                Received = 8500000,
                Pending = 1700000,
                CompletedTours = 8,
                AverageRating = 4.9m
            };

            var chartData = new
            {
                Labels = new[] { "T7/25", "T8/25", "T9/25", "T10/25", "T11/25", "T12/25", "T1/26", "T2/26", "T3/26", "T4/26", "T5/26", "T6/26" },
                Data = new[] { 500000, 800000, 450000, 1200000, 950000, 1500000, 1800000, 1100000, 900000, 1300000, 2100000, 1700000 }
            };

            var transactions = new List<dynamic>
            {
                new { Id = "BKG-260612", Date = "12/06/26", TravelerName = "Bảo Châu", TourName = "Bình minh Mỹ Sơn + Ẩm thực", TotalAmount = 1000000, PlatformFee = 150000, NetEarnings = 850000, Status = "Completed", ReleaseDate = "13/06/26" },
                new { Id = "BKG-260610", Date = "10/06/26", TravelerName = "Hùng Anh", TourName = "Khám phá phố cổ Hội An về đêm", TotalAmount = 800000, PlatformFee = 120000, NetEarnings = 680000, Status = "Completed", ReleaseDate = "11/06/26" },
                new { Id = "BKG-260608", Date = "08/06/26", TravelerName = "Linh Nguyễn", TourName = "Đạp xe đồng quê Trà Quế", TotalAmount = 600000, PlatformFee = 90000, NetEarnings = 510000, Status = "Pending", ReleaseDate = "Dự kiến 15/06/26" },
                new { Id = "BKG-260601", Date = "01/06/26", TravelerName = "Minh Tuấn", TourName = "Bình minh Mỹ Sơn + Ẩm thực", TotalAmount = 1000000, PlatformFee = 150000, NetEarnings = 850000, Status = "Completed", ReleaseDate = "02/06/26" },
                new { Id = "BKG-260525", Date = "25/05/26", TravelerName = "Anna Smith", TourName = "Khám phá phố cổ Hội An về đêm", TotalAmount = 1200000, PlatformFee = 180000, NetEarnings = 1020000, Status = "Completed", ReleaseDate = "26/05/26" }
            };

            ViewBag.Stats = stats;
            ViewBag.ChartData = chartData;
            ViewBag.Transactions = transactions;

            return View();
        }
    }

    // View Models
    public class GuideDashboardViewModel
    {
        public string GuideName { get; set; } = string.Empty;
        public string GuideRole { get; set; } = string.Empty;
        public string DateRange { get; set; } = string.Empty;
        
        // Metrics
        public decimal TotalEarnings { get; set; }
        public decimal EarningsGrowth { get; set; }
        public int ActiveTours { get; set; }
        public int TotalBookings { get; set; }
        public int BookingProgress { get; set; }
        public decimal AverageRating { get; set; }
        
        // Data
        public List<ExperiencePackageRow> MyTours { get; set; } = new();
        public List<GuideBookingItem> RecentBookings { get; set; } = new();
        public List<ActivityItem> RecentActivities { get; set; } = new();
    }

    public class GuideBookingItem
    {
        public string TravelerName { get; set; } = string.Empty;
        public string TourName { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }
}
