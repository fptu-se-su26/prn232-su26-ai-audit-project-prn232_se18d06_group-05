using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using TripMate_WebAPI.Services;
using TripMate_Webapi.Entities;
using TripMate_WebAPI.DTOs.Tour.Requests;
using TripMate_Webapi.Repositories;
using TripMate_WebAPI.DTOs;
using TripMate_Webapi.Services;

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
        [Authorize(Roles = "guide")]
        public IActionResult TripRequests()
        {
            return View();
        }

        [HttpGet]
        [Authorize(Roles = "guide")]
        public async Task<IActionResult> GetTripRequestsData([FromServices] ITripRequestService tripRequestService)
        {
            var guideProfileId = await GetCurrentGuideProfileIdAsync();
            if (string.IsNullOrEmpty(guideProfileId)) return Unauthorized();

            var requests = await tripRequestService.GetOpenRequestsAsync(guideProfileId);
            return Json(requests);
        }

        [HttpGet]
        [Authorize(Roles = "guide")]
        public async Task<IActionResult> GetMyOffersData([FromServices] ITripRequestService tripRequestService)
        {
            var guideProfileId = await GetCurrentGuideProfileIdAsync();
            if (string.IsNullOrEmpty(guideProfileId)) return Unauthorized();

            var offers = await tripRequestService.GetGuideOffersAsync(guideProfileId);
            return Json(offers);
        }

        [HttpPost]
        [Authorize(Roles = "guide")]
        public async Task<IActionResult> SendTripOffer([FromBody] TripMate_Webapi.DTOs.Guide.SendTripOfferRequest dto, [FromServices] ITripRequestService tripRequestService)
        {
            var traceId = HttpContext.TraceIdentifier;
            var guideProfileId = await GetCurrentGuideProfileIdAsync();
            if (string.IsNullOrEmpty(guideProfileId))
            {
                _logger.LogWarning("Trip offer rejected because no guide profile was found. TraceId: {TraceId}", traceId);
                return Unauthorized(new { success = false, message = "Unable to identify the guide profile.", traceId });
            }

            _logger.LogInformation(
                "Submitting trip offer. GuideProfileId: {GuideProfileId}, TripRequestId: {TripRequestId}, TraceId: {TraceId}",
                guideProfileId,
                dto.TripRequestId,
                traceId);

            var result = await tripRequestService.SendOfferAsync(guideProfileId, dto);
            if (result.Success)
            {
                _logger.LogInformation("Trip offer persisted. OfferId: {OfferId}, TraceId: {TraceId}", result.OfferId, traceId);
                return Json(new { success = true, offerId = result.OfferId });
            }

            _logger.LogWarning(
                "Trip offer rejected. GuideProfileId: {GuideProfileId}, TripRequestId: {TripRequestId}, Reason: {Reason}, TraceId: {TraceId}",
                guideProfileId,
                dto.TripRequestId,
                result.ErrorMessage,
                traceId);

            if (result.ErrorMessage?.Contains("already sent an offer", StringComparison.OrdinalIgnoreCase) == true)
            {
                return Conflict(new { success = false, message = result.ErrorMessage, traceId });
            }

            return BadRequest(new { success = false, message = result.ErrorMessage ?? "Unable to send the offer.", traceId });
        }

        [HttpGet]
        [Authorize(Roles = "guide")]
        public async Task<IActionResult> GetOfferStats([FromServices] ITripRequestService tripRequestService)
        {
            var guideProfileId = await GetCurrentGuideProfileIdAsync();
            if (string.IsNullOrEmpty(guideProfileId)) return Unauthorized();

            var stats = await tripRequestService.GetGuideOfferStatsAsync(guideProfileId);
            return Json(stats);
        }

        // GET: /Guide/Dashboard
        [HttpGet("/Guide/Dashboard")]
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
        [HttpGet("/Guide/Profile/{id}")]
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

        [HttpPost("/Guide/UpdateProfile")]
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

            try
            {
                var data = await _calendarService.GetCalendarDataAsync(guideProfileId, start, end);
                return Json(data);
            }
            catch (ArgumentException exception)
            {
                return BadRequest(new { message = exception.Message });
            }
        }

        // POST: /Guide/SaveBlockedDates
        [HttpPost, Authorize(Roles = "guide")]
        public async Task<IActionResult> SaveBlockedDates([FromBody] TripMate_WebAPI.DTOs.Guide.Requests.SaveBlockedDatesRequest req)
        {
            var guideProfileId = await GetCurrentGuideProfileIdAsync();
            if (guideProfileId == null) return Unauthorized();
            
            try
            {
                var result = await _calendarService.SaveBlockedDatesAsync(guideProfileId, req);
                return Ok(result);
            }
            catch (ArgumentException exception)
            {
                return BadRequest(new { message = exception.Message });
            }
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
                if (string.IsNullOrEmpty(guideProfileId)) return Unauthorized(new { success = false, message = "You are not authorized to perform this action." });

                var success = await _experienceService.ToggleTourStatusAsync(id, guideProfileId);
                
                if (success) return Ok(new { success = true, message = "Tour status updated successfully." });
                return BadRequest(new { success = false, message = "Experience package not found." });
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
                if (string.IsNullOrEmpty(guideProfileId)) return Unauthorized(new { success = false, message = "You are not authorized to perform this action." });

                var success = await _experienceService.DeleteTourAsync(id, guideProfileId);
                
                if (success) return Ok(new { success = true, message = "Experience package deleted." });
                return BadRequest(new { success = false, message = "Delete failed." });
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
                if (string.IsNullOrEmpty(guideProfileId)) return Unauthorized(new { success = false, message = "You are not authorized to perform this action." });

                var newTour = await _experienceService.DuplicateTourAsync(id, guideProfileId);
                
                if (newTour != null) return Ok(new { success = true, message = "Tour duplicated successfully." });
                return BadRequest(new { success = false, message = "Experience package not found." });
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
                    return BadRequest(new { success = false, message = "Invalid data." });
                }

                var guideProfileId = await GetCurrentGuideProfileIdAsync();
                if (string.IsNullOrEmpty(guideProfileId)) return Unauthorized(new { success = false, message = "You need a guide profile to create an experience package." });

                var createdTour = await _experienceService.CreateTourAsync(dto, guideProfileId);

                return Ok(new { success = true, data = new { id = createdTour.Id }, message = "Your experience package has been published." });
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
                return Ok(new { success = true, message = "Booking accepted successfully." });
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
                return Ok(new { success = true, message = "Booking rejected." });
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
            // Previously this action returned hard-coded mock threads for the UI.
            // Those mocks were replaced by live data fetched on the client, so
            // keeping them caused duplication and dead code. Removed mock threads
            // to let the client rely solely on the live conversations + active bookings.

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
        [Authorize(Roles = "guide")]
        public IActionResult Notifications()
        {
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
