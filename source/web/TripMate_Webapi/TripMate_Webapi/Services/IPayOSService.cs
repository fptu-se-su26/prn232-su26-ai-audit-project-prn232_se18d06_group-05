namespace TripMate_WebAPI.Services
{
    using System.Threading.Tasks;
    using PayOS.Models.V2.PaymentRequests;
    using PayOS.Models.Webhooks;
    using TripMate_Webapi.Entities;

    public interface IPayOSService
    {
        Task<string> CreatePaymentLink(BookingEntity booking, PaymentEntity payment, long orderCode, int amount);
        Task<PaymentLink> GetPaymentLinkAsync(string orderCode);
        Task<WebhookData> VerifyWebhookAsync(Webhook webhook);
    }
}
