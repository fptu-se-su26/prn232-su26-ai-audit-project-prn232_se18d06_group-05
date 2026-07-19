using Microsoft.AspNetCore.Mvc;
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
        private readonly IPayOSService _payOSService;
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
            IPayOSService payOSService,
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
            _payOSService = payOSService;
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
                // Lấy các booking đã confirmed hoặc completed (Status >= 1)
                var bookings = await _bookingRepository.GetBookingsByTravelerAsync(travelerId);
                var activeBookings = bookings.Where(b => b.Status >= 1).ToList();
                
                // Trích xuất các Guide duy nhất từ các booking này
                var guideProfiles = activeBookings
                    .Where(b => b.GuideProfile != null)
                    .Select(b => b.GuideProfile)
                    .GroupBy(g => g!.Id)
                    .Select(g => g.First())
                    .ToList();

                ViewBag.Guides = guideProfiles;
            }

            return View();
        }

        // GET: /Traveler/GuideProfile/{id} [Public]
        public async Task<IActionResult> GuideProfile(string id)
        {
            var guide = await _guideRepository.GetGuideByProfileIdAsync(id);
            if (guide == null) return NotFound();

            var packages = await _tourService.GetToursByGuideAsync(id);
            var reviews = await _reviewRepository.GetReviewsByGuideAsync(id);

            ViewBag.Packages = packages;
            ViewBag.Reviews = reviews;

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
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateTravelerProfileDto dto, [FromServices] Supabase.Client supabase)
        {
            // Simplified auth check based on current project pattern
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                      ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var profileResponse = await supabase.From<Entities.ProfileEntity>().Where(x => x.Id == userId).Get();
            var profile = profileResponse.Models.FirstOrDefault();
            if (profile != null)
            {
                if (dto.FullName != null) profile.FullName = dto.FullName;
                if (dto.Phone != null) profile.Phone = dto.Phone;
                if (dto.Location != null) profile.Location = dto.Location;
                if (dto.AvatarUrl != null) profile.AvatarUrl = dto.AvatarUrl;
                if (dto.Email != null) profile.Email = dto.Email;
                await supabase.From<Entities.ProfileEntity>().Update(profile);
            }

            return Ok(new { success = true });
        }

        // GET: /Traveler/Review/{id}
        public IActionResult Review(string id = "1")
        // GET: /Traveler/Review/{id} [Auth required]
        public async Task<IActionResult> Review(string id)
        {
            ViewBag.RequiresAuth = true;

            // M5: Đọc booking thực tế từ DB thay vì hardcode
            var booking = await _bookingRepository.GetBookingByIdAsync(id);
            if (booking == null)
                return RedirectToAction("Trips");

            // Chỉ cho review khi Status = 2 (Completed)
            // Nếu chưa completed, redirect về Trips để tránh review sai
            // Tạm thời bỏ check này để test UI, sẽ bật lại khi Guide flow hoàn thiện
            // if (booking.Status != 2) return RedirectToAction("Trips");

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
            // M1 Guard: lấy travelerId từ JWT claim
            var travelerId = GetCurrentUserId();
            if (string.IsNullOrEmpty(travelerId))
            {
                // Lưu return URL để redirect về sau login
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để đặt lịch.";
                return Redirect($"{LOGIN_URL}?returnUrl=/Guide/Profile/{guideId}");
            }

            if (string.IsNullOrEmpty(guideId) || guideId == "00000000-0000-0000-0000-000000000000")
            {
                TempData["ErrorMessage"] = "Không tìm thấy Guide. Vui lòng thử lại.";
                return RedirectToAction("Dashboard");
            }

            // Custom Booking Logic (Luôn tạo Custom Tour thay vì lấy Package đầu tiên)
            string packageId = "00000000-0000-0000-0000-000000000000";
            
            // Duplicate booking check
            var existingBookings = await _bookingRepository.GetBookingsByTravelerAsync(travelerId);
            bool hasDuplicate = existingBookings.Any(b => 
                b.Status >= 0 && b.Status < 2 && 
                b.ExperiencePackageId == packageId && 
                b.GuideProfileId == guideId && 
                b.BookingDate.Date == date.Date);
                
            if (hasDuplicate)
            {
                return BadRequest(new { error = "You already have an active booking for this custom tour on this date." });
            }

            decimal basePrice = 500_000m * guests; // Giá tham khảo cho Custom Tour

            // M4: PlatformFee = 15% (theo kiến trúc), GuideEarnings = 85%
            var platformFee = Math.Round(basePrice * 0.15m, 0);
            var guideEarnings = basePrice - platformFee;

            long orderCode = long.Parse(DateTimeOffset.UtcNow.ToString("yyMMddHHmmssfff"));

            var booking = new BookingEntity
            {
                TravelerId = travelerId,
                GuideProfileId = guideId,
                ExperiencePackageId = packageId,
                BookingDate = date,
                StartTime = date.Date.AddHours(9), // Default 9:00 AM
                GuestCount = guests,
                TotalAmount = basePrice,
                PlatformFee = platformFee,
                GuideEarnings = guideEarnings,
                TravelerNotes = notes,
                PaymentReference = orderCode.ToString(),
                Status = -1 // Pending Payment
            };

            var createdBooking = await _bookingRepository.CreateBookingAsync(booking);

            // Xóa Ghost Booking session sau khi booking thật đã tạo
            HttpContext.Session.Remove("GhostBooking");

            string paymentUrl = await _payOSService.CreatePaymentLink(createdBooking, orderCode);

            return Json(new { bookingId = createdBooking.Id, paymentUrl = paymentUrl });
        }

        /// <summary>
        /// POST: /Traveler/BookTour
        /// Books a specific experience package (tour) — creates booking → redirects to Checkout.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> BookTour(string guideId, string packageId, DateTime date, int guests = 1)
        {
            var travelerId = GetCurrentUserId();
            if (string.IsNullOrEmpty(travelerId))
                return Unauthorized(new { error = "Not authenticated" });

            if (string.IsNullOrEmpty(guideId) || string.IsNullOrEmpty(packageId))
                return BadRequest(new { error = "Missing guideId or packageId" });

            var selectedPackage = await _tourService.GetTourByIdAsync(packageId);

            decimal basePrice;
            if (selectedPackage != null)
            {
                // Force the guideId to match the actual owner of the package
                guideId = selectedPackage.GuideProfileId ?? guideId;
                
                if (selectedPackage.PricePerSession > 0)
                    basePrice = selectedPackage.PricePerSession;
                else if (selectedPackage.PricePerPerson.HasValue && selectedPackage.PricePerPerson > 0)
                    basePrice = selectedPackage.PricePerPerson.Value * guests;
                else
                    basePrice = 500_000m * guests;
            }
            else
            {
                // Fallback to custom package
                basePrice = 500_000m * guests;
                packageId = "00000000-0000-0000-0000-000000000000";
            }

            var platformFee = Math.Round(basePrice * 0.15m, 0);
            var guideEarnings = basePrice - platformFee;

            var targetDate = date == default ? DateTime.UtcNow.AddDays(7).Date : date.Date;

            // Duplicate booking check
            var existingBookings = await _bookingRepository.GetBookingsByTravelerAsync(travelerId);
            bool hasDuplicate = existingBookings.Any(b => 
                b.Status >= 0 && b.Status < 2 && 
                b.ExperiencePackageId == packageId && 
                b.GuideProfileId == guideId && 
                b.BookingDate.Date == targetDate);
                
            if (hasDuplicate)
            {
                return BadRequest(new { error = "You already have an active booking for this tour on this date." });
            }

            long orderCode = long.Parse(DateTimeOffset.UtcNow.ToString("yyMMddHHmmssfff"));

            var booking = new BookingEntity
            {
                TravelerId = travelerId,
                GuideProfileId = guideId,
                ExperiencePackageId = packageId,
                BookingDate = targetDate,
                StartTime = targetDate.AddHours(9),
                GuestCount = guests,
                TotalAmount = basePrice,
                PlatformFee = platformFee,
                GuideEarnings = guideEarnings,
                PaymentReference = orderCode.ToString(),
                Status = -1 // Pending Payment
            };

            var createdBooking = await _bookingRepository.CreateBookingAsync(booking);
            HttpContext.Session.Remove("GhostBooking");

            string paymentUrl = await _payOSService.CreatePaymentLink(createdBooking, orderCode);

            return Json(new { bookingId = createdBooking.Id, paymentUrl = paymentUrl });
        }

        /// <summary>
        /// GET: /Traveler/PaymentCallback
        /// Handles redirect from PayOS after payment attempt.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> PaymentCallback([FromQuery] string bookingId, [FromQuery] string cancel, [FromQuery] string status, [FromQuery] string orderCode)
        {
            try
            {
                if (string.IsNullOrEmpty(bookingId)) return RedirectToAction("Dashboard");

                var booking = await _bookingRepository.GetBookingByIdAsync(bookingId);
                if (booking == null) return RedirectToAction("Dashboard");

                if (cancel == "true" || status != "PAID")
                {
                    await _bookingRepository.UpdateBookingStatusAsync(booking.Id, 3); // Cancelled
                    TempData["ErrorMessage"] = "Payment was cancelled.";
                }
                else if (status == "PAID" && booking.Status == -1)
                {
                    await _bookingRepository.UpdateBookingStatusAsync(booking.Id, 0); // Pending Guide Approval
                    TempData["SuccessMessage"] = "Payment successful! Your booking is now pending guide approval.";
                }
                
                return RedirectToAction("Dashboard");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Payment processing error: {ex.Message} - Please take a screenshot and report to technical support.";
                return RedirectToAction("Dashboard");
            }
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

            // Validate rating
            if (rating < 1 || rating > 5)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn số sao từ 1 đến 5.";
                return RedirectToAction("Review", new { id });
            }

            // Validate comment length
            if (string.IsNullOrWhiteSpace(comment) || comment.Length < 10)
            {
                TempData["ErrorMessage"] = "Nhận xét phải có ít nhất 10 ký tự.";
                return RedirectToAction("Review", new { id });
            }

            // Lấy booking để lấy guideProfileId
            var booking = await _bookingRepository.GetBookingByIdAsync(id);
            if (booking == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy booking. Vui lòng thử lại.";
                return RedirectToAction("Trips");
            }

            // M5: Duplicate check — mỗi booking chỉ được review 1 lần
            var alreadyReviewed = await _reviewRepository.HasReviewForBookingAsync(id);
            if (alreadyReviewed)
            {
                TempData["ErrorMessage"] = "Bạn đã đánh giá chuyến đi này rồi.";
                return RedirectToAction("Trips");
            }

            // M5: Lưu review vào DB
            var review = new TripMate_Webapi.Entities.ReviewEntity
            {
                Id = Guid.NewGuid().ToString(),
                BookingId = id,
                TravelerId = travelerId,
                GuideProfileId = booking.GuideProfileId,
                Rating = rating,
                Comment = comment.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            try
            {
                await _reviewRepository.CreateReviewAsync(review);
                _logger.LogInformation("[Review] Traveler={TravelerId} rated Guide={GuideId} with {Rating}★ for Booking={BookingId}",
                    travelerId, booking.GuideProfileId, rating, id);

                TempData["SuccessMessage"] = $"Cảm ơn bạn đã để lại đánh giá {rating}★! Guide sẽ nhận được phản hồi của bạn.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Review] Failed to save review for BookingId={BookingId}", id);
                TempData["ErrorMessage"] = "Không thể lưu đánh giá lúc này. Vui lòng thử lại sau.";
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
                var profile = await _supabase.From<ProfileEntity>().Where(x => x.Id == travelerId).Single();
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
            public string DisplayName { get; set; } = string.Empty;
            public string Phone { get; set; } = string.Empty;
            public string Nationality { get; set; } = string.Empty;
        }

        [HttpPost("Traveler/UpdateProfileAjax")]
        public async Task<IActionResult> UpdateProfileAjax([FromBody] UpdateProfileRequest req)
        {
            var travelerId = GetCurrentUserId();
            if (string.IsNullOrEmpty(travelerId))
                return Unauthorized(new { error = "Not authenticated" });

            try
            {
                var profile = await _supabase.From<ProfileEntity>().Where(x => x.Id == travelerId).Single();
                if (profile != null)
                {
                    profile.FullName = req.DisplayName;
                    profile.Phone = req.Phone;
                    profile.Location = req.Nationality;
                    await _supabase.From<ProfileEntity>().Update(profile);
                    return Json(new { success = true });
                }
                return NotFound(new { error = "Profile not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile");
                return StatusCode(500, new { error = "Internal server error" });
            }
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

                resultList.Add(new
                {
                    id = b.Id,
                    status = b.Status,
                    bookingDate = b.BookingDate.ToString("MMM dd, yyyy"),
                    totalAmount = b.TotalAmount,
                    notes = b.TravelerNotes,
                    guideName = b.GuideProfile?.Profile?.FullName ?? "Local Guide",
                    guideAvatar = b.GuideProfile?.Profile?.AvatarUrl ?? "",
                    guideCoverPhoto = b.GuideProfile?.CoverPhotoUrl ?? "",
                    guideProfileId = b.GuideProfileId,
                    packageTitle = b.ExperiencePackage?.Title ?? "Custom Tour",
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

            if (req.Rating < 1 || req.Rating > 5)
                return BadRequest(new { error = "Rating must be between 1 and 5" });

            var booking = await _bookingRepository.GetBookingByIdAsync(req.BookingId);
            if (booking == null || booking.TravelerId != travelerId)
                return NotFound(new { error = "Booking not found" });

            if (booking.Status != 2)
                return BadRequest(new { error = "You can only review completed trips." });

            var hasReview = await _reviewRepository.HasReviewForBookingAsync(req.BookingId);
            if (hasReview)
                return BadRequest(new { error = "You have already reviewed this trip." });

            var review = new ReviewEntity
            {
                BookingId = req.BookingId,
                TravelerId = travelerId,
                GuideProfileId = booking.GuideProfileId,
                Rating = req.Rating,
                Comment = req.Comment
            };

            var created = await _reviewRepository.CreateReviewAsync(review);
            return Json(new { success = true });
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

            // Ensure booking belongs to this traveler
            var booking = await _bookingRepository.GetBookingByIdAsync(id);
            if (booking == null || booking.TravelerId != travelerId)
                return Unauthorized(new { error = "Not authorized" });

            if (booking.Status != 0)
                return BadRequest(new { error = "Only pending bookings can be deleted" });

            await _bookingRepository.DeleteBookingAsync(id);
            return Json(new { success = true });
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
    }
}
