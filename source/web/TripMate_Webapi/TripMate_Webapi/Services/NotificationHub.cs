using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace TripMate_WebAPI.Services;

[Authorize]
public sealed class NotificationHub : Hub
{
}
