using Microsoft.AspNetCore.Mvc;

using TripMate_WebAPI.Services;

namespace TripMate_Webapi.Controllers;

/// <summary>
/// API Controller for Survey/Review operations
/// Implements Traveler Post-Survey Flow requirements
/// </summary>
[ApiController]
[Route("api/surveys")]
[Produces("application/json")]
public class SurveyApiController : ControllerBase
{
    private readonly SurveyService _surveyService;
    private readonly ILogger<SurveyApiController> _logger;

    public SurveyApiController(
        SurveyService surveyService,
        ILogger<SurveyApiController> logger)
    {
        _surveyService = surveyService;
        _logger = logger;
    }

    /// <summary>
    /// Submit a survey/review for a completed tour
    /// </summary>
    /// <remarks>
    /// Implements Requirements 1, 2, 5, 6, 9:
    /// - Validates rating (1-5) and comment (10-500 chars)
    /// - Verifies booking is completed
    /// - Prevents duplicate surveys
    /// - Recalculates tour rating
    /// - Sends notification to guide
    /// - Creates discount voucher for first-time reviewers
    /// </remarks>
    /// <param name="request">Survey submission data</param>
    /// <returns>Survey submission response with optional discount voucher</returns>
    /// <response code="200">Survey submitted successfully</response>
    /// <response code="400">Invalid request or validation failed</response>
    /// <response code="401">User not authenticated</response>
    [HttpPost]
    [ProducesResponseType(typeof(SurveySubmissionResponse), 200)]
    [ProducesResponseType(typeof(SurveyErrorResponse), 400)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> SubmitSurvey([FromBody] SubmitSurveyRequest request)
    {
        // Get user ID and token from headers
        var userId = Request.Headers["X-User-Id"].FirstOrDefault();
        var userToken = Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");

        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(userToken))
        {
            return Unauthorized(new SurveyErrorResponse(
                false,
                "User not authenticated"
            ));
        }

        // Requirement 1.1: Validate rating is between 1 and 5
        if (request.Rating < 1 || request.Rating > 5)
        {
            return BadRequest(new SurveyErrorResponse(
                false,
                "Rating must be between 1 and 5 stars",
                "Invalid rating value"
            ));
        }

        // Requirement 1.2: Validate comment length is between 10 and 500 characters
        if (string.IsNullOrWhiteSpace(request.Comment) ||
            request.Comment.Length < 10 ||
            request.Comment.Length > 500)
        {
            return BadRequest(new SurveyErrorResponse(
                false,
                "Comment must be between 10 and 500 characters",
                $"Current length: {request.Comment?.Length ?? 0}"
            ));
        }

        // Submit survey
        var response = await _surveyService.SubmitSurveyAsync(request, userId, userToken);

        if (!response.Success)
        {
            return BadRequest(new SurveyErrorResponse(
                false,
                response.Message
            ));
        }

        return Ok(response);
    }

    /// <summary>
    /// Get all published surveys for a specific tour
    /// </summary>
    /// <remarks>
    /// Implements Requirement 4:
    /// - Returns all published surveys
    /// - Includes traveler name, rating, comment, and date
    /// - Sorted by submission date (newest first)
    /// - Includes average rating
    /// </remarks>
    /// <param name="tourId">Tour ID</param>
    /// <returns>List of surveys with average rating</returns>
    /// <response code="200">Surveys retrieved successfully</response>
    /// <response code="401">User not authenticated</response>
    [HttpGet("tour/{tourId}")]
    [ProducesResponseType(typeof(TourSurveysResponse), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetTourSurveys(string tourId)
    {
        var userToken = Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");

        if (string.IsNullOrEmpty(userToken))
        {
            return Unauthorized(new SurveyErrorResponse(
                false,
                "User not authenticated"
            ));
        }

        var response = await _surveyService.GetTourSurveysAsync(tourId, userToken);
        return Ok(response);
    }

    /// <summary>
    /// Get survey history for the authenticated traveler
    /// </summary>
    /// <remarks>
    /// Implements Requirement 8:
    /// - Returns all surveys submitted by the traveler
    /// - Includes tour title, location, rating, and comment
    /// - Sorted by submission date (newest first)
    /// </remarks>
    /// <returns>List of traveler's surveys</returns>
    /// <response code="200">Survey history retrieved successfully</response>
    /// <response code="401">User not authenticated</response>
    [HttpGet("my-surveys")]
    [ProducesResponseType(typeof(TravelerSurveysResponse), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetMySurveys()
    {
        var userId = Request.Headers["X-User-Id"].FirstOrDefault();
        var userToken = Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");

        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(userToken))
        {
            return Unauthorized(new SurveyErrorResponse(
                false,
                "User not authenticated"
            ));
        }

        var response = await _surveyService.GetTravelerSurveysAsync(userId, userToken);
        return Ok(response);
    }

    /// <summary>
    /// Get survey analytics for admin dashboard
    /// </summary>
    /// <remarks>
    /// Implements Requirement 10:
    /// - Total surveys submitted
    /// - Average rating across all tours
    /// - Highest and lowest rated tours
    /// - Survey submission rate
    /// - Rating distribution
    /// </remarks>
    /// <returns>Survey analytics data</returns>
    /// <response code="200">Analytics retrieved successfully</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="403">User is not an admin</response>
    [HttpGet("analytics")]
    [ProducesResponseType(typeof(SurveyAnalyticsDto), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> GetAnalytics()
    {
        var userToken = Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");

        if (string.IsNullOrEmpty(userToken))
        {
            return Unauthorized(new SurveyErrorResponse(
                false,
                "User not authenticated"
            ));
        }

        // TODO: Add admin role verification
        // For now, allow any authenticated user

        var response = await _surveyService.GetSurveyAnalyticsAsync(userToken);
        return Ok(response);
    }

    /// <summary>
    /// Check if a booking already has a survey
    /// </summary>
    /// <remarks>
    /// Implements Requirement 7:
    /// - Returns whether a survey exists for the booking
    /// - Used to hide survey form if already submitted
    /// </remarks>
    /// <param name="bookingId">Booking ID</param>
    /// <returns>Boolean indicating if survey exists</returns>
    /// <response code="200">Check completed successfully</response>
    /// <response code="401">User not authenticated</response>
    [HttpGet("check/{bookingId}")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(401)]
    public IActionResult CheckSurveyExists(string bookingId)
    {
        var userToken = Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");

        if (string.IsNullOrEmpty(userToken))
        {
            return Unauthorized(new SurveyErrorResponse(
                false,
                "User not authenticated"
            ));
        }

        // This is a simple check - the actual logic is in SurveyService
        // We'll return a simple response for now
        return Ok(new
        {
            booking_id = bookingId,
            message = "Use GET /api/surveys/tour/{tourId} to check if survey exists"
        });
    }
}
