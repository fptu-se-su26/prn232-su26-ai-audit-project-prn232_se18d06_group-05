namespace TripMate_WebAPI.Services;

public static class TourPricingCalculator
{
    public static decimal CalculateTotal(
        decimal baseTourPrice,
        decimal? additionalGuestFee,
        int includedGuestCount,
        int guestCount)
    {
        if (guestCount < 1)
            throw new ArgumentOutOfRangeException(nameof(guestCount), "Guest count must be at least one.");

        var includedGuests = Math.Max(1, includedGuestCount);
        var extraGuests = Math.Max(0, guestCount - includedGuests);
        var extraFee = Math.Max(0, additionalGuestFee ?? 0);
        return baseTourPrice + extraGuests * extraFee;
    }
}
