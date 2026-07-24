using Supabase;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TripMate_Webapi.Entities;

namespace TripMate_Webapi.Repositories
{
    public class ExperiencePackageRepository : IExperiencePackageRepository
    {
        private readonly Client _supabase;

        public ExperiencePackageRepository(Client supabase)
        {
            _supabase = supabase;
        }

        public async Task<ExperiencePackageEntity> CreatePackageAsync(ExperiencePackageEntity entity)
        {
            var response = await _supabase.From<ExperiencePackageEntity>().Insert(entity);
            return response.Models.FirstOrDefault() ?? entity;
        }

        public async Task<ExperiencePackageEntity> UpdatePackageAsync(ExperiencePackageEntity entity)
        {
            var response = await _supabase.From<ExperiencePackageEntity>()
                .Where(e => e.Id == entity.Id && e.GuideProfileId == entity.GuideProfileId)
                .Update(entity);
            return response.Models.FirstOrDefault() ?? entity;
        }

        public async Task<ExperiencePackageEntity?> GetPackageByIdAsync(string id, string guideId)
        {
            var response = await _supabase.From<ExperiencePackageEntity>()
                .Where(e => e.Id == id && e.GuideProfileId == guideId)
                .Single();
            return response;
        }

        public async Task<List<ExperiencePackageEntity>> GetPackagesByGuideIdAsync(string guideId)
        {
            var response = await _supabase.From<ExperiencePackageEntity>()
                .Where(e => e.GuideProfileId == guideId)
                .Get();
            
            // For now, sorting descending by CreatedAt locally if Supabase order isn't applied
            return response.Models.OrderByDescending(e => e.CreatedAt).ToList();
        }

        public async Task<bool> TogglePackageStatusAsync(string id, string guideId)
        {
            var response = await _supabase.From<ExperiencePackageEntity>()
                .Where(e => e.Id == id && e.GuideProfileId == guideId)
                .Single();
                
            if (response == null) return false;

            response.IsActive = !response.IsActive;
            await _supabase.From<ExperiencePackageEntity>().Where(e => e.Id == response.Id).Update(response);
            return true;
        }

        public async Task<bool> DeletePackageAsync(string id, string guideId)
        {
            await _supabase.From<ExperiencePackageEntity>()
                .Where(e => e.Id == id && e.GuideProfileId == guideId)
                .Delete();
            return true;
        }
    }
}
