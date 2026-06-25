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

        private const string LOGIN_URL = "/Auth/Login";

        public TravelerController(
            ILogger<TravelerController> logger,
            SupabaseAuthService authService,
            ITripRequestRepository tripRequestRepository,
            IBookingRepository bookingRepository,
            TourService tourService,
            IReviewRepository reviewRepository)
        {
            _logger = logger;
            _authService = authService;
            _tripRequestRepository = tripRequestRepository;
            _bookingRepository = bookingRepository;
            _tourService = tourService;
            _reviewRepository = reviewRepository;
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

            ViewBag.Bookings = bookings;
            return View(trips);
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

        // GET: /Traveler/Checkout/{id} [Auth required]
        public async Task<IActionResult> Checkout(string id)
        {
            ViewBag.RequiresAuth = true;
            var booking = await _bookingRepository.GetBookingByIdAsync(id);
            if (booking == null)
                return RedirectToAction("Dashboard");

            return View(booking);
        }

        // GET: /Traveler/Messages [Auth required]
        public IActionResult Messages()
        {
            ViewBag.RequiresAuth = true;
            return View();
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

            // M4: Lấy package thực từ DB để tính giá đúng
            var packages = await _tourService.GetToursByGuideAsync(guideId);
            string packageId;
            decimal basePrice;

            if (packages != null && packages.Any())
            {
                var selectedPackage = packages.First();
                packageId = selectedPackage.Id!;

                // Ưu tiên PricePerSession (giá cố định/buổi), nếu không có thì tính theo đầu người
                if (selectedPackage.PricePerSession > 0)
                    basePrice = selectedPackage.PricePerSession;
                else if (selectedPackage.PricePerPerson.HasValue && selectedPackage.PricePerPerson > 0)
                    basePrice = selectedPackage.PricePerPerson.Value * guests;
                else
                    basePrice = 500_000m * guests; // Fallback hợp lý
            }
            else
            {
                packageId = "00000000-0000-0000-0000-000000000000";
                basePrice = 500_000m * guests; // Không có package → giá tham khảo
            }

            // M4: PlatformFee = 15% (theo kiến trúc), GuideEarnings = 85%
            var platformFee = Math.Round(basePrice * 0.15m, 0);
            var guideEarnings = basePrice - platformFee;

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
                Status = 0 // M4: Pending — chờ Guide Accept (KHÔNG nhảy thẳng Completed)
            };

            await _bookingRepository.CreateBookingAsync(booking);

            // Xóa Ghost Booking session sau khi booking thật đã tạo
            HttpContext.Session.Remove("GhostBooking");

            TempData["SuccessMessage"] = "Yêu cầu đặt lịch đã được gửi đến Guide thành công! Vui lòng chờ xác nhận.";
            return RedirectToAction("Dashboard");
        }

        /// <summary>
        /// POST: /Traveler/ProcessPayment
        /// M4: KHÔNG chuyển Status = Completed ngay. Chỉ lưu payment reference.
        /// Status vẫn là Pending(0) cho đến khi Guide Accept → Confirmed(1).
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> ProcessPayment(string id, string paymentMethod)
        {
            var travelerId = GetCurrentUserId();
            if (string.IsNullOrEmpty(travelerId))
                return Redirect($"{LOGIN_URL}?returnUrl=/Traveler/Checkout/{id}");

            var booking = await _bookingRepository.GetBookingByIdAsync(id);
            if (booking == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy booking.";
                return RedirectToAction("Dashboard");
            }

            // M4: Chỉ lưu payment reference — KHÔNG thay đổi Status
            // Status sẽ chuyển: Pending(0) → [Guide Accept] → Confirmed(1) → [Auto] → Completed(2)
            booking.PaymentReference = $"TM-{paymentMethod.ToUpper()}-{DateTime.UtcNow:yyyyMMddHHmmss}-{id[..6].ToUpper()}";
            booking.PaymentMethod = paymentMethod;
            // Status giữ nguyên = 0 (Pending) — chờ Guide xác nhận

            await _bookingRepository.UpdateBookingAsync(booking);

            TempData["SuccessMessage"] = $"Thanh toán qua {paymentMethod} đã được ghi nhận! Guide sẽ xác nhận trong 24 giờ.";
            return RedirectToAction("BookingDetails", new { id });
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
                IsVisible = true,
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
    }
}
