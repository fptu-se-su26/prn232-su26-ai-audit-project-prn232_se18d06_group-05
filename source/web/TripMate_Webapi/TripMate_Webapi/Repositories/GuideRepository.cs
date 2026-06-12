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

        public async Task<GuideProfileEntity> GetGuideByIdAsync(string id)
        {
            var response = await _supabase.From<GuideProfileEntity>()
                .Where(x => x.UserId == id)
                .Single();
                
            return response;
        }
    }
}
