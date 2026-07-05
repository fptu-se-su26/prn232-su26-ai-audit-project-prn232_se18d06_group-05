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
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IExperiencePackageRepository _repository;

        public ExperienceService(ICloudinaryService cloudinaryService, IExperiencePackageRepository repository)
        {
            _cloudinaryService = cloudinaryService;
            _repository = repository;
        }

        public async Task<ExperiencePackageEntity> CreateTourAsync(CreateTourDto dto, string guideProfileId)
        {
            // 1. Upload Cover Image to Cloudinary
            string coverUrl = string.Empty;
            if (dto.CoverImage != null)
            {
                coverUrl = await _cloudinaryService.UploadImageAsync(dto.CoverImage) ?? string.Empty;
            }

            // 2. Upload Gallery Images
            var galleryUrls = new List<string>();
            if (dto.GalleryImages != null && dto.GalleryImages.Count > 0)
            {
                galleryUrls = await _cloudinaryService.UploadImagesAsync(dto.GalleryImages);
            }

            // 3. Deserialize JSON strings to Lists
            var includedServicesList = DeserializeJsonArray(dto.IncludedServices);
            var languagesList = DeserializeJsonArray(dto.Languages);
            var tagsList = DeserializeJsonArray(dto.Tags);

            // 4. Map DTO to Entity
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
                MaxGroupSize = dto.MaxGroupSize,
                IncludedItems = includedServicesList,
                Languages = languagesList,
                Tags = tagsList,
                TimelineJson = dto.TimelineJson,
                CoverImageUrl = coverUrl,
                GalleryImageUrls = galleryUrls,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            // 5. Save to Database
            return await _repository.CreatePackageAsync(entity);
        }

        public async Task<List<MyTourDashboardDto>> GetMyToursAsync(string guideProfileId)
        {
            var entities = await _repository.GetPackagesByGuideIdAsync(guideProfileId);
            var dtos = new List<MyTourDashboardDto>();

            foreach (var entity in entities)
            {
                dtos.Add(new MyTourDashboardDto
                {
                    Id = entity.Id,
                    Name = entity.Title,
                    Duration = entity.DurationHours,
                    MaxGuests = entity.MaxGroupSize,
                    Price = entity.PricePerSession, // or PricePerPerson based on your business logic
                    Tags = entity.Tags ?? new List<string>(),
                    Bookings = 0, // Mock: Query actual bookings if needed later
                    Rating = 0.0m, // Mock: Query actual reviews later
                    IsActive = entity.IsActive,
                    ImageUrl = entity.CoverImageUrl ?? ""
                });
            }

            return dtos;
        }

        public async Task<bool> ToggleTourStatusAsync(string tourId, string guideProfileId)
        {
            return await _repository.TogglePackageStatusAsync(tourId, guideProfileId);
        }

        public async Task<bool> DeleteTourAsync(string tourId, string guideProfileId)
        {
            // Note: You might want to delete images from Cloudinary here as well
            // using _cloudinaryService.DeleteImageAsync(publicId) before deleting the record
            return await _repository.DeletePackageAsync(tourId, guideProfileId);
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
    }
}
