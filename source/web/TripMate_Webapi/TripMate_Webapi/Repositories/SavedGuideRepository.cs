using Supabase;
using TripMate_Webapi.Entities;

namespace TripMate_Webapi.Repositories
{
    public class SavedGuideRepository : ISavedGuideRepository
    {
        private readonly Client _supabase;

        public SavedGuideRepository(Client supabase)
        {
            _supabase = supabase;
        }

        public async Task<SavedGuideEntity> SaveGuideAsync(SavedGuideEntity savedGuide)
        {
            var response = await _supabase.From<SavedGuideEntity>().Insert(savedGuide);
            return response.Models.FirstOrDefault() ?? savedGuide;
        }

        public async Task DeleteSavedGuideAsync(string travelerId, string guideProfileId)
        {
            await _supabase.From<SavedGuideEntity>()
                .Where(x => x.TravelerId == travelerId && x.GuideProfileId == guideProfileId)
                .Delete();
        }

        public async Task<List<SavedGuideEntity>> GetSavedGuidesByTravelerAsync(string travelerId)
        {
            var response = await _supabase.From<SavedGuideEntity>()
                .Where(x => x.TravelerId == travelerId)
                .Order("created_at", Postgrest.Constants.Ordering.Descending)
                .Get();
                
            return response.Models;
        }

        public async Task<bool> IsGuideSavedAsync(string travelerId, string guideProfileId)
        {
            var response = await _supabase.From<SavedGuideEntity>()
                .Where(x => x.TravelerId == travelerId && x.GuideProfileId == guideProfileId)
                .Get();
            return response.Models.Any();
        }
    }
}
