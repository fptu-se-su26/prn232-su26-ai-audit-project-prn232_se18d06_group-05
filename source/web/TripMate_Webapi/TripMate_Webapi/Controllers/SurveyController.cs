using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using TripMate_WebAPI.DTOs.Matching;
using TripMate_WebAPI.Services;

namespace TripMate_Webapi.Controllers;

/// <summary>
/// Hosts the trip-preference survey and delegates matching rules to the matching service.
/// </summary>
public class SurveyController : Controller
{
    private const string PendingQuizKey = "PendingQuiz";
    private const string GhostBookingKey = "GhostBooking";

    private readonly ILogger<SurveyController> _logger;
    private readonly IMatchingService _matchingService;

    public SurveyController(
        ILogger<SurveyController> logger,
        IMatchingService matchingService)
    {
        _logger = logger;
        _matchingService = matchingService;
    }

    public IActionResult Index() => View();

    public IActionResult Personality() => View();

    public IActionResult Results() => View();

    public IActionResult Matches()
    {
        var quizJson = HttpContext.Session.GetString(PendingQuizKey);
        if (!string.IsNullOrEmpty(quizJson))
            ViewBag.PendingQuizJson = quizJson;

        return View();
    }

    [HttpPost]
    public IActionResult SavePreferences([FromBody] JsonElement payload)
    {
        try
        {
            HttpContext.Session.SetString(PendingQuizKey, payload.GetRawText());
            _logger.LogInformation("Saved trip preferences to the server session");
            return Ok(new { success = true, message = "Trip preferences saved." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save trip preferences");
            return StatusCode(500, new { success = false, message = "Could not save preferences." });
        }
    }

    [HttpGet]
    public IActionResult GetPendingQuiz()
    {
        var quizJson = HttpContext.Session.GetString(PendingQuizKey);
        if (string.IsNullOrEmpty(quizJson))
            return Ok(new { found = false });

        return Ok(new
        {
            found = true,
            data = JsonSerializer.Deserialize<JsonElement>(quizJson)
        });
    }

    [HttpPost]
    public IActionResult HoldBooking([FromBody] JsonElement payload)
    {
        try
        {
            HttpContext.Session.SetString(GhostBookingKey, payload.GetRawText());
            _logger.LogInformation("Saved booking intent to the server session");
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save booking intent");
            return StatusCode(500, new { success = false });
        }
    }

    /// <summary>
    /// Returns deterministic, explainable tour matches using real tour pricing,
    /// capacity, Guide availability and confirmed-booking conflicts.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CalculateMatches(
        [FromBody] MatchingRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return BadRequest(new { success = false, message = "Matching preferences are required." });

        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        try
        {
            var result = await _matchingService.FindMatchesAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Smart matching failed");
            return StatusCode(500, new
            {
                success = false,
                message = "Could not calculate matches. Please try again."
            });
        }
    }

    [HttpPost]
    public IActionResult ClearSession()
    {
        HttpContext.Session.Remove(PendingQuizKey);
        HttpContext.Session.Remove(GhostBookingKey);
        return Ok(new { success = true });
    }
}
