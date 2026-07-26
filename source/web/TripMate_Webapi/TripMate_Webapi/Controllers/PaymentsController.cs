using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PayOS.Models.Webhooks;
using TripMate_WebAPI.Services;

namespace TripMate_WebAPI.Controllers;

[ApiController]
[Route("api/payments")]
public sealed class PaymentsController : ControllerBase
{
    private readonly IPaymentService _payments;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(IPaymentService payments, ILogger<PaymentsController> logger)
    {
        _payments = payments;
        _logger = logger;
    }

    [AllowAnonymous]
    [HttpPost("payos/webhook")]
    public async Task<IActionResult> PayOSWebhook([FromBody] Webhook webhook)
    {
        try
        {
            var result = await _payments.HandlePayOSWebhookAsync(webhook);

            // PayOS sends a signed validation event while registering a webhook.
            // A valid but unknown order must receive 200 and must not mutate data.
            return Ok(new
            {
                success = true,
                processed = result.Processed,
                alreadyProcessed = result.AlreadyProcessed,
                knownPayment = result.KnownPayment
            });
        }
        catch (PayOS.Exceptions.WebhookException ex)
        {
            _logger.LogWarning(ex, "Rejected PayOS webhook with an invalid signature.");
            return BadRequest(new { success = false, message = "Invalid webhook signature." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process verified PayOS webhook.");
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                success = false,
                message = "Webhook processing failed."
            });
        }
    }
}
