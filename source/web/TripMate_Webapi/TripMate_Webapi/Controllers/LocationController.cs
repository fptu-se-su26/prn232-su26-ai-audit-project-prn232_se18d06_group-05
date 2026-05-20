using Microsoft.AspNetCore.Mvc;
using TripMate_WebAPI.Services;

namespace TripMate_WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LocationController : ControllerBase
{
    private readonly LocationService _locationService;
    public LocationController(LocationService locationService)
        => _locationService = locationService;

    /// <summary>Tìm địa điểm xung quanh bằng SerpAPI Google Local</summary>
    [HttpGet("nearby")]
    public async Task<IActionResult> SearchNearby(
        [FromQuery] string q,
        [FromQuery] string location,
        [FromQuery] string hl = "vi",
        [FromQuery] string gl = "vn")
    {
        if (string.IsNullOrWhiteSpace(q) || string.IsNullOrWhiteSpace(location))
            return BadRequest(new { message = "q và location là bắt buộc" });

        try
        {
            var results = await _locationService.SearchNearbyAsync(q, location, hl, gl);
            return Ok(new { results, total = results.Count });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }
}
