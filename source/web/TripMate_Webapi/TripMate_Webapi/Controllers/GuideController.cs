using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using TripMate_WebAPI.Services;
using TripMate_Webapi.Entities;
using TripMate_WebAPI.DTOs.Tour.Requests;
using TripMate_Webapi.Repositories;
using TripMate_WebAPI.DTOs;

namespace TripMate_Webapi.Controllers
{
    [Route("Guide/[action]")]
    public class GuideController : Controller
    {
        private readonly TourService _tourService;
        private readonly BookingService _bookingService;
        private readonly IGuideRepository _guideRepository;
        private readonly IExperienceService _experienceService;
        private readonly ICalendarService _calendarService;
        private readonly ILogger<GuideController> _logger;
        private readonly Supabase.Client _supabase;
        private readonly IGuideDashboardService _dashboardService;

        public GuideController(
            TourService tourService,
            BookingService bookingService,
            IGuideRepository guideRepository,
            IExperienceService experienceService,
            ICalendarService calendarService,
            ILogger<GuideController> logger,
            Supabase.Client supabase,
            IGuideDashboardService dashboardService)
        {
            _tourService = tourService;
            _bookingService = bookingService;
            _guideRepository = guideRepository;
            _experienceService = experienceService;
            _calendarService = calendarService;
            _logger = logger;
            _supabase = supabase;
            _dashboardService = dashboardService;
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
        [HttpGet("Guide/Dashboard")]
        [Authorize(Roles = "guide")]
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("Login", "Auth");
                }

                var viewModel = await _dashboardService.BuildDashboardAsync(userId);
                
                // For the sparkline JS in the view
                var sparklineJson = System.Text.Json.JsonSerializer.Serialize(viewModel.EarningsSparkline);
                ViewBag.SparklineJson = sparklineJson;
                
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Guide Dashboard");
                return View(new GuideDashboardViewModel()); // Return empty on error to not crash UI
            }
        }

        // GET: /Guide/Profile/{id}
        // This is the public profile viewed by the Traveler
        [HttpGet("Guide/Profile/{id}")]
        public IActionResult Profile(string id)
        {
            return RedirectToAction("GuideProfile", "Traveler", new { id });
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

        // ponytail ultra: minimal inline update
        public class UpdateGuideProfileDto
        {
            public string? AvatarUrl { get; set; }
            public string? Location { get; set; }
            public string? Bio { get; set; }
            public List<string>? Languages { get; set; }
            public List<string>? Specialties { get; set; }
            public string? CityArea { get; set; }
            public decimal? PricePerHour { get; set; }
            public string? CoverPhotoUrl { get; set; }
            // ponytail: certificate, phone number, full name, email explicitly excluded per requirements
        }

        [HttpPost("Guide/UpdateProfile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateGuideProfileDto dto, [FromServices] Supabase.Client supabase)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var profileResponse = await supabase.From<ProfileEntity>().Where(x => x.Id == userId).Get();
            var profile = profileResponse.Models.FirstOrDefault();
            if (profile != null)
            {
                if (dto.AvatarUrl != null) profile.AvatarUrl = dto.AvatarUrl;
                if (dto.Location != null) profile.Location = dto.Location;
                await supabase.From<ProfileEntity>().Update(profile);
            }

            var guideProfileResponse = await supabase.From<GuideProfileEntity>().Where(x => x.UserId == userId).Get();
            var guideProfile = guideProfileResponse.Models.FirstOrDefault();
            if (guideProfile != null)
            {
                if (dto.Bio != null) guideProfile.Bio = dto.Bio;
                if (dto.Languages != null) guideProfile.Languages = dto.Languages;
                if (dto.Specialties != null) guideProfile.Specialties = dto.Specialties;
                if (dto.CityArea != null) guideProfile.CityArea = dto.CityArea;
                if (dto.PricePerHour != null) guideProfile.PricePerHour = dto.PricePerHour;
                if (dto.CoverPhotoUrl != null) guideProfile.CoverPhotoUrl = dto.CoverPhotoUrl;
                await supabase.From<GuideProfileEntity>().Update(guideProfile);
            }

            return Ok(new { success = true });
        }

        // GET: /Guide/Calendar
        [Authorize(Roles = "guide")]
        public IActionResult Calendar()
        {
            return View();
        }

        // GET: /Guide/GetCalendarData
        [HttpGet, Authorize(Roles = "guide")]
        public async Task<IActionResult> GetCalendarData(string start, string end)
        {
            var guideProfileId = await GetCurrentGuideProfileIdAsync();
            if (guideProfileId == null) return Unauthorized();
            
            var data = await _calendarService.GetCalendarDataAsync(guideProfileId, start, end);
            return Json(data);
        }

        // POST: /Guide/SaveBlockedDates
        [HttpPost, Authorize(Roles = "guide")]
        public async Task<IActionResult> SaveBlockedDates([FromBody] TripMate_WebAPI.DTOs.Guide.Requests.SaveBlockedDatesRequest req)
        {
            var guideProfileId = await GetCurrentGuideProfileIdAsync();
            if (guideProfileId == null) return Unauthorized();
            
            await _calendarService.SaveBlockedDatesAsync(guideProfileId, req);
            return Ok(new { message = "Cập nhật thành công" });
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
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> Bookings()
        {
            var guideProfileId = await GetCurrentGuideProfileIdAsync();
            if (string.IsNullOrEmpty(guideProfileId)) return RedirectToAction("Dashboard");

            var bookings = await _bookingService.GetGuideBookingsAsync(guideProfileId);
            ViewBag.Bookings = bookings;
            return View();
        }

        // POST: /Guide/AcceptBooking/{id}
        [HttpPost("/Guide/AcceptBooking/{id}")]
        [Authorize(Roles = "guide")]
        public async Task<IActionResult> AcceptBooking(string id)
        {
            try
            {
                var guideProfileId = await GetCurrentGuideProfileIdAsync();
                if (string.IsNullOrEmpty(guideProfileId)) return Unauthorized();

                await _bookingService.UpdateGuideBookingStatusAsync(id, guideProfileId, 1); // 1 = Confirmed
                return Ok(new { success = true, message = "Đã chấp nhận booking thành công" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accepting booking {BookingId}", id);
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // POST: /Guide/RejectBooking/{id}
        [HttpPost("/Guide/RejectBooking/{id}")]
        [Authorize(Roles = "guide")]
        public async Task<IActionResult> RejectBooking(string id)
        {
            try
            {
                var guideProfileId = await GetCurrentGuideProfileIdAsync();
                if (string.IsNullOrEmpty(guideProfileId)) return Unauthorized();

                await _bookingService.UpdateGuideBookingStatusAsync(id, guideProfileId, 3); // 3 = Cancelled
                return Ok(new { success = true, message = "Đã từ chối booking" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting booking {BookingId}", id);
                return BadRequest(new { success = false, message = ex.Message });
            }
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

            // Also provide active bookings for this guide so chat threads can be created even without messages
            try
            {
                var guideProfileIdTask = GetCurrentGuideProfileIdAsync();
                guideProfileIdTask.Wait();
                var guideProfileId = guideProfileIdTask.Result;
                if (!string.IsNullOrEmpty(guideProfileId))
                {
                    var guideBookingsTask = _bookingService.GetGuideBookingsAsync(guideProfileId);
                    guideBookingsTask.Wait();
                    var guideBookings = guideBookingsTask.Result;

                    // Map to simple DTO to pass to view
                    var active = guideBookings.Select(b => new TripMate_WebAPI.DTOs.Chat.ActiveBookingDto
                    {
                        BookingId = b.Id ?? string.Empty,
                        TravelerId = b.TravelerId,
                        TravelerName = b.TravelerName,
                        TravelerAvatar = b.TravelerAvatar,
                        TourName = b.TourName,
                        BookingDate = b.Date
                    }).ToList();

                    ViewBag.ActiveBookings = active;
                }
                else
                {
                    ViewBag.ActiveBookings = new List<object>();
                }
            }
            catch
            {
                ViewBag.ActiveBookings = new List<object>();
            }

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


}
