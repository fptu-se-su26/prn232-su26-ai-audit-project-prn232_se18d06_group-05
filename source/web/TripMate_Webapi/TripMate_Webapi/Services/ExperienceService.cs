using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using TripMate_WebAPI.DTOs.Tour.Requests;
using TripMate_WebAPI.DTOs.Tour.Responses;
using TripMate_WebAPI.DTOs.Tour.Scheduling;
using TripMate_Webapi.Entities;
using TripMate_Webapi.Repositories;

namespace TripMate_WebAPI.Services
{
    public class ExperienceService : IExperienceService
    {
        private const long MaxImageSizeBytes = 5 * 1024 * 1024;
        private static readonly HashSet<string> AllowedImageTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg",
            "image/png"
        };

        private readonly ICloudinaryService _cloudinaryService;
        private readonly IExperiencePackageRepository _repository;
        private readonly IBookingRepository _bookingRepository;
        private readonly IReviewRepository _reviewRepository;

        public ExperienceService(
            ICloudinaryService cloudinaryService,
            IExperiencePackageRepository repository,
            IBookingRepository bookingRepository,
            IReviewRepository reviewRepository)
        {
            _cloudinaryService = cloudinaryService;
            _repository = repository;
            _bookingRepository = bookingRepository;
            _reviewRepository = reviewRepository;
        }

        public async Task<ExperiencePackageEntity> CreateTourAsync(CreateTourDto dto, string guideProfileId)
        {
            ValidatePricing(dto);

            var existing = string.IsNullOrWhiteSpace(dto.Id)
                ? null
                : await _repository.GetPackageByIdAsync(dto.Id, guideProfileId);
            if (!string.IsNullOrWhiteSpace(dto.Id) && existing == null)
                throw new ArgumentException("The experience package could not be found.");

            ValidateImage(dto.CoverImage, "Cover image");
            foreach (var file in dto.GalleryImages ?? [])
                ValidateImage(file, "Gallery image");

            if ((existing == null || string.IsNullOrWhiteSpace(existing.CoverImageUrl)) && dto.CoverImage == null)
                throw new ArgumentException("A cover image is required before publishing.");

            var includedServicesList = DeserializeJsonArray(dto.IncludedServices);
            var languagesList = DeserializeJsonArray(dto.Languages);
            var tagsList = DeserializeJsonArray(dto.Tags);
            if (languagesList.Count == 0)
                throw new ArgumentException("Select at least one language.");

            var (schedule, persistedItinerary) = ResolvePublishedSchedule(dto, existing);

            var retainedGalleryUrls = DeserializeJsonArray(dto.RetainedGalleryImages);
            if (existing == null)
            {
                retainedGalleryUrls.Clear();
            }
            else
            {
                var existingUrls = (existing.GalleryImageUrls ?? [])
                    .ToHashSet(StringComparer.Ordinal);
                retainedGalleryUrls = retainedGalleryUrls
                    .Where(existingUrls.Contains)
                    .Distinct(StringComparer.Ordinal)
                    .ToList();
            }

            if (retainedGalleryUrls.Count + (dto.GalleryImages?.Count ?? 0) > 5)
                throw new ArgumentException("You can add up to five gallery images.");

            // Upload media only after every non-file rule has passed.
            string coverUrl = string.Empty;
            if (dto.CoverImage != null)
            {
                coverUrl = await _cloudinaryService.UploadImageAsync(dto.CoverImage) ?? string.Empty;
            }

            var newGalleryUrls = new List<string>();
            if (dto.GalleryImages != null && dto.GalleryImages.Count > 0)
            {
                newGalleryUrls = await _cloudinaryService.UploadImagesAsync(dto.GalleryImages);
            }
            var galleryUrls = retainedGalleryUrls.Concat(newGalleryUrls).Take(5).ToList();

            var entity = new ExperiencePackageEntity
            {
                GuideProfileId = guideProfileId,
                Title = dto.Title.Trim(),
                City = dto.City.Trim(),
                MeetingPoint = dto.MeetingPoint.Trim(),
                Description = dto.Description.Trim(),
                PricePerSession = dto.PricePerSession,
                PricePerPerson = dto.PricePerGuest,
                IncludedGuestCount = dto.IncludedGuestCount,
                MaxGroupSize = dto.MaxGroupSize,
                IncludedItems = includedServicesList,
                Languages = languagesList,
                Tags = tagsList,
                TimelineJson = persistedItinerary,
                CoverImageUrl = coverUrl,
                GalleryImageUrls = galleryUrls,
                IsActive = true,
                PublicationStatus = "published",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            TourScheduleCompatibility.Apply(entity, schedule, dto.DurationHours);

            if (existing != null)
            {
                entity.Id = existing.Id;
                entity.CreatedAt = existing.CreatedAt;
                entity.IsActive = existing.IsActive;
                entity.PublicationStatus = existing.PublicationStatus == "draft" ? "published" : existing.PublicationStatus;
                if (existing.PublicationStatus == "draft") entity.IsActive = true;
                if (string.IsNullOrEmpty(coverUrl)) entity.CoverImageUrl = existing.CoverImageUrl;
                return await _repository.UpdatePackageAsync(entity);
            }

            return await _repository.CreatePackageAsync(entity);
        }

        public async Task<TourEditorDto?> GetTourEditorAsync(string id, string guideProfileId)
        {
            var entity = await _repository.GetPackageByIdAsync(id, guideProfileId);
            if (entity == null) return null;

            return new TourEditorDto
            {
                Id = entity.Id,
                Title = entity.Title,
                DurationHours = entity.DurationHours,
                Schedule = TourScheduleCompatibility.FromEntity(entity),
                MaxGroupSize = entity.MaxGroupSize,
                City = entity.City,
                MeetingPoint = entity.MeetingPoint,
                Description = entity.Description,
                PricePerSession = entity.PricePerSession,
                AdditionalGuestFee = entity.PricePerPerson ?? 0,
                IncludedGuestCount = Math.Max(1, entity.IncludedGuestCount),
                ItineraryDays = TourItineraryMapper.Expand(entity.TimelineJson),
                TimelineJson = TourItineraryMapper.ToLegacyEditorTimeline(entity.TimelineJson),
                Languages = entity.Languages ?? [],
                IncludedItems = entity.IncludedItems ?? [],
                Tags = entity.Tags ?? [],
                CoverImageUrl = entity.CoverImageUrl,
                GalleryImageUrls = entity.GalleryImageUrls ?? [],
                PublicationStatus = NormalizePublicationStatus(entity)
            };
        }

        public async Task<List<MyTourDashboardDto>> GetMyToursAsync(string guideProfileId)
        {
            var packagesTask = _repository.GetPackagesByGuideIdAsync(guideProfileId);
            var bookingsTask = _bookingRepository.GetBookingsForGuideAsync(guideProfileId);
            var reviewsTask = _reviewRepository.GetReviewsByGuideAsync(guideProfileId);
            await Task.WhenAll(packagesTask, bookingsTask, reviewsTask);

            var entities = await packagesTask;
            var bookings = await bookingsTask;
            var reviews = await reviewsTask;
            var dtos = new List<MyTourDashboardDto>();

            foreach (var entity in entities)
            {
                var tourBookings = bookings
                    .Where(booking => booking.ExperiencePackageId == entity.Id && booking.Status is >= 0 and <= 2)
                    .ToList();
                var completedBookings = tourBookings.Where(booking => booking.Status == 2).ToList();
                var bookingIds = tourBookings.Select(booking => booking.Id).ToHashSet(StringComparer.Ordinal);
                var tourReviews = reviews.Where(review => bookingIds.Contains(review.BookingId)).ToList();
                var status = NormalizePublicationStatus(entity);
                var itineraryDays = TourItineraryMapper.Expand(entity.TimelineJson);
                var scheduleSummary = BuildScheduleSummary(entity, itineraryDays);
                var quality = EvaluateListingQuality(entity, scheduleSummary);
                dtos.Add(new MyTourDashboardDto
                {
                    Id = entity.Id,
                    Name = entity.Title,
                    Duration = entity.DurationHours,
                    MaxGuests = entity.MaxGroupSize,
                    IncludedGuests = Math.Max(1, entity.IncludedGuestCount),
                    Price = entity.PricePerSession,
                    AdditionalGuestFee = entity.PricePerPerson ?? 0,
                    City = entity.City,
                    Tags = entity.Tags ?? new List<string>(),
                    IsActive = status == "published",
                    PublicationStatus = status,
                    ImageUrl = entity.CoverImageUrl ?? "",
                    Description = entity.Description,
                    MeetingPoint = entity.MeetingPoint,
                    Languages = entity.Languages ?? [],
                    BookingCount = tourBookings.Count,
                    CompletedBookingCount = completedBookings.Count,
                    Revenue = completedBookings.Sum(booking => booking.GuideEarnings),
                    AverageRating = tourReviews.Count == 0 ? null : (decimal)Math.Round(tourReviews.Average(review => review.Rating), 1),
                    ReviewCount = tourReviews.Count,
                    CompletenessScore = quality.Score,
                    MissingQualityItems = quality.MissingItems,
                    Schedule = scheduleSummary,
                    ItineraryDays = itineraryDays,
                    UpdatedAt = entity.UpdatedAt == default ? entity.CreatedAt : entity.UpdatedAt
                });
            }

            return dtos;
        }

        public async Task<bool> ToggleTourStatusAsync(string tourId, string guideProfileId)
        {
            var package = await _repository.GetPackageByIdAsync(tourId, guideProfileId);
            if (package == null) return false;
            if (NormalizePublicationStatus(package) == "draft")
                throw new ArgumentException("Complete and publish this draft from the editor.");

            package.IsActive = !package.IsActive;
            package.PublicationStatus = package.IsActive ? "published" : "hidden";
            package.UpdatedAt = DateTime.UtcNow;
            await _repository.UpdatePackageAsync(package);
            return true;
        }

        public async Task<TourRemovalOutcome> DeleteTourAsync(string tourId, string guideProfileId)
        {
            var package = await _repository.GetPackageByIdAsync(tourId, guideProfileId)
                ?? throw new ArgumentException("The experience package could not be found.");
            var tourBookings = (await _bookingRepository.GetBookingsForGuideAsync(guideProfileId))
                .Where(booking => booking.ExperiencePackageId == tourId)
                .ToList();

            if (tourBookings.Any(booking => booking.Status is -1 or 0 or 1))
                throw new ArgumentException("This tour has an active booking and cannot be deleted. Hide it to prevent new bookings.");

            if (tourBookings.Count > 0)
            {
                package.IsActive = false;
                package.PublicationStatus = "hidden";
                package.UpdatedAt = DateTime.UtcNow;
                await _repository.UpdatePackageAsync(package);
                return TourRemovalOutcome.Archived;
            }

            await _repository.DeletePackageAsync(tourId, guideProfileId);
            return TourRemovalOutcome.Deleted;
        }

        public async Task<ExperiencePackageEntity?> DuplicateTourAsync(string tourId, string guideProfileId)
        {
            var existingTour = await _repository.GetPackageByIdAsync(tourId, guideProfileId);
            if (existingTour == null) return null;

            existingTour.Id = Guid.NewGuid().ToString();
            existingTour.Title = existingTour.Title + " (Copy)";
            existingTour.IsActive = false;
            existingTour.PublicationStatus = "draft";
            existingTour.CreatedAt = DateTime.UtcNow;
            existingTour.UpdatedAt = DateTime.UtcNow;

            return await _repository.CreatePackageAsync(existingTour);
        }

        public async Task<ExperiencePackageEntity> SaveTourDraftAsync(SaveTourDraftDto dto, string guideProfileId)
        {
            var existing = string.IsNullOrWhiteSpace(dto.Id)
                ? null
                : await _repository.GetPackageByIdAsync(dto.Id, guideProfileId);
            if (!string.IsNullOrWhiteSpace(dto.Id) && existing == null)
                throw new ArgumentException("The draft could not be found.");
            if (existing != null && NormalizePublicationStatus(existing) != "draft")
                throw new ArgumentException("Published tours are saved only when you choose Save Changes.");

            var entity = existing ?? new ExperiencePackageEntity
            {
                Id = Guid.NewGuid().ToString(),
                GuideProfileId = guideProfileId,
                CreatedAt = DateTime.UtcNow
            };

            entity.Title = (dto.Title ?? string.Empty).Trim();
            entity.City = (dto.City ?? string.Empty).Trim();
            entity.MeetingPoint = (dto.MeetingPoint ?? string.Empty).Trim();
            entity.Description = (dto.Description ?? string.Empty).Trim();
            var usesStructuredSchedule = UsesStructuredSchedule(dto);
            var preserveStructuredData = !usesStructuredSchedule && HasStructuredTourData(existing);
            var draftSchedule = ResolveDraftSchedule(dto, existing, preserveStructuredData);
            TourScheduleCompatibility.Apply(entity, draftSchedule, dto.DurationHours);
            entity.PricePerSession = Math.Max(0, dto.PricePerSession);
            entity.PricePerPerson = Math.Max(0, dto.PricePerGuest);
            entity.IncludedGuestCount = Math.Clamp(dto.IncludedGuestCount, 1, 50);
            entity.MaxGroupSize = Math.Clamp(Math.Max(dto.MaxGroupSize, entity.IncludedGuestCount), 1, 50);
            entity.IncludedItems = dto.IncludedServices?.Where(value => !string.IsNullOrWhiteSpace(value)).Distinct().ToList() ?? [];
            entity.Languages = dto.Languages?.Where(value => !string.IsNullOrWhiteSpace(value)).Distinct().ToList() ?? [];
            entity.Tags = dto.Tags?.Where(value => !string.IsNullOrWhiteSpace(value)).Distinct().Take(5).ToList() ?? [];
            entity.TimelineJson = dto.ItineraryDays != null
                ? TourItineraryMapper.Flatten(dto.ItineraryDays)
                : preserveStructuredData
                    ? existing!.TimelineJson ?? []
                : dto.Timeline ?? [];
            entity.IsActive = false;
            entity.PublicationStatus = "draft";
            entity.UpdatedAt = DateTime.UtcNow;

            return existing == null
                ? await _repository.CreatePackageAsync(entity)
                : await _repository.UpdatePackageAsync(entity);
        }

        private static (TourScheduleDto Schedule, List<Dictionary<string, string>> Itinerary)
            ResolvePublishedSchedule(CreateTourDto dto, ExperiencePackageEntity? existing)
        {
            if (!UsesStructuredSchedule(dto))
            {
                if (HasStructuredTourData(existing))
                {
                    return (
                        TourScheduleCompatibility.FromEntity(existing!),
                        existing!.TimelineJson ?? []);
                }

                var legacyTimeline = TourItineraryMapper.DeserializeLegacy(dto.TimelineJson);
                ThrowIfInvalid(TourItineraryMapper.ValidateLegacyForPublish(legacyTimeline));
                return (TourScheduleCompatibility.FromLegacyDuration(dto.DurationHours), legacyTimeline);
            }

            var durationType = TourSchedulePolicy.NormalizeDurationType(dto.DurationType);
            var schedule = new TourScheduleDto
            {
                DurationType = durationType,
                DurationMinutes = dto.DurationMinutes,
                DurationDays = dto.DurationDays ??
                    (durationType == TourDurationTypes.SameDay ? 1 : 0),
                DefaultStartTime = dto.DefaultStartTime,
                DefaultEndTime = dto.DefaultEndTime,
                TimeZone = string.IsNullOrWhiteSpace(dto.TimeZone)
                    ? TourSchedulePolicy.DefaultTimeZone
                    : dto.TimeZone
            };

            ThrowIfInvalid(TourSchedulePolicy.ValidateForPublish(schedule));
            schedule.DurationMinutes = TourSchedulePolicy.CalculateElapsedMinutes(schedule);

            var itineraryDays = TourItineraryMapper.DeserializeStructured(dto.ItineraryJson);
            ThrowIfInvalid(TourItineraryPolicy.ValidateForPublish(itineraryDays, schedule));

            return (schedule, TourItineraryMapper.Flatten(itineraryDays));
        }

        private static TourScheduleDto ResolveDraftSchedule(
            SaveTourDraftDto dto,
            ExperiencePackageEntity? existing,
            bool preserveStructuredData)
        {
            if (!UsesStructuredSchedule(dto))
            {
                return preserveStructuredData
                    ? TourScheduleCompatibility.FromEntity(existing!)
                    : TourScheduleCompatibility.FromLegacyDuration(dto.DurationHours);
            }

            var current = existing != null
                ? TourScheduleCompatibility.FromEntity(existing)
                : TourScheduleCompatibility.FromLegacyDuration(dto.DurationHours);
            var scheduleShapeChanged = dto.DurationType != null ||
                                       dto.DurationDays.HasValue ||
                                       dto.DefaultStartTime != null ||
                                       dto.DefaultEndTime != null;

            return new TourScheduleDto
            {
                DurationType = dto.DurationType ?? current.DurationType,
                DurationMinutes = dto.DurationMinutes ??
                    (scheduleShapeChanged ? null : current.DurationMinutes),
                DurationDays = dto.DurationDays ?? current.DurationDays,
                DefaultStartTime = dto.DefaultStartTime ?? current.DefaultStartTime,
                DefaultEndTime = dto.DefaultEndTime ?? current.DefaultEndTime,
                TimeZone = dto.TimeZone ?? current.TimeZone
            };
        }

        private static bool UsesStructuredSchedule(CreateTourDto dto)
            => dto.ItineraryJson != null ||
               !string.IsNullOrWhiteSpace(dto.DurationType) ||
               dto.DurationMinutes.HasValue ||
               dto.DurationDays.HasValue ||
               dto.DefaultStartTime != null ||
               dto.DefaultEndTime != null ||
               dto.TimeZone != null;

        private static bool HasStructuredTourData(ExperiencePackageEntity? entity)
            => entity != null &&
               TourScheduleCompatibility.HasConfiguredSchedule(entity) &&
               TourItineraryMapper.IsStructured(entity.TimelineJson);

        private static bool UsesStructuredSchedule(SaveTourDraftDto dto)
            => dto.ItineraryDays != null ||
               !string.IsNullOrWhiteSpace(dto.DurationType) ||
               dto.DurationMinutes.HasValue ||
               dto.DurationDays.HasValue ||
               dto.DefaultStartTime != null ||
               dto.DefaultEndTime != null ||
               dto.TimeZone != null;

        private static void ThrowIfInvalid(IReadOnlyList<string> errors)
        {
            if (errors.Count == 0) return;
            throw new ArgumentException(string.Join(" ", errors.Distinct(StringComparer.Ordinal)));
        }

        private List<string> DeserializeJsonArray(string json)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(json)) return new List<string>();
                return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }

        private static void ValidatePricing(CreateTourDto dto)
        {
            if (dto.IncludedGuestCount < 1)
                throw new ArgumentException("The base tour price must include at least one guest.");
            if (dto.MaxGroupSize < dto.IncludedGuestCount)
                throw new ArgumentException("Maximum group size cannot be lower than the included guest count.");
            if (dto.PricePerSession <= 0)
                throw new ArgumentException("Base tour price must be greater than zero.");
            if (dto.PricePerGuest < 0)
                throw new ArgumentException("Additional guest fee cannot be negative.");
        }

        private static void ValidateImage(Microsoft.AspNetCore.Http.IFormFile? file, string label)
        {
            if (file == null) return;
            if (file.Length <= 0 || file.Length > MaxImageSizeBytes)
                throw new ArgumentException($"{label} must be smaller than 5 MB.");
            if (!AllowedImageTypes.Contains(file.ContentType))
                throw new ArgumentException($"{label} must be a JPG or PNG file.");
        }

        private static string NormalizePublicationStatus(ExperiencePackageEntity entity)
        {
            if (entity.PublicationStatus is "draft" or "published" or "hidden")
                return entity.PublicationStatus;
            return entity.IsActive ? "published" : "hidden";
        }

        private static MyTourScheduleSummaryDto BuildScheduleSummary(
            ExperiencePackageEntity entity,
            IReadOnlyCollection<TourItineraryDayDto> itineraryDays)
        {
            var schedule = TourScheduleCompatibility.FromEntity(entity);
            var isConfigured = TourScheduleCompatibility.HasConfiguredSchedule(entity);
            return new MyTourScheduleSummaryDto
            {
                DurationType = schedule.DurationType,
                DurationMinutes = schedule.DurationMinutes,
                DurationDays = schedule.DurationDays,
                StartTime = schedule.DefaultStartTime,
                EndTime = schedule.DefaultEndTime,
                TimeZone = schedule.TimeZone,
                IsConfigured = isConfigured,
                DurationLabel = TourScheduleFormatter.FormatDuration(schedule),
                TimeRangeLabel = isConfigured
                    ? $"{schedule.DefaultStartTime}–{schedule.DefaultEndTime}"
                    : "Time not set",
                ItineraryDayCount = itineraryDays.Count,
                ActivityCount = itineraryDays.Sum(day => day.Items?.Count ?? 0)
            };
        }

        private static (int Score, List<string> MissingItems) EvaluateListingQuality(
            ExperiencePackageEntity entity,
            MyTourScheduleSummaryDto schedule)
        {
            var checks = new (bool Complete, string MissingMessage)[]
            {
                (entity.Title.Trim().Length >= 5, "Add a clear tour name"),
                (!string.IsNullOrWhiteSpace(entity.City), "Choose a destination"),
                (!string.IsNullOrWhiteSpace(entity.MeetingPoint), "Add a specific meeting point"),
                (entity.Description.Trim().Length >= 20, "Write a more useful tour description"),
                (entity.DurationHours >= 0.5m, "Set a valid duration"),
                (schedule.IsConfigured, "Set the default start and end times"),
                (entity.PricePerSession > 0, "Set the base tour price"),
                (entity.MaxGroupSize >= Math.Max(1, entity.IncludedGuestCount), "Confirm the group capacity"),
                (!string.IsNullOrWhiteSpace(entity.CoverImageUrl), "Upload a cover photo"),
                (entity.Languages?.Count > 0, "Select at least one language"),
                (entity.TimelineJson?.Count > 0, "Add at least one itinerary item")
            };
            var missingItems = checks.Where(check => !check.Complete).Select(check => check.MissingMessage).ToList();
            var score = (int)Math.Round(checks.Count(check => check.Complete) * 100m / checks.Length);
            return (score, missingItems);
        }
    }
}
