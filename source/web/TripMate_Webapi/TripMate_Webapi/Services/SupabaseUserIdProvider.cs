using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace TripMate_WebAPI.Services;

public sealed class SupabaseUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        return connection.User?.FindFirst("sub")?.Value
            ?? connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }
}
