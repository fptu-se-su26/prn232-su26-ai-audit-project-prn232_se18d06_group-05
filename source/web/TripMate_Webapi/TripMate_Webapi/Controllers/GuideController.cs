using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using TripMate_WebAPI.Services;
using TripMate_WebAPI.DTOs.Tour.Requests;
using TripMate_Webapi.Repositories;

namespace TripMate_Webapi.Controllers
{
    [Route("Guide/[action]")]
    public class GuideController : Controller
    {
        private readonly TourService _tourService;
        private readonly BookingService _bookingService;
        private readonly IGuideRepository _guideRepository;
        private readonly IExperienceService _experienceService;
        private readonly ILogger<GuideController> _logger;
        private readonly Supabase.Client _supabase;

        public GuideController(
            TourService tourService,
            BookingService bookingService,
            IGuideRepository guideRepository,
            IExperienceService experienceService,
            ILogger<GuideController> logger,
            Supabase.Client supabase)
        {
            _tourService = tourService;
            _bookingService = bookingService;
            _guideRepository = guideRepository;
            _experienceService = experienceService;
            _logger = logger;
            _supabase = supabase;
        }

        // GET: /Guide/Index (List of all Guides for Traveler)
        public IActionResult Index()
        {
            return View();
        }

        // GET: /Guide/TripRequests (For Guides to see public requests)
        public IActionResult TripRequests()
        {
            return View();
        }

        // GET: /Guide/Dashboard
        [Authorize(Roles = "guide")]
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
        [Authorize(Roles = "guide")]
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
        [Authorize(Roles = "guide")]
        public IActionResult Calendar()
        {
            return View();
        }

        // Helper method to get the guide profile ID of the currently logged-in user
        private async Task<string?> GetCurrentGuideProfileIdAsync()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      ?? User.FindFirst("sub")?.Value;

            Console.WriteLine($"[GetCurrentGuideProfileIdAsync] Extracted userId: '{userId}'");

            if (string.IsNullOrEmpty(userId)) return null;

            try 
            {
                var guide = await _guideRepository.GetGuideByIdAsync(userId);
                Console.WriteLine($"[GetCurrentGuideProfileIdAsync] Found guide profile with Id: '{guide?.Id}' for userId: '{userId}'");
                return guide?.Id;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GetCurrentGuideProfileIdAsync] Exception fetching guide profile for userId '{userId}': {ex.Message}");
                return null;
            }
        }

        // GET: /Guide/MyTours
        [Authorize(Roles = "guide")]
        public async Task<IActionResult> MyTours()
        {
            var guideProfileId = await GetCurrentGuideProfileIdAsync();
            if (string.IsNullOrEmpty(guideProfileId))
            {
                // If the user is logged in but doesn't have a guide profile, they shouldn't be here
                // Redirecting to an error page or Dashboard
                return RedirectToAction("Index", "Home"); 
            }

            var tours = await _experienceService.GetMyToursAsync(guideProfileId);
            
            ViewBag.Tours = tours;
            return View();
        }

        [HttpPatch("/ToggleTourStatus/{id}")]
        [Authorize(Roles = "guide")]
        public async Task<IActionResult> ToggleTourStatus(string id)
        {
            try
            {
                var guideProfileId = await GetCurrentGuideProfileIdAsync();
                if (string.IsNullOrEmpty(guideProfileId)) return Unauthorized(new { success = false, message = "Không có quyền thực hiện thao tác này" });

                var success = await _experienceService.ToggleTourStatusAsync(id, guideProfileId);
                
                if (success) return Ok(new { success = true, message = "Cập nhật trạng thái thành công" });
                return BadRequest(new { success = false, message = "Không tìm thấy gói trải nghiệm" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpDelete("/DeleteTour/{id}")]
        [Authorize(Roles = "guide")]
        public async Task<IActionResult> DeleteTour(string id)
        {
            try
            {
                var guideProfileId = await GetCurrentGuideProfileIdAsync();
                if (string.IsNullOrEmpty(guideProfileId)) return Unauthorized(new { success = false, message = "Không có quyền thực hiện thao tác này" });

                var success = await _experienceService.DeleteTourAsync(id, guideProfileId);
                
                if (success) return Ok(new { success = true, message = "Đã xóa gói trải nghiệm" });
                return BadRequest(new { success = false, message = "Xóa thất bại" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("/DuplicateTour/{id}")]
        [Authorize(Roles = "guide")]
        public async Task<IActionResult> DuplicateTour(string id)
        {
            try
            {
                var guideProfileId = await GetCurrentGuideProfileIdAsync();
                if (string.IsNullOrEmpty(guideProfileId)) return Unauthorized(new { success = false, message = "Không có quyền thực hiện thao tác này" });

                var newTour = await _experienceService.DuplicateTourAsync(id, guideProfileId);
                
                if (newTour != null) return Ok(new { success = true, message = "Nhân bản thành công" });
                return BadRequest(new { success = false, message = "Không tìm thấy gói trải nghiệm" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // GET: /Guide/CreateTour
        [HttpGet]
        [Authorize(Roles = "guide")]
        public async Task<IActionResult> CreateTour(string? id = null)
        {
            if (string.IsNullOrEmpty(id))
            {
                ViewBag.IsEdit = false;
                return View();
            }

            var guideProfileId = await GetCurrentGuideProfileIdAsync();
            if (string.IsNullOrEmpty(guideProfileId)) return Unauthorized();

            var tour = await _experienceService.GetPackageByIdAsync(id, guideProfileId);
            if (tour == null) return NotFound();

            ViewBag.IsEdit = true;
            
            var tourDto = new 
            {
                id = tour.Id,
                title = tour.Title,
                durationHours = tour.DurationHours,
                maxGroupSize = tour.MaxGroupSize,
                city = tour.City,
                meetingPoint = tour.MeetingPoint,
                description = tour.Description,
                pricePerSession = tour.PricePerSession,
                pricePerPerson = tour.PricePerPerson,
                timelineJson = tour.TimelineJson,
                languages = tour.Languages,
                includedItems = tour.IncludedItems,
                tags = tour.Tags,
                coverImageUrl = tour.CoverImageUrl,
                galleryImageUrls = tour.GalleryImageUrls
            };
            
            ViewBag.TourData = System.Text.Json.JsonSerializer.Serialize(tourDto, new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase });
            return View();
        }

        // POST: /Guide/CreateTour
        [HttpPost]
        [Authorize(Roles = "guide")]
        public async Task<IActionResult> CreateTour([FromForm] CreateTourDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ." });
                }

                var guideProfileId = await GetCurrentGuideProfileIdAsync();
                if (string.IsNullOrEmpty(guideProfileId)) return Unauthorized(new { success = false, message = "Bạn cần có hồ sơ hướng dẫn viên để tạo gói trải nghiệm." });

                var createdTour = await _experienceService.CreateTourAsync(dto, guideProfileId);

                return Ok(new { success = true, data = new { id = createdTour.Id }, message = "Tuyệt vời! Gói trải nghiệm của bạn đã được xuất bản." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating tour");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // GET: /Guide/Bookings
        [Authorize(Roles = "guide")]
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

        // GET: /Guide/Notifications
        public IActionResult Notifications()
        {
            var notifications = new List<dynamic>
            {
                new { Id = 1, Type = "booking_new", Title = "Booking mới từ Trần Thị Bảo Châu", Message = "Đặt 'Bình minh Mỹ Sơn' vào 15/06/2026. Phản hồi trong 24h", Time = "10 phút trước", IsRead = false },
                new { Id = 2, Type = "payment", Title = "Thanh toán đã được giải ngân", Message = "850,000đ từ booking #BK-2024-008 đã về ví", Time = "1 giờ trước", IsRead = false },
                new { Id = 3, Type = "review", Title = "Đánh giá mới 5 sao từ Nguyễn Văn An", Message = "\"Guide rất nhiệt tình, rất recommend!\"", Time = "Hôm qua 14:23", IsRead = true },
                new { Id = 4, Type = "admin", Title = "Tài khoản của bạn đã được xác minh", Message = "Chào mừng bạn đến với TripMate Local Guide. Bắt đầu tạo tour ngay nhé!", Time = "2 ngày trước", IsRead = true }
            };
            ViewBag.Notifications = notifications;
            return View();
        }

        // GET: /Guide/Support
        public IActionResult Support()
        {
            var tickets = new List<dynamic>
            {
                new { Id = "TK-001", Title = "Khách hủy sát giờ", Status = "Pending", Date = "12/06/2026" },
                new { Id = "TK-002", Title = "Lỗi không nhận được tiền giải ngân", Status = "Resolved", Date = "05/06/2026" }
            };
            ViewBag.Tickets = tickets;
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
