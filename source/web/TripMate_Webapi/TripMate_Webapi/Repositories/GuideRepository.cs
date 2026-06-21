using Supabase;
using TripMate_Webapi.Entities;

namespace TripMate_Webapi.Repositories
{
    public class GuideRepository : IGuideRepository
    {
        private readonly Client _supabase;

        public GuideRepository(Client supabase)
        {
            _supabase = supabase;
        }

        public async Task<List<GuideProfileEntity>> GetAllGuidesAsync()
        {
            var response = await _supabase.From<GuideProfileEntity>()
                .Get();
            
            return response.Models;
        }

        public async Task<List<GuideProfileEntity>> GetGuidesByDestinationAsync(string destination)
        {
            var response = await _supabase.From<GuideProfileEntity>()
                .Filter("city_area", Postgrest.Constants.Operator.ILike, $"%{destination}%")
                .Get();
                
            return response.Models;
        }

        public async Task<List<GuideProfileEntity>> GetGuidesFilteredAsync(string? destination, string? specialty)
        {
            List<GuideProfileEntity> guides;
            
            if (!string.IsNullOrEmpty(destination))
            {
                var response = await _supabase.From<GuideProfileEntity>()
                    .Filter("city_area", Postgrest.Constants.Operator.ILike, $"%{destination}%")
                    .Get();
                guides = response.Models;
            }
            else
            {
                var response = await _supabase.From<GuideProfileEntity>().Get();
                guides = response.Models;
            }
            
            
            if (!string.IsNullOrEmpty(specialty) && specialty != "All")
            {
                // Specialties is a List<string> mapped to jsonb or similar. Filtering client-side for simplicity since it's a mock/small DB
                guides = guides.Where(g => g.Specialties != null && g.Specialties.Any(s => s.Contains(specialty, StringComparison.OrdinalIgnoreCase))).ToList();
            }
            
            return guides;
        }

        public async Task<GuideProfileEntity> GetGuideByIdAsync(string id)
        {
            var response = await _supabase.From<GuideProfileEntity>()
                .Where(x => x.UserId == id)
                .Single();
                
            return response!;
        }
    }
}
