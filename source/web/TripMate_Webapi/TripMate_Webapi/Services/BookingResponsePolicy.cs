namespace TripMate_WebAPI.Services;

public static class BookingResponsePolicy
{
    public const int PendingStatus = 0;
    public static readonly TimeSpan ResponseWindow = TimeSpan.FromHours(24);

    public static DateTime GetDeadlineUtc(DateTime createdAt)
        => AsUtc(createdAt).Add(ResponseWindow);

    public static bool IsExpired(int status, DateTime createdAt, DateTime utcNow)
        => status == PendingStatus && AsUtc(utcNow) >= GetDeadlineUtc(createdAt);

    public static bool IsAwaitingGuideResponse(int status, DateTime createdAt, DateTime utcNow)
        => status == PendingStatus && AsUtc(utcNow) < GetDeadlineUtc(createdAt);

    public static DateTime AsUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }
}
