namespace TripMate_WebAPI.DTOs.Survey;

/// <summary>
/// Discount voucher given to first-time survey submitters
/// </summary>
public record DiscountVoucherDto(
    string Code,
    int DiscountPercent,
    DateTime ExpiresAt
);
