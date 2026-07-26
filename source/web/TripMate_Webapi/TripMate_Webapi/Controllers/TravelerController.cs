using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using TripMate_Webapi.Repositories;
using TripMate_Webapi.Entities;
using TripMate_WebAPI.Services;

namespace TripMate_Webapi.Controllers
{
    /// <summary>
    /// MVC Controller cho tất cả Traveler pages.
    /// M1: Guard — mọi action cần auth đều kiểm tra JWT từ header/cookie trước khi xử lý.
    /// M4: Fix Booking state machine và tính tiền từ ExperiencePackage thực tế.
    /// </summary>
    public class TravelerController : Controller
    {
        private readonly ILogger<TravelerController> _logger;
        private readonly SupabaseAuthService _authService;
        private readonly ITripRequestRepository _tripRequestRepository;
        private readonly IBookingRepository _bookingRepository;
        private readonly TourService _tourService;
        private readonly IReviewRepository _reviewRepository;
        private readonly IGuideRepository _guideRepository;
        private readonly ISavedGuideRepository _savedGuideRepository;
        private readonly ITravelerBookingService _travelerBookingService;
        private readonly IReviewService _reviewService;
        private readonly IProfileService _profileService;
        private readonly BookingService _bookingService;
        private readonly Supabase.Client _supabase;

        private const string LOGIN_URL = "/Auth/Login";

        public TravelerController(
            ILogger<TravelerController> logger,
            SupabaseAuthService authService,
            ITripRequestRepository tripRequestRepository,
            IBookingRepository bookingRepository,
            TourService tourService,
            IReviewRepository reviewRepository,
            IGuideRepository guideRepository,
            ISavedGuideRepository savedGuideRepository,
            ITravelerBookingService travelerBookingService,
            IReviewService reviewService,
            IProfileService profileService,
            BookingService bookingService,
            Supabase.Client supabase)
        {
            _logger = logger;
            _authService = authService;
            _tripRequestRepository = tripRequestRepository;
            _bookingRepository = bookingRepository;
            _tourService = tourService;
            _reviewRepository = reviewRepository;
            _guideRepository = guideRepository;
            _savedGuideRepository = savedGuideRepository;
            _travelerBookingService = travelerBookingService;
            _reviewService = reviewService;
            _profileService = profileService;
            _bookingService = bookingService;
            _supabase = supabase;
        }

        // ────────────────────────────────────────────────────────────────────
        // M1 — Auth Helper: Đọc userId từ JWT claim được inject bởi middleware
        // Vì dự án dùng JWT trong localStorage (client-side), ta đọc từ Claims
        // sau khi JwtBearer middleware đã validate token từ Authorization header.
        // ────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Lấy travelerId từ JWT claims. Trả về null nếu chưa đăng nhập.
        /// </summary>
        private string? GetCurrentUserId()
        {
            // JWT "sub" claim = Supabase user UUID
            return User.FindFirst("sub")?.Value
                ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        }

        /// <summary>
        /// Kiểm tra user đã đăng nhập chưa (server-side, dựa vào JWT claim).
        /// </summary>
        private bool IsAuthenticated() => !string.IsNullOrEmpty(GetCurrentUserId());

        /// <summary>
        /// Redirect về Login page, lưu URL hiện tại vào localStorage thông qua response script.
        /// </summary>
        private IActionResult RedirectToLogin(string? returnUrl = null)
        {
            var url = returnUrl ?? Request.Path.ToString();
            return Redirect($"{LOGIN_URL}?returnUrl={Uri.EscapeDataString(url)}");
        }

        // ─────────────────────────────────────────────────────────────────────
        // PAGES
        // ─────────────────────────────────────────────────────────────────────

        // GET: /Traveler/Home (public)
        public IActionResult Home() => View();

        // GET: /Traveler/Dashboard [Auth required]
        public async Task<IActionResult> Dashboard()
        {
            // M1: Guard — không cho phép truy cập nếu chưa đăng nhập
            // Lưu ý: JWT được client gửi qua Authorization header ở AJAX calls.
            // Với MVC page navigation (browser), JWT nằm trong localStorage nên
            // server không nhận được Authorization header → ta check bằng JS redirect.
            // Tuy nhiên ta vẫn cần load data đúng khi user đã auth qua ajax/header.
            var travelerId = GetCurrentUserId();

            // Nếu server nhận được token (Authorization header) → dùng ngay
            // Nếu không (browser page navigation) → trả về view với ViewBag flag
            // để JS client tự check localStorage và redirect nếu cần
            ViewBag.RequiresAuth = true;

            var bookings = new List<BookingEntity>();

            if (!string.IsNullOrEmpty(travelerId))
            {
                bookings = await _bookingRepository.GetBookingsByTravelerAsync(travelerId);
                ViewBag.TravelerName = User.Identity?.Name
                    ?? User.FindFirst("email")?.Value
                    ?? "Traveler";
            }

            return View(bookings);
        }

        // GET: /Traveler/Trips [Auth required]
        public async Task<IActionResult> Trips()
        {
            ViewBag.RequiresAuth = true;
            var travelerId = GetCurrentUserId();

            var trips = new List<TripRequestEntity>();
            var bookings = new List<BookingEntity>();

            if (!string.IsNullOrEmpty(travelerId))
            {
                trips = await _tripRequestRepository.GetTripRequestsByTravelerAsync(travelerId);
                bookings = await _bookingRepository.GetBookingsByTravelerAsync(travelerId);
            }

            ViewBag.Trips = trips;
            return View(bookings);
        }

        // GET: /Traveler/BookingDetails/{id} [Auth required]
        public async Task<IActionResult> BookingDetails(string id)
        {
            ViewBag.RequiresAuth = true;
            var booking = await _bookingRepository.GetBookingByIdAsync(id);
            if (booking == null)
                return RedirectToAction("Dashboard");

            return View(booking);
        }





        // GET: /Traveler/Messages [Auth required]
        public async Task<IActionResult> Messages()
        {
            ViewBag.RequiresAuth = true;
            var travelerId = GetCurrentUserId();

            if (!string.IsNullOrEmpty(travelerId))
            {
                // Include all statuses so one permanent guide thread can be
                // activated only when at least one related booking is confirmed.
                var bookings = await _bookingRepository.GetBookingsByTravelerAsync(travelerId);
                var activeBookings = bookings.ToList();
                
                // Prepare active bookings info for client (include booking id + guide profile)
                var activeList = activeBookings
                    .Where(b => b.GuideProfile != null)
                    .Select(b => new TripMate_WebAPI.DTOs.Chat.ActiveBookingDto
                    {
                        BookingId = b.Id ?? string.Empty,
                        Status = b.Status,
                        GuideProfileId = b.GuideProfile?.Id,
                        GuideUserId = b.GuideProfile?.UserId,
                        GuideName = b.GuideProfile?.Profile?.FullName ?? b.GuideProfile?.UserId,
                        GuideAvatar = b.GuideProfile?.Profile?.AvatarUrl,
                        TourName = b.ExperiencePackage?.Title,
                        BookingDate = b.BookingDate.ToString("yyyy-MM-dd")
                    })
                    .ToList();

                ViewBag.ActiveBookings = activeList;
            }

            return View();
        }

        [Authorize(Roles = "traveler")]
        public IActionResult Notifications() => View();

        // GET: /Traveler/GuideProfile/{id} [Public]
        public async Task<IActionResult> GuideProfile(string id)
        {
            var guide = await _guideRepository.GetGuideByProfileIdAsync(id);
            if (guide == null) return NotFound();

            var packages = await _tourService.GetToursByGuideAsync(id);
            var reviews = await _reviewRepository.GetReviewsByGuideAsync(id);

            // Enrich reviews with traveler name & tour title
            var reviewDetails = new List<ReviewDetailDto>();
            foreach (var rev in reviews)
            {
                string travelerName = "Traveler";
                string tourTitle = "";

                try
                {
                    // Get traveler name
                    var travelerProfile = await _profileService.GetProfileAsync(rev.TravelerId);
                    if (travelerProfile != null && !string.IsNullOrEmpty(travelerProfile.FullName))
                        travelerName = travelerProfile.FullName;

                    // Get tour title from booking
                    if (!string.IsNullOrEmpty(rev.BookingId))
                    {
                        var booking = await _bookingRepository.GetBookingByIdAsync(rev.BookingId);
                        if (booking?.ExperiencePackage != null)
                            tourTitle = booking.ExperiencePackage.Title ?? "";
                    }
                }
                catch { /* non-critical enrichment */ }

                reviewDetails.Add(new ReviewDetailDto
                {
                    ReviewId = rev.Id,
                    TravelerName = travelerName,
                    TourTitle = tourTitle,
                    Rating = rev.Rating,
                    Comment = rev.Comment ?? "",
                    CreatedAt = rev.CreatedAt
                });
            }

            ViewBag.Packages = packages;
            ViewBag.Reviews = reviews;
            ViewBag.ReviewDetails = reviewDetails;

            return View(guide);
        }

        // GET: /Traveler/Saved [Auth required]
        public IActionResult Saved()
        {
            ViewBag.RequiresAuth = true;
            return View();
        }

        // GET: /Traveler/Settings [Auth required]
        public IActionResult Settings()
        {
            ViewBag.RequiresAuth = true;
            return View();
        }





        // ponytail ultra: minimal inline update
        public class UpdateTravelerProfileDto
        {
            public string? FullName { get; set; }
            public string? Phone { get; set; }
            public string? Location { get; set; }
            public string? AvatarUrl { get; set; }
            public string? Email { get; set; }
        }

        [HttpPost("Traveler/UpdateProfile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateTravelerProfileDto dto)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var result = await _profileService.UpdateTravelerProfileAsync(
                userId, dto.FullName, dto.Phone, dto.Location, dto.Email, null, dto.AvatarUrl);
            
            if (!result.Success) return BadRequest(new { error = result.Message });

            return Ok(new { success = true, avatarUrl = result.AvatarUrl });
        }

        // GET: /Traveler/Review/{id} [Auth required]
        public async Task<IActionResult> Review(string id)
        {
            ViewBag.RequiresAuth = true;

            // M5: Đọc booking thực tế từ DB thay vì hardcode
            var booking = await _bookingRepository.GetBookingByIdAsync(id);
            if (booking == null)
                return RedirectToAction("Trips");

            // Chỉ cho review khi Status = 2 (Completed)
            if (booking.Status != 2)
            {
                TempData["ErrorMessage"] = "You can only review trips that have been completed.";
                return RedirectToAction("Trips");
            }

            // Check if already reviewed
            var alreadyReviewed = await _reviewRepository.HasReviewForBookingAsync(id);
            if (alreadyReviewed)
            {
                TempData["ErrorMessage"] = "You have already reviewed this trip.";
                return RedirectToAction("Trips");
            }

            return View(booking);
        }

        // GET: /Traveler/CreateTripRequest [Auth required]
        public IActionResult CreateTripRequest()
        {
            ViewBag.RequiresAuth = true;
            return View();
        }

        // ─────────────────────────────────────────────────────────────────────
        // POST ACTIONS
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// POST: /Traveler/Book
        /// M1: Xóa fallback giả danh tính — chỉ chấp nhận user đã đăng nhập.
        /// M4: Tính TotalAmount từ ExperiencePackage thực tế, PlatformFee = 15%.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Book(string guideId, DateTime date, int guests, string? notes = null)
        {
            var travelerId = GetCurrentUserId();
            if (string.IsNullOrEmpty(travelerId))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để đặt lịch.";
                return Redirect($"{LOGIN_URL}?returnUrl=/Guide/Profile/{guideId}");
            }

            if (string.IsNullOrEmpty(guideId) || guideId == "00000000-0000-0000-0000-000000000000")
            {
                TempData["ErrorMessage"] = "Không tìm thấy Guide. Vui lòng thử lại.";
                return RedirectToAction("Dashboard");
            }

            var result = await _travelerBookingService.CreateCustomBookingAsync(travelerId, guideId, date, guests, notes);
            if (!result.Success)
            {
                return BadRequest(new { error = result.Message });
            }

            HttpContext.Session.Remove("GhostBooking");
            return Json(new { bookingId = result.BookingId, paymentUrl = result.PaymentUrl });
        }

        /// <summary>
        /// POST: /Traveler/BookTour
        /// Books a specific experience package (tour) — creates booking → redirects to Checkout.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> BookTour(
            string guideId,
            string packageId,
            DateTime date,
            int guests = 1,
            string? notes = null)
        {
            var travelerId = GetCurrentUserId();
            if (string.IsNullOrEmpty(travelerId))
                return Unauthorized(new { error = "Not authenticated" });

            if (string.IsNullOrEmpty(guideId) || string.IsNullOrEmpty(packageId))
                return BadRequest(new { error = "Missing guideId or packageId" });

            var result = await _travelerBookingService.CreateTourBookingAsync(
                travelerId,
                guideId,
                packageId,
                date,
                guests,
                notes);
            if (!result.Success)
            {
                return BadRequest(new { error = result.Message });
            }

            HttpContext.Session.Remove("GhostBooking");
            return Json(new { bookingId = result.BookingId, paymentUrl = result.PaymentUrl });
        }

        /// <summary>
        /// GET: /Traveler/PaymentCallback
        /// Handles redirect from PayOS after payment attempt.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> PaymentCallback(
            [FromQuery] string bookingId,
            [FromQuery] string? orderCode,
            [FromQuery] string? cancel = null)
        {
            try
            {
                if (string.IsNullOrEmpty(bookingId)) return RedirectToAction("Dashboard");

                var travelerId = GetCurrentUserId();
                if (string.IsNullOrWhiteSpace(travelerId))
                    return Redirect($"{LOGIN_URL}?returnUrl=/Traveler/Dashboard");

                if (string.Equals(cancel, "true", StringComparison.OrdinalIgnoreCase))
                {
                    var booking = await _bookingRepository.GetBookingByIdAsync(bookingId);
                    if (booking != null && booking.TravelerId == travelerId && booking.Status == -1)
                    {
                        await _bookingRepository.UpdateBookingStatusAsync(bookingId, 3); // 3 = Cancelled
                        TempData["ErrorMessage"] = "Payment Cancelled";
                        return RedirectToAction("Dashboard");
                    }
                }

                // PayOS redirect query values are not proof of payment. The
                // verified webhook is the only writer; this action only reads.
                var result = await _travelerBookingService.GetPaymentReturnStatusAsync(
                    travelerId,
                    bookingId,
                    orderCode,
                    cancel);
                
                if (result.Success)
                {
                    TempData["SuccessMessage"] = result.Message;
                }
                else
                {
                    TempData["ErrorMessage"] = result.Message;
                }
                
                return RedirectToAction("Dashboard");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Payment processing error");
                TempData["ErrorMessage"] = $"Payment processing error: {ex.Message} - Please take a screenshot and report to technical support.";
                return RedirectToAction("Dashboard");
            }
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetGuideAvailability(string guideId)
        {
            if (string.IsNullOrEmpty(guideId))
                return Json(new string[] { });

            try
            {
                var response = await _supabase.From<GuideAvailabilityEntity>()
                    .Where(x => x.GuideProfileId == guideId)
                    .Get();

                var blockedDates = response.Models.Select(x => x.UnavailableDate).Distinct().ToList();
                return Json(blockedDates);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching guide availability");
                return Json(new string[] { });
            }
        }

        /// <summary>
        /// M1, M4: Handles the first step of booking a tour (Request creation/Direct payment). link for a booking that is still in status -1 (Pending Payment).
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> RetryPayment([FromBody] RetryPaymentRequest req)
        {
            var travelerId = GetCurrentUserId();
            if (string.IsNullOrEmpty(travelerId))
                return Unauthorized(new { error = "Not authenticated" });

            var result = await _travelerBookingService.RetryPaymentAsync(travelerId, req.BookingId);
            
            if (result.Success)
            {
                return Json(new { success = true, paymentUrl = result.PaymentUrl });
            }
            else
            {
                if (result.Message == "Booking not found") return NotFound(new { error = result.Message });
                return BadRequest(new { error = result.Message });
            }
        }

        public class RetryPaymentRequest
        {
            public string BookingId { get; set; } = string.Empty;
        }
        
        /// <summary>
        /// POST: /Traveler/CreateTripRequest
        /// M1: Require auth. Fix: Đọc groupSize từ form.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateTripRequest(
            string destination, string dates, string budget, string notes,
            int groupSize = 1) // FIX: Thêm tham số groupSize từ form
        {
            var travelerId = GetCurrentUserId();
            if (string.IsNullOrEmpty(travelerId))
                return Redirect($"{LOGIN_URL}?returnUrl=/Traveler/CreateTripRequest");

            // Parse dates từ "YYYY-MM-DD to YYYY-MM-DD"
            DateTime startDate = DateTime.UtcNow;
            DateTime endDate = DateTime.UtcNow.AddDays(1);
            if (!string.IsNullOrEmpty(dates) && dates.Contains(" to "))
            {
                var parts = dates.Split(" to ");
                if (DateTime.TryParse(parts[0], out var start)) startDate = start;
                if (DateTime.TryParse(parts[1], out var end)) endDate = end;
            }
            else if (DateTime.TryParse(dates, out var singleDate))
            {
                startDate = singleDate;
                endDate = singleDate;
            }

            var tripRequest = new TripRequestEntity
            {
                Id = Guid.NewGuid().ToString(),
                TravelerId = travelerId,
                Destination = destination,
                StartDate = startDate.ToUniversalTime(),
                EndDate = endDate.ToUniversalTime(),
                GroupSize = groupSize, // FIX: Lưu đúng từ form
                Budget = budget ?? "",
                Notes = notes ?? "",
                Status = "open",
                CreatedAt = DateTime.UtcNow
            };

            await _tripRequestRepository.CreateTripRequestAsync(tripRequest);

            TempData["SuccessMessage"] = "Yêu cầu chuyến đi đã được đăng! Các hướng dẫn viên địa phương sẽ liên hệ với bạn sớm.";
            return RedirectToAction("Trips");
        }

        /// <summary>
        /// POST: /Traveler/SubmitReview
        /// M5: Lưu review vào bảng reviews trên Supabase.
        /// Validation: rating 1-5, comment >= 10 chars, duplicate check per booking.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SubmitReview(string id, int rating, string comment)
        {
            var travelerId = GetCurrentUserId();
            if (string.IsNullOrEmpty(travelerId))
                return Redirect($"{LOGIN_URL}?returnUrl=/Traveler/Review/{id}");

            var result = await _reviewService.SubmitReviewAsync(id, travelerId, rating, comment);
            
            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }

            if (!result.Success && result.Message.Contains("Vui lòng"))
            {
                return RedirectToAction("Review", new { id });
            }

            return RedirectToAction("Trips");
        }

        // POST: /Traveler/DeleteTrip/{id}
        [HttpPost]
        public async Task<IActionResult> DeleteTrip(string id)
        {
            var travelerId = GetCurrentUserId();
            if (string.IsNullOrEmpty(travelerId))
                return Redirect(LOGIN_URL);

            await _tripRequestRepository.DeleteTripRequestAsync(id);
            TempData["SuccessMessage"] = "Yêu cầu chuyến đi đã được xóa thành công.";
            return RedirectToAction("Trips");
        }

        // POST: /Traveler/ToggleTripStatus/{id}
        [HttpPost]
        public async Task<IActionResult> ToggleTripStatus(string id)
        {
            var travelerId = GetCurrentUserId();
            if (string.IsNullOrEmpty(travelerId))
                return Redirect(LOGIN_URL);

            await _tripRequestRepository.ToggleTripRequestStatusAsync(id);
            TempData["SuccessMessage"] = "Trạng thái chuyến đi đã được cập nhật.";
            return RedirectToAction("Trips");
        }

        // ─────────────────────────────────────────────────────────────────────
        // JSON API endpoints — called from client-side JS with Bearer token
        // ─────────────────────────────────────────────────────────────────────

        // GET: /Traveler/GetMyTrips  [Bearer Auth via header]
        [HttpGet]
        public async Task<IActionResult> GetMyTrips()
        {
            var travelerId = GetCurrentUserId();
            if (string.IsNullOrEmpty(travelerId))
                return Unauthorized(new { error = "Not authenticated" });

            var trips = await _tripRequestRepository.GetTripRequestsByTravelerAsync(travelerId);
            var result = trips.Select(t => new
            {
                id = t.Id,
                destination = t.Destination,
                startDate = t.StartDate.ToString("MMM dd, yyyy"),
                endDate = t.EndDate.ToString("MMM dd, yyyy"),
                groupSize = t.GroupSize,
                budget = t.Budget,
                notes = t.Notes,
                status = t.Status,
                createdAt = t.CreatedAt.ToString("MMM dd, yyyy HH:mm")
            });
            return Json(result);
        }

        [HttpGet("Traveler/GetProfileAjax")]
        public async Task<IActionResult> GetProfileAjax()
        {
            var travelerId = GetCurrentUserId();
            if (string.IsNullOrEmpty(travelerId))
                return Unauthorized(new { error = "Not authenticated" });

            try
            {
                var profile = await _profileService.GetProfileAsync(travelerId);
                if (profile == null) return NotFound(new { error = "Profile not found" });

                return Json(new {
                    displayName = profile.FullName,
                    email = profile.Email,
                    phone = profile.Phone,
                    nationality = profile.Location,
                    avatarUrl = profile.AvatarUrl
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching profile");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        public class UpdateProfileRequest
        {
            public string? DisplayName { get; set; }
            public string? Phone { get; set; }
            public string? Nationality { get; set; }
            public IFormFile? AvatarFile { get; set; }
        }

        [HttpPost("Traveler/UpdateProfileAjax")]
        public async Task<IActionResult> UpdateProfileAjax([FromForm] UpdateProfileRequest req)
        {
            var travelerId = GetCurrentUserId();
            if (string.IsNullOrEmpty(travelerId))
                return Unauthorized(new { error = "Not authenticated" });

            var result = await _profileService.UpdateTravelerProfileAsync(
                travelerId, req.DisplayName, req.Phone, req.Nationality, null, req.AvatarFile, null);
            
            if (result.Success)
            {
                return Json(new { success = true, avatarUrl = result.AvatarUrl });

            }
            
            if (result.Message == "Profile not found")
            {
                return NotFound(new { error = result.Message });
            }

            return StatusCode(500, new { error = result.Message });
        }

        // GET: /Traveler/GetMyBookings  [Bearer Auth via header]
        [HttpGet]
        public async Task<IActionResult> GetMyBookings()
        {
            var travelerId = GetCurrentUserId();
            if (string.IsNullOrEmpty(travelerId))
                return Unauthorized(new { error = "Not authenticated" });

            var bookings = await _bookingRepository.GetBookingsByTravelerAsync(travelerId);
            
            var resultList = new List<object>();
            foreach(var b in bookings)
            {
                bool hasReviewed = false;
                if (b.Status == 2) 
                {
                    hasReviewed = await _reviewRepository.HasReviewForBookingAsync(b.Id);
                }

                bool isPastDeadline = false;
                bool isApproachingDeadline = false;
                if (b.Status == 1 && b.AmountPaid < b.TotalAmount)
                {
                    // 72h deadline before StartTime for 2nd payment
                    // b.StartTime contains time, b.BookingDate contains date
                    var actualStartTime = b.BookingDate.Date.Add(b.StartTime.TimeOfDay);
                    var deadline = actualStartTime.AddHours(-72);
                    if (DateTime.UtcNow > deadline)
                    {
                        isPastDeadline = true;
                    }
                    else if (DateTime.UtcNow > deadline.AddHours(-24))
                    {
                        isApproachingDeadline = true;
                    }
                }

                resultList.Add(new
                {
                    id = b.Id,
                    status = b.Status,
                    bookingDate = b.BookingDate.ToString("MMM dd, yyyy"),
                    totalAmount = b.TotalAmount,
                    amountPaid = b.AmountPaid,
                    isPastDeadline = isPastDeadline,
                    isApproachingDeadline = isApproachingDeadline,
                    notes = b.TravelerNotes,
                    guideName = b.GuideProfile?.Profile?.FullName ?? "Local Guide",
                    guideAvatar = b.GuideProfile?.Profile?.AvatarUrl ?? "",
                    guideCoverPhoto = b.GuideProfile?.CoverPhotoUrl ?? "",
                    guideProfileId = b.GuideProfileId,
                    packageTitle = b.ExperiencePackage?.Title ?? "Custom Tour",
                    guestCount = b.GuestCount,
                    paymentReference = b.PaymentReference ?? "",
                    hasReviewed = hasReviewed
                });
            }

            return Json(resultList);
        }

        public class SubmitReviewRequest
        {
            public string BookingId { get; set; } = string.Empty;
            public int Rating { get; set; }
            public string Comment { get; set; } = string.Empty;
        }

        [HttpPost("Traveler/SubmitReviewAjax")]
        public async Task<IActionResult> SubmitReviewAjax([FromBody] SubmitReviewRequest req)
        {
            var travelerId = GetCurrentUserId();
            if (string.IsNullOrEmpty(travelerId))
                return Unauthorized(new { error = "Not authenticated" });

            try
            {
                await _reviewService.SubmitReviewAsync(req.BookingId, travelerId, req.Rating, req.Comment);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // POST: /Traveler/DeleteTripAjax/{id}  [Bearer Auth via header]
        [HttpPost]
        public async Task<IActionResult> DeleteTripAjax(string id)
        {
            var travelerId = GetCurrentUserId();
            if (string.IsNullOrEmpty(travelerId))
                return Unauthorized(new { error = "Not authenticated" });

            await _tripRequestRepository.DeleteTripRequestAsync(id);
            return Json(new { success = true });
        }

        // POST: /Traveler/CancelBookingAjax/{id}  [Bearer Auth via header]
        [HttpPost]
        public async Task<IActionResult> CancelBookingAjax(string id)
        {
            var travelerId = GetCurrentUserId();
            if (string.IsNullOrEmpty(travelerId))
                return Unauthorized(new { error = "Not authenticated" });

            try
            {
                await _bookingService.CancelBookingAsync(id, travelerId);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // POST: /Traveler/ToggleTripStatusAjax/{id}  [Bearer Auth via header]
        [HttpPost]
        public async Task<IActionResult> ToggleTripStatusAjax(string id)
        {
            var travelerId = GetCurrentUserId();
            if (string.IsNullOrEmpty(travelerId))
                return Unauthorized(new { error = "Not authenticated" });

            await _tripRequestRepository.ToggleTripRequestStatusAsync(id);
            return Json(new { success = true });
        }

        // GET: /Traveler/GetTripOffersAjax/{requestId}
        [HttpGet("Traveler/GetTripOffersAjax/{requestId}")]
        public async Task<IActionResult> GetTripOffersAjax(string requestId)
        {
            var travelerId = GetCurrentUserId();
            if (string.IsNullOrEmpty(travelerId))
                return Unauthorized(new { error = "Not authenticated" });

            try
            {
                var request = await _tripRequestRepository.GetTripRequestByIdAsync(requestId);
                if (request == null || request.TravelerId != travelerId)
                    return NotFound(new { error = "Trip request not found or unauthorized" });

                var offers = await _tripRequestRepository.GetTripOffersByRequestIdAsync(requestId);
                
                // Get unique guide profile ids to enrich offers
                var guideProfileIds = offers.Select(o => o.GuideProfileId).Distinct().ToList();
                var profilesMap = new Dictionary<string, object>();
                
                foreach(var gId in guideProfileIds)
                {
                    var guideProfile = await _guideRepository.GetGuideByProfileIdAsync(gId);
                    if (guideProfile != null)
                    {
                        profilesMap[gId] = new {
                            name = guideProfile.Profile?.FullName ?? "Local Guide",
                            avatarUrl = guideProfile.Profile?.AvatarUrl ?? "",
                            rating = guideProfile.AverageRating
                        };
                    }
                }

                var result = offers.Select(o => new {
                    id = o.Id,
                    guideProfileId = o.GuideProfileId,
                    message = o.Message,
                    proposedPrice = o.ProposedPrice,
                    status = o.Status,
                    createdAt = o.CreatedAt.ToString("MMM dd, yyyy HH:mm"),
                    guide = profilesMap.ContainsKey(o.GuideProfileId) ? profilesMap[o.GuideProfileId] : null
                });

                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching offers for request {RequestId}", requestId);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // POST: /Traveler/AcceptOfferAjax/{offerId}
        [HttpPost("Traveler/AcceptOfferAjax/{offerId}")]
        public async Task<IActionResult> AcceptOfferAjax(string offerId)
        {
            var travelerId = GetCurrentUserId();
            if (string.IsNullOrEmpty(travelerId))
                return Unauthorized(new { error = "Not authenticated" });

            try
            {
                var offer = await _tripRequestRepository.GetTripOfferByIdAsync(offerId);
                if (offer == null) return NotFound(new { error = "Offer not found" });

                var request = await _tripRequestRepository.GetTripRequestByIdAsync(offer.TripRequestId);
                if (request == null || request.TravelerId != travelerId)
                    return Unauthorized(new { error = "Unauthorized" });

                if (request.Status != "open")
                    return BadRequest(new { error = "This request is no longer open." });

                // Update all offers for this request
                var allOffers = await _tripRequestRepository.GetTripOffersByRequestIdAsync(request.Id);
                foreach(var o in allOffers)
                {
                    o.Status = (o.Id == offerId) ? "accepted" : "rejected";
                    await _tripRequestRepository.UpdateTripOfferAsync(o);
                }

                // Close request
                request.Status = "closed";
                await _tripRequestRepository.UpdateTripRequestAsync(request);

                // Generate booking & payment link
                var result = await _travelerBookingService.CreateBookingFromOfferAsync(travelerId, request, offer);

                if (!result.Success)
                    return BadRequest(new { error = result.Message });

                return Json(new { success = true, paymentUrl = result.PaymentUrl, bookingId = result.BookingId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accepting offer {OfferId}", offerId);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // POST: /Traveler/RejectOfferAjax/{offerId}
        [HttpPost("Traveler/RejectOfferAjax/{offerId}")]
        public async Task<IActionResult> RejectOfferAjax(string offerId)
        {
            var travelerId = GetCurrentUserId();
            if (string.IsNullOrEmpty(travelerId))
                return Unauthorized(new { error = "Not authenticated" });

            try
            {
                var offer = await _tripRequestRepository.GetTripOfferByIdAsync(offerId);
                if (offer == null) return NotFound(new { error = "Offer not found" });

                var request = await _tripRequestRepository.GetTripRequestByIdAsync(offer.TripRequestId);
                if (request == null || request.TravelerId != travelerId)
                    return Unauthorized(new { error = "Unauthorized" });

                offer.Status = "rejected";
                await _tripRequestRepository.UpdateTripOfferAsync(offer);

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting offer {OfferId}", offerId);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // POST: /Traveler/DeleteOfferAjax/{offerId}
        [HttpPost("Traveler/DeleteOfferAjax/{offerId}")]
        public async Task<IActionResult> DeleteOfferAjax(string offerId)
        {
            var travelerId = GetCurrentUserId();
            if (string.IsNullOrEmpty(travelerId))
                return Unauthorized(new { error = "Not authenticated" });

            try
            {
                var offer = await _tripRequestRepository.GetTripOfferByIdAsync(offerId);
                if (offer == null) return NotFound(new { error = "Offer not found" });

                var request = await _tripRequestRepository.GetTripRequestByIdAsync(offer.TripRequestId);
                if (request == null || request.TravelerId != travelerId)
                    return Unauthorized(new { error = "Unauthorized" });

                if (offer.Status != "rejected")
                    return BadRequest(new { error = "Only rejected offers can be deleted." });

                await _tripRequestRepository.DeleteTripOfferAsync(offerId);

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting offer {OfferId}", offerId);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // POST: /Traveler/DeleteBookingAjax/{id}
        [HttpDelete]
        public async Task<IActionResult> DeleteBookingAjax(string id)
        {
            var travelerId = GetCurrentUserId();
            if (string.IsNullOrEmpty(travelerId))
                return Unauthorized(new { error = "Not authenticated" });

            try
            {
                var booking = await _bookingRepository.GetBookingByIdAsync(id);
                if (booking == null) return NotFound(new { error = "Booking not found" });

                if (booking.TravelerId != travelerId)
                    return Unauthorized(new { error = "Unauthorized" });

                if (booking.Status != 3) // 3 = Cancelled
                    return BadRequest(new { error = "Only cancelled bookings can be deleted." });

                await _bookingRepository.DeleteBookingAsync(id);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting booking {BookingId}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CompleteTourAjax(string id)
        {
            var travelerId = GetCurrentUserId();
            if (string.IsNullOrEmpty(travelerId))
                return Unauthorized(new { error = "Not authenticated" });

            try
            {
                var booking = await _bookingRepository.GetBookingByIdAsync(id);
                if (booking == null) return NotFound(new { error = "Booking not found" });

                if (booking.TravelerId != travelerId)
                    return Unauthorized(new { error = "Unauthorized" });

                if (booking.Status != 1) // Only confirmed tours can be completed
                    return BadRequest(new { error = "Only confirmed bookings can be completed." });

                // Set status to Completed (2)
                await _bookingRepository.UpdateBookingStatusAsync(id, 2);
                
                // M4: (Optional) Initiate payout logic via admin panel or automatic here.
                
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing tour {BookingId}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        public class ReportGuideRequest
        {
            public string BookingId { get; set; }
            public string Reason { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> ReportGuideAjax([FromBody] ReportGuideRequest req)
        {
            var travelerId = GetCurrentUserId();
            if (string.IsNullOrEmpty(travelerId))
                return Unauthorized(new { error = "Not authenticated" });

            try
            {
                var booking = await _bookingRepository.GetBookingByIdAsync(req.BookingId);
                if (booking == null || booking.TravelerId != travelerId)
                    return NotFound(new { error = "Booking not found" });

                // TODO: Save to reports table in database.
                // For now, we will log it.
                _logger.LogWarning("Guide reported for Booking {BookingId} by Traveler {TravelerId}. Reason: {Reason}", req.BookingId, travelerId, req.Reason);

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reporting guide for booking {BookingId}", req.BookingId);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        public class SubmitFeedbackRequest
        {
            public int Rating { get; set; }
            public string Comment { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> SubmitFeedbackAjax([FromBody] SubmitFeedbackRequest req)
        {
            var travelerId = GetCurrentUserId();
            if (string.IsNullOrEmpty(travelerId))
                return Unauthorized(new { error = "Not authenticated" });

            try
            {
                // TODO: Save to feedback/reports table in database.
                // For now, log it.
                _logger.LogInformation("System feedback submitted by Traveler {TravelerId}. Rating: {Rating}, Comment: {Comment}", travelerId, req.Rating, req.Comment);

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting feedback for Traveler {TravelerId}", travelerId);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // GET: /Traveler/GetSavedGuidesAjax  [Bearer Auth via header]
        [HttpGet]
        public async Task<IActionResult> GetSavedGuidesAjax()
        {
            var travelerId = GetCurrentUserId();
            if (string.IsNullOrEmpty(travelerId))
                return Unauthorized(new { error = "Not authenticated" });

            try
            {
                var savedGuides = await _savedGuideRepository.GetSavedGuidesByTravelerAsync(travelerId);
                var result = new List<object>();

                foreach (var sg in savedGuides)
                {
                    var guideProfile = await _guideRepository.GetGuideByProfileIdAsync(sg.GuideProfileId);
                    if (guideProfile != null)
                    {
                        result.Add(new
                        {
                            guideId = guideProfile.Id,
                            userId = guideProfile.UserId,
                            name = guideProfile.Profile?.FullName ?? "Unknown Guide",
                            avatarUrl = guideProfile.Profile?.AvatarUrl ?? "",
                            coverPhotoUrl = guideProfile.CoverPhotoUrl ?? "",
                            cityArea = guideProfile.CityArea ?? "Local Area",
                            averageRating = guideProfile.AverageRating,
                            totalReviews = guideProfile.TotalReviews,
                            price = guideProfile.PricePerHour
                        });
                    }
                }
                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching saved guides for user {UserId}", travelerId);
                return StatusCode(500, new { error = "Internal server error while fetching saved guides." });
            }
        }

        // POST: /Traveler/ToggleSaveGuideAjax/{guideProfileId}  [Bearer Auth via header]
        [HttpPost("Traveler/ToggleSaveGuideAjax/{guideProfileId}")]
        public async Task<IActionResult> ToggleSaveGuideAjax(string guideProfileId)
        {
            var travelerId = GetCurrentUserId();
            if (string.IsNullOrEmpty(travelerId))
                return Unauthorized(new { error = "Not authenticated" });

            try
            {
                var isSaved = await _savedGuideRepository.IsGuideSavedAsync(travelerId, guideProfileId);
                if (isSaved)
                {
                    await _savedGuideRepository.DeleteSavedGuideAsync(travelerId, guideProfileId);
                    return Json(new { success = true, saved = false });
                }
                else
                {
                    await _savedGuideRepository.SaveGuideAsync(new SavedGuideEntity
                    {
                        TravelerId = travelerId,
                        GuideProfileId = guideProfileId
                    });
                    return Json(new { success = true, saved = true });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling saved guide for user {UserId} and guide {GuideId}", travelerId, guideProfileId);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // GET: /Traveler/IsGuideSavedAjax/{guideProfileId}
        [HttpGet("Traveler/IsGuideSavedAjax/{guideProfileId}")]
        public async Task<IActionResult> IsGuideSavedAjax(string guideProfileId)
        {
            var travelerId = GetCurrentUserId();
            if (string.IsNullOrEmpty(travelerId))
                return Ok(new { saved = false });

            try
            {
                var isSaved = await _savedGuideRepository.IsGuideSavedAsync(travelerId, guideProfileId);
                return Ok(new { saved = isSaved });
            }
            catch
            {
                return Ok(new { saved = false });
            }
        }

        // GET: /Traveler/Notifications
        [HttpGet]
        public async Task<IActionResult> Notifications([FromServices] TripMate_Webapi.Repositories.INotificationRepository _notificationRepo)
        {
            var travelerId = GetCurrentUserId();
            if (string.IsNullOrEmpty(travelerId))
            {
                return RedirectToAction("LoginView", "Auth");
            }

            var notifications = await _notificationRepo.GetNotificationsByUserIdAsync(travelerId, 50); // Fetch latest 50
            return View(notifications);
        }
    }

    /// <summary>
    /// DTO for displaying enriched review info on the Guide Profile page
    /// </summary>
    public class ReviewDetailDto
    {
        public string ReviewId { get; set; } = string.Empty;
        public string TravelerName { get; set; } = "Traveler";
        public string TourTitle { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
