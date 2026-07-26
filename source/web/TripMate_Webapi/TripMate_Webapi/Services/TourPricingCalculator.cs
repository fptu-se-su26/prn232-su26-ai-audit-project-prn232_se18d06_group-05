namespace TripMate_WebAPI.Services;

public static class TourPricingCalculator
{
    public const decimal PlatformFeeRate = 0.15m;
    public const decimal DepositRate = 0.30m;

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

    public static BookingPriceBreakdown CalculateTourPrice(
        decimal baseTourPrice,
        decimal? additionalGuestFee,
        int includedGuestCount,
        int guestCount)
    {
        var total = CalculateTotal(
            baseTourPrice,
            additionalGuestFee,
            includedGuestCount,
            guestCount);

        return FromAgreedTotal(total);
    }

    public static BookingPriceBreakdown FromAgreedTotal(decimal totalAmount)
    {
        if (totalAmount <= 0)
            throw new ArgumentOutOfRangeException(nameof(totalAmount), "The booking total must be greater than zero.");

        var total = Math.Round(totalAmount, 0, MidpointRounding.AwayFromZero);
        var platformFee = Math.Round(total * PlatformFeeRate, 0, MidpointRounding.AwayFromZero);
        var deposit = Math.Round(total * DepositRate, 0, MidpointRounding.AwayFromZero);

        return new BookingPriceBreakdown(
            TotalAmount: total,
            PlatformFee: platformFee,
            GuideEarnings: total - platformFee,
            DepositAmount: deposit,
            BalanceAmount: total - deposit);
    }
}

public sealed record BookingPriceBreakdown(
    decimal TotalAmount,
    decimal PlatformFee,
    decimal GuideEarnings,
    decimal DepositAmount,
    decimal BalanceAmount);
