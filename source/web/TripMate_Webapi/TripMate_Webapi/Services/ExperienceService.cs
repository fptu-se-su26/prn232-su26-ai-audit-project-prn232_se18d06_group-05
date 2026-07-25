using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using TripMate_WebAPI.DTOs.Tour.Requests;
using TripMate_WebAPI.DTOs.Tour.Responses;
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

            // Upload media only after the request passes validation.
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

            var includedServicesList = DeserializeJsonArray(dto.IncludedServices);
            var languagesList = DeserializeJsonArray(dto.Languages);
            var tagsList = DeserializeJsonArray(dto.Tags);
            if (languagesList.Count == 0)
                throw new ArgumentException("Select at least one language.");

            List<Dictionary<string, string>> timelineList;
            try 
            {
                timelineList = string.IsNullOrWhiteSpace(dto.TimelineJson)
                    ? []
                    : JsonSerializer.Deserialize<List<Dictionary<string, string>>>(dto.TimelineJson) ?? [];
            }
            catch (JsonException)
            {
                throw new ArgumentException("The itinerary format is invalid.");
            }
            if (timelineList.Count == 0 || timelineList.Any(item =>
                    !item.TryGetValue("time", out var time) || string.IsNullOrWhiteSpace(time) ||
                    !item.TryGetValue("activity", out var activity) || string.IsNullOrWhiteSpace(activity)))
                throw new ArgumentException("Add at least one complete itinerary item before publishing.");

            var entity = new ExperiencePackageEntity
            {
                GuideProfileId = guideProfileId,
                Title = dto.Title,
                City = dto.City,
                MeetingPoint = dto.MeetingPoint,
                Description = dto.Description,
                DurationHours = dto.DurationHours,
                PricePerSession = dto.PricePerSession,
                PricePerPerson = dto.PricePerGuest,
                IncludedGuestCount = dto.IncludedGuestCount,
                MaxGroupSize = dto.MaxGroupSize,
                IncludedItems = includedServicesList,
                Languages = languagesList,
                Tags = tagsList,
                TimelineJson = timelineList,
                CoverImageUrl = coverUrl,
                GalleryImageUrls = galleryUrls,
                IsActive = true,
                PublicationStatus = "published",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

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

        public async Task<ExperiencePackageEntity?> GetPackageByIdAsync(string id, string guideProfileId)
        {
            return await _repository.GetPackageByIdAsync(id, guideProfileId);
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
                var quality = EvaluateListingQuality(entity);
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
            entity.DurationHours = dto.DurationHours > 0 ? dto.DurationHours : 4;
            entity.PricePerSession = Math.Max(0, dto.PricePerSession);
            entity.PricePerPerson = Math.Max(0, dto.PricePerGuest);
            entity.IncludedGuestCount = Math.Clamp(dto.IncludedGuestCount, 1, 50);
            entity.MaxGroupSize = Math.Clamp(Math.Max(dto.MaxGroupSize, entity.IncludedGuestCount), 1, 50);
            entity.IncludedItems = dto.IncludedServices?.Where(value => !string.IsNullOrWhiteSpace(value)).Distinct().ToList() ?? [];
            entity.Languages = dto.Languages?.Where(value => !string.IsNullOrWhiteSpace(value)).Distinct().ToList() ?? [];
            entity.Tags = dto.Tags?.Where(value => !string.IsNullOrWhiteSpace(value)).Distinct().Take(5).ToList() ?? [];
            entity.TimelineJson = dto.Timeline ?? [];
            entity.IsActive = false;
            entity.PublicationStatus = "draft";
            entity.UpdatedAt = DateTime.UtcNow;

            return existing == null
                ? await _repository.CreatePackageAsync(entity)
                : await _repository.UpdatePackageAsync(entity);
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

        private static (int Score, List<string> MissingItems) EvaluateListingQuality(ExperiencePackageEntity entity)
        {
            var checks = new (bool Complete, string MissingMessage)[]
            {
                (entity.Title.Trim().Length >= 5, "Add a clear tour name"),
                (!string.IsNullOrWhiteSpace(entity.City), "Choose a destination"),
                (!string.IsNullOrWhiteSpace(entity.MeetingPoint), "Add a specific meeting point"),
                (entity.Description.Trim().Length >= 20, "Write a more useful tour description"),
                (entity.DurationHours >= 0.5m, "Set a valid duration"),
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
