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

        public class UpdateGuideProfileRequest
        {
            public string? FullName { get; set; }
            public string? PhoneNumber { get; set; }
            public string? Nationality { get; set; }
            public string? CityArea { get; set; }
            public string? Bio { get; set; }
            public string? Languages { get; set; }
            public string? Specialties { get; set; }
            public decimal? BaseRate { get; set; }
            public IFormFile? AvatarFile { get; set; }
            public IFormFile? CoverFile { get; set; }
        }

        [HttpPost]
        [Authorize(Roles = "guide")]
        public async Task<IActionResult> UpdateProfileAjax([FromForm] UpdateGuideProfileRequest req, [FromServices] TripMate_WebAPI.Services.ICloudinaryService cloudinary)
        {
            var userId = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation("UpdateProfileAjax called. UserId={UserId}, FullName={FullName}, Bio={Bio}, Languages={Languages}, Specialties={Specialties}, BaseRate={BaseRate}, CityArea={CityArea}",
                userId, req.FullName, req.Bio, req.Languages, req.Specialties, req.BaseRate, req.CityArea);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { error = "Not authenticated" });

            try
            {
                // Update User Profile
                var profileResponse = await _supabase.From<TripMate_Webapi.Entities.ProfileEntity>().Where(x => x.Id == userId).Get();
                var profile = profileResponse.Models.FirstOrDefault();
                _logger.LogInformation("Profile found: {Found}, CurrentName={Name}", profile != null, profile?.FullName);

                if (profile != null)
                {
                    if (req.FullName != null) profile.FullName = req.FullName;
                    if (req.PhoneNumber != null) profile.Phone = req.PhoneNumber;
                    if (req.Nationality != null) profile.Location = req.Nationality;

                    if (req.AvatarFile != null)
                    {
                        var avatarUrl = await cloudinary.UploadImageAsync(req.AvatarFile, "tripmate_avatars");
                        if (!string.IsNullOrEmpty(avatarUrl))
                            profile.AvatarUrl = avatarUrl;
                    }

                    var updateResult = await _supabase.From<TripMate_Webapi.Entities.ProfileEntity>().Update(profile);
                    _logger.LogInformation("ProfileEntity update result: {Count} models returned", updateResult.Models.Count);
                }

                // Update Guide Profile
                var guideResponse = await _supabase.From<TripMate_Webapi.Entities.GuideProfileEntity>().Where(x => x.UserId == userId).Get();
                var guideProfile = guideResponse.Models.FirstOrDefault();
                _logger.LogInformation("GuideProfile found: {Found}, Id={Id}", guideProfile != null, guideProfile?.Id);

                if (guideProfile == null)
                {
                    guideProfile = new TripMate_Webapi.Entities.GuideProfileEntity
                    {
                        UserId = userId,
                        Bio = req.Bio ?? "",
                        CityArea = req.CityArea ?? "Hội An",
                        PricePerHour = req.BaseRate ?? 50000,
                        Languages = req.Languages != null ? req.Languages.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList() : new List<string>(),
                        Specialties = req.Specialties != null ? req.Specialties.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList() : new List<string>()
                    };

                    if (req.CoverFile != null)
                    {
                        var coverUrl = await cloudinary.UploadImageAsync(req.CoverFile, "tripmate_covers");
                        if (!string.IsNullOrEmpty(coverUrl))
                            guideProfile.CoverPhotoUrl = coverUrl;
                    }

                    var insertResult = await _supabase.From<TripMate_Webapi.Entities.GuideProfileEntity>().Insert(guideProfile);
                    _logger.LogInformation("GuideProfile INSERT result: {Count} models", insertResult.Models.Count);
                }
                else
                {
                    if (req.CityArea != null) guideProfile.CityArea = req.CityArea;
                    if (req.Bio != null) guideProfile.Bio = req.Bio;
                    if (req.BaseRate != null) guideProfile.PricePerHour = req.BaseRate;
                    
                    if (req.Languages != null) 
                        guideProfile.Languages = req.Languages.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList();
                    
                    if (req.Specialties != null) 
                        guideProfile.Specialties = req.Specialties.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList();

                    if (req.CoverFile != null)
                    {
                        var coverUrl = await cloudinary.UploadImageAsync(req.CoverFile, "tripmate_covers");
                        if (!string.IsNullOrEmpty(coverUrl))
                            guideProfile.CoverPhotoUrl = coverUrl;
                    }

                    var updateResult = await _supabase.From<TripMate_Webapi.Entities.GuideProfileEntity>().Update(guideProfile);
                    _logger.LogInformation("GuideProfile UPDATE result: {Count} models", updateResult.Models.Count);
                }

                _logger.LogInformation("UpdateProfileAjax SUCCESS for userId={UserId}", userId);
                return Json(new { success = true, avatarUrl = profile?.AvatarUrl, coverUrl = guideProfile?.CoverPhotoUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating guide profile for userId={UserId}", userId);
                return StatusCode(500, new { error = "Internal server error", details = ex.Message });
            }
        }

        [HttpGet]
        [Authorize(Roles = "guide")]
        public async Task<IActionResult> GetProfileAjax()
        {
            var userId = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { error = "Not authenticated" });

            try
            {
                var profileResponse = await _supabase.From<TripMate_Webapi.Entities.ProfileEntity>().Where(x => x.Id == userId).Get();
                var profile = profileResponse.Models.FirstOrDefault();

                var guideResponse = await _supabase.From<TripMate_Webapi.Entities.GuideProfileEntity>().Where(x => x.UserId == userId).Get();
                var guideProfile = guideResponse.Models.FirstOrDefault();

                if (profile == null) 
                    return NotFound(new { error = "Profile not found" });

                return Json(new {
                    full_name = profile.FullName,
                    phone_number = profile.Phone,
                    nationality = profile.Location,
                    avatar_url = profile.AvatarUrl,
                    bio = guideProfile?.Bio ?? "",
                    cover_photo_url = guideProfile?.CoverPhotoUrl ?? "",
                    city_area = guideProfile?.CityArea ?? "Hội An",
                    languages = guideProfile?.Languages != null ? string.Join(", ", guideProfile.Languages) : "",
                    specialties = guideProfile?.Specialties != null ? string.Join(", ", guideProfile.Specialties) : "",
                    average_rating = guideProfile?.AverageRating ?? 5.0m,
                    total_reviews = guideProfile?.TotalReviews ?? 0,
                    base_rate = guideProfile?.PricePerHour ?? 50000m
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching guide profile");
                return StatusCode(500, new { error = "Internal server error" });
            }
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
        public async Task<IActionResult> Profile()
        {
            var userId = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                var profileResponse = await _supabase.From<TripMate_Webapi.Entities.ProfileEntity>().Where(x => x.Id == userId).Get();
                var profile = profileResponse.Models.FirstOrDefault();

                var guideResponse = await _supabase.From<TripMate_Webapi.Entities.GuideProfileEntity>().Where(x => x.UserId == userId).Get();
                var guideProfile = guideResponse.Models.FirstOrDefault();

                ViewBag.Profile = new {
                    full_name = profile?.FullName ?? "",
                    phone_number = profile?.Phone ?? "",
                    nationality = profile?.Location ?? "",
                    avatar_url = profile?.AvatarUrl ?? "/images/AVATAR.png",
                    bio = guideProfile?.Bio ?? "",
                    cover_photo_url = guideProfile?.CoverPhotoUrl ?? "",
                    city_area = guideProfile?.CityArea ?? "Hội An",
                    languages = guideProfile?.Languages != null ? string.Join(", ", guideProfile.Languages) : "",
                    specialties = guideProfile?.Specialties != null ? string.Join(", ", guideProfile.Specialties) : "",
                    average_rating = guideProfile?.AverageRating ?? 5.0m,
                    total_reviews = guideProfile?.TotalReviews ?? 0,
                    base_rate = guideProfile?.PricePerHour ?? 50000m
                };
            }

            return View();
        }

        public class UpdateGuideProfileDto
        {
            public string? FullName { get; set; }
            public string? PhoneNumber { get; set; }
            public string? AvatarUrl { get; set; }
            public string? Location { get; set; }
            public string? Bio { get; set; }
            public List<string>? Languages { get; set; }
            public List<string>? Specialties { get; set; }
            public string? CityArea { get; set; }
            public decimal? PricePerHour { get; set; }
            public string? CoverPhotoUrl { get; set; }
        }

        [HttpPost("/Guide/UpdateProfile")]
        [Authorize(Roles = "guide")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateGuideProfileDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            try
            {
                // Update base profile (profiles table)
                var profileResponse = await _supabase.From<ProfileEntity>().Where(x => x.Id == userId).Get();
                var profile = profileResponse.Models.FirstOrDefault();
                if (profile != null)
                {
                    if (dto.FullName != null) profile.FullName = dto.FullName;
                    if (dto.PhoneNumber != null) profile.Phone = dto.PhoneNumber;
                    if (dto.AvatarUrl != null) profile.AvatarUrl = dto.AvatarUrl;
                    if (dto.Location != null) profile.Location = dto.Location;
                    await _supabase.From<ProfileEntity>().Update(profile);
                }

                // Update guide profile (guide_profiles table)
                var guideProfileResponse = await _supabase.From<GuideProfileEntity>().Where(x => x.UserId == userId).Get();
                var guideProfile = guideProfileResponse.Models.FirstOrDefault();
                if (guideProfile != null)
                {
                    if (dto.Bio != null) guideProfile.Bio = dto.Bio;
                    if (dto.Languages != null) guideProfile.Languages = dto.Languages;
                    if (dto.Specialties != null) guideProfile.Specialties = dto.Specialties;
                    if (dto.CityArea != null) guideProfile.CityArea = dto.CityArea;
                    if (dto.PricePerHour != null) guideProfile.PricePerHour = dto.PricePerHour;
                    if (dto.CoverPhotoUrl != null) guideProfile.CoverPhotoUrl = dto.CoverPhotoUrl;
                    await _supabase.From<GuideProfileEntity>().Update(guideProfile);
                }

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating guide profile");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
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
            
            return View(tours);
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
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
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

                var outcome = await _experienceService.DeleteTourAsync(id, guideProfileId);
                var message = outcome == TourRemovalOutcome.Archived
                    ? "This tour has booking history, so it was archived instead of permanently deleted."
                    : "Experience package permanently deleted.";
                return Ok(new { success = true, data = new { outcome = outcome.ToString().ToLowerInvariant() }, message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
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
                
                if (newTour != null) return Ok(new { success = true, data = new { id = newTour.Id }, message = "A draft copy has been created." });
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
                additionalGuestFee = tour.PricePerPerson,
                includedGuestCount = Math.Max(1, tour.IncludedGuestCount),
                timelineJson = tour.TimelineJson,
                languages = tour.Languages,
                includedItems = tour.IncludedItems,
                tags = tour.Tags,
                coverImageUrl = tour.CoverImageUrl,
                galleryImageUrls = tour.GalleryImageUrls,
                publicationStatus = string.IsNullOrWhiteSpace(tour.PublicationStatus)
                    ? (tour.IsActive ? "published" : "hidden")
                    : tour.PublicationStatus
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
                    var errors = ModelState
                        .Where(item => item.Value?.Errors.Count > 0)
                        .ToDictionary(
                            item => item.Key,
                            item => item.Value!.Errors.Select(error => error.ErrorMessage).ToArray());
                    var firstError = errors.Values.SelectMany(value => value).FirstOrDefault();
                    return BadRequest(new
                    {
                        success = false,
                        message = firstError ?? "Please review the highlighted fields.",
                        errors
                    });
                }

                var guideProfileId = await GetCurrentGuideProfileIdAsync();
                if (string.IsNullOrEmpty(guideProfileId)) return Unauthorized(new { success = false, message = "You need a guide profile to create an experience package." });

                var createdTour = await _experienceService.CreateTourAsync(dto, guideProfileId);

                var message = string.IsNullOrWhiteSpace(dto.Id)
                    ? "Your experience package has been published."
                    : "Your changes have been saved.";
                return Ok(new { success = true, data = new { id = createdTour.Id }, message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating tour");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [Authorize(Roles = "guide")]
        public async Task<IActionResult> SaveTourDraft([FromBody] SaveTourDraftDto dto)
        {
            try
            {
                var guideProfileId = await GetCurrentGuideProfileIdAsync();
                if (string.IsNullOrEmpty(guideProfileId))
                    return Unauthorized(new { success = false, message = "You need a guide profile to save a draft." });

                var draft = await _experienceService.SaveTourDraftAsync(dto, guideProfileId);
                return Ok(new
                {
                    success = true,
                    data = new { id = draft.Id, savedAt = draft.UpdatedAt },
                    message = "Draft saved."
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving tour draft");
                return StatusCode(500, new { success = false, message = "The draft could not be saved." });
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
                        Status = BookingService.MapStatus(b.Status),
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
