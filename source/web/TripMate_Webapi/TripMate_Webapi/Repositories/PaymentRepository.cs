using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Supabase;
using TripMate_Webapi.Entities;

namespace TripMate_Webapi.Repositories;

public sealed class PaymentRepository : IPaymentRepository
{
    private readonly Client _supabase;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _supabaseUrl;
    private readonly string _serviceKey;
    public PaymentRepository(Client supabase, IHttpClientFactory httpClientFactory, IConfiguration config)
    {
        _supabase = supabase;
        _httpClientFactory = httpClientFactory;
        _supabaseUrl = config["Supabase:Url"]
            ?? Environment.GetEnvironmentVariable("SUPABASE_URL")
            ?? throw new InvalidOperationException("Supabase URL is not configured.");
        _serviceKey = config["Supabase:ServiceRoleKey"]
            ?? Environment.GetEnvironmentVariable("SUPABASE_SERVICE_ROLE_KEY")
            ?? throw new InvalidOperationException("Supabase service role key is not configured.");
    }

    public async Task<PaymentEntity> CreateAsync(PaymentEntity payment)
    {
        using var request = BuildRequest(HttpMethod.Post, $"{_supabaseUrl}/rest/v1/payments");
        request.Headers.Add("Prefer", "return=minimal");
        request.Content = new StringContent(
            JsonSerializer.Serialize(new
            {
                id = payment.Id,
                booking_id = payment.BookingId,
                payer_id = payment.PayerId,
                amount = payment.Amount,
                currency = payment.Currency,
                payment_method = payment.PaymentMethod,
                installment_type = payment.InstallmentType,
                status = payment.Status,
                provider_order_code = payment.ProviderOrderCode,
                payment_intent = payment.PaymentIntent,
                expires_at = payment.ExpiresAt,
                idempotency_key = payment.IdempotencyKey,
                metadata = payment.Metadata
            }),
            Encoding.UTF8,
            "application/json");

        var client = _httpClientFactory.CreateClient();
        using var response = await client.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"The payment attempt could not be persisted: {content}");
        }

        return payment;
    }

    public async Task UpdateCheckoutAsync(string paymentId, string checkoutUrl)
    {
        await PatchAsync(paymentId, new
        {
            checkout_url = checkoutUrl,
            updated_at = DateTime.UtcNow.ToString("O")
        });
    }

    public async Task MarkFailedAsync(string paymentId, string reason)
    {
        await PatchAsync(paymentId, new
        {
            status = "failed",
            failure_reason = reason.Length > 500 ? reason[..500] : reason,
            processed_at = DateTime.UtcNow.ToString("O"),
            updated_at = DateTime.UtcNow.ToString("O")
        });
    }

    public async Task<PaymentEntity?> GetByOrderCodeAsync(string orderCode)
    {
        return await _supabase.From<PaymentEntity>()
            .Where(p => p.ProviderOrderCode == orderCode)
            .Single();
    }

    public async Task<PaymentEntity?> GetLatestForBookingAsync(string bookingId, string payerId)
    {
        var response = await _supabase.From<PaymentEntity>()
            .Where(p => p.BookingId == bookingId && p.PayerId == payerId)
            .Order(p => p.CreatedAt, Postgrest.Constants.Ordering.Descending)
            .Limit(1)
            .Get();
        return response.Models.FirstOrDefault();
    }

    public async Task<PaymentWebhookResult> ProcessVerifiedPayOSWebhookAsync(
        string orderCode,
        decimal amount,
        string currency,
        string providerTransactionId,
        DateTime paidAtUtc,
        object payload)
    {
        using var request = BuildRequest(
            HttpMethod.Post,
            $"{_supabaseUrl}/rest/v1/rpc/process_payos_payment_webhook");
        request.Content = new StringContent(
            JsonSerializer.Serialize(new
            {
                p_order_code = orderCode,
                p_amount = amount,
                p_currency = currency,
                p_transaction_id = providerTransactionId,
                p_paid_at = paidAtUtc.ToString("O"),
                p_payload = payload
            }),
            Encoding.UTF8,
            "application/json");

        var client = _httpClientFactory.CreateClient();
        using var response = await client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Payment webhook transaction failed: {content}");

        using var document = JsonDocument.Parse(content);
        var root = document.RootElement;
        return new PaymentWebhookResult(
            KnownPayment: GetBoolean(root, "known_payment"),
            Processed: GetBoolean(root, "processed"),
            AlreadyProcessed: GetBoolean(root, "already_processed"),
            Reason: GetString(root, "reason"),
            BookingId: GetString(root, "booking_id"),
            PayerId: GetString(root, "payer_id"),
            GuideProfileId: GetString(root, "guide_profile_id"),
            InstallmentType: GetString(root, "installment_type"),
            Amount: GetDecimal(root, "amount"),
            BookingStatus: GetInt32(root, "booking_status"));
    }

    private async Task PatchAsync(string paymentId, object body)
    {
        using var request = BuildRequest(
            HttpMethod.Patch,
            $"{_supabaseUrl}/rest/v1/payments?id=eq.{Uri.EscapeDataString(paymentId)}");
        request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        var client = _httpClientFactory.CreateClient();
        using var response = await client.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Failed to update payment attempt: {content}");
        }
    }

    private HttpRequestMessage BuildRequest(HttpMethod method, string url)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add("apikey", _serviceKey);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _serviceKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return request;
    }

    private static bool GetBoolean(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.True;

    private static string? GetString(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind != JsonValueKind.Null
            ? value.ToString()
            : null;

    private static decimal GetDecimal(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.TryGetDecimal(out var result) ? result : 0;

    private static int? GetInt32(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.TryGetInt32(out var result) ? result : null;
}
