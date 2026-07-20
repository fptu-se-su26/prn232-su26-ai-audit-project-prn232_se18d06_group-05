using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;

namespace TripMate_WebAPI.Services
{
    public interface IEmailService
    {
        Task SendGuideApprovalEmailAsync(string toEmail, string fullName, bool isApproved, string adminMessage, string verificationLink);
        Task SendAdminNotificationEmailAsync(string adminEmail, string adminName, string guideName, string guideEmail);
        Task SendPasswordResetEmailAsync(string toEmail, string resetLink);
        Task SendNotificationEmailAsync(string toEmail, string fullName, string title, string message, string? actionUrl = null);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task SendGuideApprovalEmailAsync(string toEmail, string fullName, bool isApproved, string adminMessage, string verificationLink)
        {
            try
            {
                var smtpHost = _config["EmailSettings:SmtpHost"] ?? "smtp.gmail.com";
                var smtpPort = int.Parse(_config["EmailSettings:SmtpPort"] ?? "587");
                var smtpUser = _config["EmailSettings:SmtpUser"];
                var smtpPass = _config["EmailSettings:SmtpPass"];

                if (string.IsNullOrEmpty(smtpUser) || string.IsNullOrEmpty(smtpPass))
                {
                    _logger.LogWarning("EmailSettings not configured properly in appsettings.json.");
                    return;
                }

                var email = new MimeMessage();
                email.From.Add(new MailboxAddress("TripMate Admin", smtpUser));
                email.To.Add(new MailboxAddress(fullName, toEmail));

                // Add CC to admin so they have a record of the approval/rejection
                email.Cc.Add(new MailboxAddress("TripMate System Admin", smtpUser));

                string statusText = isApproved ? "CHẤP THUẬN" : "TỪ CHỐI";
                email.Subject = $"[TripMate] Kết quả xét duyệt tài khoản Hướng dẫn viên - {statusText}";

                string statusColor = isApproved ? "#4CAF50" : "#F44336";
                string statusIcon = isApproved ? "✅" : "❌";

                // Build the verification link section - only for approved guides
                string verificationSection = "";
                if (isApproved && !string.IsNullOrEmpty(verificationLink))
                {
                    verificationSection = $@"
                        <div style='background:#f0fdf4; border:1px solid #bbf7d0; border-radius:10px; padding:20px; margin:24px 0;'>
                            <p style='color:#166534; font-size:14px; font-weight:600; margin:0 0 12px 0;'>🔗 Xác thực tài khoản của bạn</p>
                            <p style='color:#555; font-size:13px; margin:0 0 16px 0;'>
                                Tài khoản của bạn đã được phê duyệt! Nhấp vào nút bên dưới để xác thực tài khoản và bắt đầu sử dụng TripMate với tư cách Hướng dẫn viên.
                            </p>
                            <div style='text-align:center;'>
                                <a href='{verificationLink}' 
                                   style='display:inline-block; background:linear-gradient(135deg,#FF6B35,#FF8C42); color:white; padding:14px 32px; 
                                          text-decoration:none; border-radius:8px; font-weight:700; font-size:15px;'>
                                    Xác thực &amp; Đăng nhập TripMate
                                </a>
                            </div>
                            <p style='color:#888; font-size:11px; margin:12px 0 0 0; text-align:center;'>
                                Link có hiệu lực trong 7 ngày. Nếu không phải bạn yêu cầu, hãy bỏ qua email này.
                            </p>
                        </div>";
                }

                string bodyHtml = $@"
                    <div style='font-family:Arial,sans-serif; max-width:600px; margin:0 auto; background:#ffffff; border-radius:16px; overflow:hidden; box-shadow:0 4px 20px rgba(0,0,0,0.1);'>
                        <!-- Header -->
                        <div style='background:linear-gradient(135deg,#FF6B35,#FF8C42); padding:40px 30px; text-align:center;'>
                            <div style='font-size:48px; margin-bottom:12px;'>🗺️</div>
                            <h1 style='color:white; margin:0; font-size:24px; font-weight:800;'>TripMate</h1>
                            <p style='color:rgba(255,255,255,0.85); margin:4px 0 0 0; font-size:14px;'>Nền tảng kết nối Du khách & Hướng dẫn viên</p>
                        </div>

                        <!-- Body -->
                        <div style='padding:30px;'>
                            <h2 style='color:{statusColor}; font-size:20px; margin:0 0 8px 0;'>{statusIcon} Kết quả xét duyệt: {statusText}</h2>
                            <p style='color:#333; font-size:15px; margin:0 0 20px 0;'>Xin chào <strong>{fullName}</strong>,</p>
                            <p style='color:#555; font-size:14px; line-height:1.6; margin:0 0 20px 0;'>
                                Hồ sơ đăng ký Hướng dẫn viên của bạn đã được Admin TripMate xem xét và kết quả là:
                                <strong style='color:{statusColor};'> {statusText}</strong>.
                            </p>

                            <!-- Admin Message -->
                            <div style='border-left:4px solid #FF6B35; background:#fff8f5; padding:16px; border-radius:0 8px 8px 0; margin:20px 0;'>
                                <p style='color:#FF6B35; font-size:13px; font-weight:700; margin:0 0 8px 0;'>💬 Tin nhắn từ Admin:</p>
                                <p style='color:#555; font-size:14px; line-height:1.6; margin:0;'>
                                    {adminMessage.Replace("\n", "<br/>")}
                                </p>
                            </div>

                            {verificationSection}

                            {(isApproved ? "" : @"
                            <div style='background:#fff5f5; border:1px solid #fecaca; border-radius:10px; padding:16px; margin:20px 0;'>
                                <p style='color:#991b1b; font-size:13px; margin:0;'>
                                    ❓ Nếu bạn muốn khiếu nại hoặc gửi lại hồ sơ, vui lòng liên hệ Admin qua email này.
                                </p>
                            </div>")}
                        </div>

                        <!-- Footer -->
                        <div style='background:#f8f8f8; padding:20px 30px; text-align:center; border-top:1px solid #eee;'>
                            <p style='color:#aaa; font-size:12px; margin:0;'>© 2025 TripMate. Đây là email tự động, vui lòng không reply trực tiếp.</p>
                        </div>
                    </div>";

                email.Body = new TextPart(TextFormat.Html) { Text = bodyHtml };

                using var smtp = new SmtpClient();
                await smtp.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(smtpUser, smtpPass);
                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);

                _logger.LogInformation("Guide {ApprovalStatus} email sent to {Email}", isApproved ? "approval" : "rejection", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending guide approval email to {Email}", toEmail);
            }
        }
        public async Task SendAdminNotificationEmailAsync(string adminEmail, string adminName, string guideName, string guideEmail)
        {
            try
            {
                var smtpHost = _config["EmailSettings:SmtpHost"] ?? "smtp.gmail.com";
                var smtpPort = int.Parse(_config["EmailSettings:SmtpPort"] ?? "587");
                var smtpUser = _config["EmailSettings:SmtpUser"];
                var smtpPass = _config["EmailSettings:SmtpPass"];

                if (string.IsNullOrEmpty(smtpUser) || string.IsNullOrEmpty(smtpPass))
                {
                    _logger.LogWarning("EmailSettings not configured properly in appsettings.json.");
                    return;
                }

                var email = new MimeMessage();
                email.From.Add(new MailboxAddress("TripMate System", smtpUser));
                email.To.Add(new MailboxAddress(adminName, adminEmail));

                email.Subject = "[TripMate Admin] Đơn đăng ký Hướng dẫn viên mới cần xét duyệt";

                string bodyHtml = $@"
                    <div style='font-family:Arial,sans-serif; max-width:600px; margin:0 auto; background:#ffffff; border-radius:16px; overflow:hidden; box-shadow:0 4px 20px rgba(0,0,0,0.1);'>
                        <!-- Header -->
                        <div style='background:linear-gradient(135deg,#FF6B35,#FF8C42); padding:30px; text-align:center;'>
                            <div style='font-size:48px; margin-bottom:12px;'>🔔</div>
                            <h1 style='color:white; margin:0; font-size:22px; font-weight:800;'>TripMate Admin</h1>
                            <p style='color:rgba(255,255,255,0.85); margin:4px 0 0 0; font-size:13px;'>Thông báo xét duyệt hướng dẫn viên</p>
                        </div>

                        <!-- Body -->
                        <div style='padding:30px;'>
                            <h2 style='color:#FF6B35; font-size:18px; margin:0 0 8px 0;'>📝 Đơn đăng ký Hướng dẫn viên mới</h2>
                            <p style='color:#333; font-size:15px; margin:0 0 20px 0;'>Xin chào <strong>{adminName}</strong>,</p>
                            <p style='color:#555; font-size:14px; line-height:1.6; margin:0 0 20px 0;'>
                                Có một đơn đăng ký hướng dẫn viên mới cần được xét duyệt trên hệ thống TripMate:
                            </p>

                            <!-- Guide Info -->
                            <div style='border:1px solid #e0e0e0; border-radius:10px; padding:20px; margin:20px 0; background:#f9f9f9;'>
                                <p style='color:#FF6B35; font-size:14px; font-weight:700; margin:0 0 12px 0;'>👤 Thông tin ứng viên:</p>
                                <p style='color:#333; font-size:14px; margin:0 0 8px 0;'><strong>Họ tên:</strong> {guideName}</p>
                                <p style='color:#333; font-size:14px; margin:0;'><strong>Email:</strong> {guideEmail}</p>
                            </div>

                            <!-- Action Required -->
                            <div style='background:#fff3cd; border:1px solid #ffeaa7; border-radius:10px; padding:16px; margin:20px 0;'>
                                <p style='color:#856404; font-size:13px; font-weight:600; margin:0 0 8px 0;'>⚡ Hành động cần thực hiện:</p>
                                <p style='color:#555; font-size:13px; line-height:1.6; margin:0;'>
                                    Vui lòng đăng nhập vào hệ thống Admin để xem xét và phê duyệt/từ chối đơn đăng ký này.
                                    Ứng viên sẽ không thể sử dụng hệ thống cho đến khi được phê duyệt.
                                </p>
                            </div>

                            <!-- Login Button -->
                            <div style='text-align:center; margin:24px 0;'>
                                <a href='#' 
                                   style='display:inline-block; background:linear-gradient(135deg,#FF6B35,#FF8C42); color:white; padding:12px 30px; 
                                          text-decoration:none; border-radius:8px; font-weight:600; font-size:14px;'>
                                    Đăng nhập Admin Dashboard
                                </a>
                            </div>
                        </div>

                        <!-- Footer -->
                        <div style='background:#f8f8f8; padding:20px 30px; text-align:center; border-top:1px solid #eee;'>
                            <p style='color:#aaa; font-size:12px; margin:0;'>© 2025 TripMate. Email tự động từ hệ thống.</p>
                        </div>
                    </div>";

                email.Body = new TextPart(TextFormat.Html) { Text = bodyHtml };

                using var smtp = new SmtpClient();
                await smtp.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(smtpUser, smtpPass);
                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);

                _logger.LogInformation("Admin notification email sent to {AdminEmail} for guide {GuideEmail}", adminEmail, guideEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending admin notification email to {AdminEmail}", adminEmail);
            }
        }

        public async Task SendPasswordResetEmailAsync(string toEmail, string resetLink)
        {
            try
            {
                var smtpHost = _config["EmailSettings:SmtpHost"] ?? "smtp.gmail.com";
                var smtpPort = int.Parse(_config["EmailSettings:SmtpPort"] ?? "587");
                var smtpUser = _config["EmailSettings:SmtpUser"];
                var smtpPass = _config["EmailSettings:SmtpPass"];

                if (string.IsNullOrEmpty(smtpUser) || string.IsNullOrEmpty(smtpPass))
                {
                    _logger.LogWarning("EmailSettings not configured properly in appsettings.json.");
                    return;
                }

                var email = new MimeMessage();
                email.From.Add(new MailboxAddress("TripMate Support", smtpUser));
                email.To.Add(new MailboxAddress("", toEmail));

                email.Subject = "[TripMate] Đặt lại mật khẩu tài khoản";

                string bodyHtml = $@"
                    <div style='font-family:Arial,sans-serif; max-width:600px; margin:0 auto; background:#ffffff; border-radius:16px; overflow:hidden; box-shadow:0 4px 20px rgba(0,0,0,0.1);'>
                        <!-- Header -->
                        <div style='background:linear-gradient(135deg,#FF6B35,#FF8C42); padding:30px; text-align:center;'>
                            <div style='font-size:48px; margin-bottom:12px;'>🔑</div>
                            <h1 style='color:white; margin:0; font-size:22px; font-weight:800;'>TripMate</h1>
                            <p style='color:rgba(255,255,255,0.85); margin:4px 0 0 0; font-size:13px;'>Đặt lại mật khẩu</p>
                        </div>

                        <!-- Body -->
                        <div style='padding:30px;'>
                            <h2 style='color:#FF6B35; font-size:18px; margin:0 0 8px 0;'>🔐 Yêu cầu đặt lại mật khẩu</h2>
                            <p style='color:#333; font-size:15px; margin:0 0 20px 0;'>Xin chào,</p>
                            <p style='color:#555; font-size:14px; line-height:1.6; margin:0 0 20px 0;'>
                                Chúng tôi đã nhận được yêu cầu đặt lại mật khẩu cho tài khoản TripMate của bạn.
                            </p>

                            <!-- Reset Button -->
                            <div style='text-align:center; margin:24px 0;'>
                                <a href='{resetLink}' 
                                   style='display:inline-block; background:linear-gradient(135deg,#FF6B35,#FF8C42); color:white; padding:14px 32px; 
                                          text-decoration:none; border-radius:8px; font-weight:700; font-size:15px;'>
                                    Đặt lại mật khẩu
                                </a>
                            </div>

                            <!-- Security Note -->
                            <div style='background:#fff3cd; border:1px solid #ffeaa7; border-radius:10px; padding:16px; margin:20px 0;'>
                                <p style='color:#856404; font-size:13px; margin:0;'>
                                    🔒 <strong>Lưu ý bảo mật:</strong> Link này có hiệu lực trong 24 giờ. 
                                    Nếu bạn không yêu cầu đặt lại mật khẩu, vui lòng bỏ qua email này.
                                </p>
                            </div>

                            <!-- Manual Link -->
                            <p style='color:#888; font-size:12px; margin:20px 0 0 0;'>
                                Nếu nút không hoạt động, copy link sau vào trình duyệt:<br>
                                <a href='{resetLink}' style='color:#FF6B35; word-break:break-all;'>{resetLink}</a>
                            </p>
                        </div>

                        <!-- Footer -->
                        <div style='background:#f8f8f8; padding:20px 30px; text-align:center; border-top:1px solid #eee;'>
                            <p style='color:#aaa; font-size:12px; margin:0;'>© 2025 TripMate. Email tự động từ hệ thống.</p>
                        </div>
                    </div>";

                email.Body = new TextPart(TextFormat.Html) { Text = bodyHtml };

                using var smtp = new SmtpClient();
                await smtp.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(smtpUser, smtpPass);
                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);

                _logger.LogInformation("Password reset email sent to {Email}", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending password reset email to {Email}", toEmail);
            }
        }

        public async Task SendNotificationEmailAsync(
            string toEmail,
            string fullName,
            string title,
            string message,
            string? actionUrl = null)
        {
            try
            {
                var smtpHost = _config["EmailSettings:SmtpHost"] ?? "smtp.gmail.com";
                var smtpPort = int.Parse(_config["EmailSettings:SmtpPort"] ?? "587");
                var smtpUser = _config["EmailSettings:SmtpUser"];
                var smtpPass = _config["EmailSettings:SmtpPass"];
                if (string.IsNullOrWhiteSpace(smtpUser) || string.IsNullOrWhiteSpace(smtpPass))
                {
                    _logger.LogWarning("Skipped notification email because EmailSettings are incomplete");
                    return;
                }

                var safeName = System.Net.WebUtility.HtmlEncode(fullName);
                var safeTitle = System.Net.WebUtility.HtmlEncode(title);
                var safeMessage = System.Net.WebUtility.HtmlEncode(message);
                string? resolvedActionUrl = null;
                if (Uri.TryCreate(actionUrl, UriKind.Absolute, out var absoluteAction))
                {
                    resolvedActionUrl = absoluteAction.ToString();
                }
                else if (!string.IsNullOrWhiteSpace(actionUrl))
                {
                    var configuredBase = _config["App:BaseUrl"];
                    if (Uri.TryCreate(configuredBase, UriKind.Absolute, out var baseUri))
                    {
                        var origin = new Uri(baseUri.GetLeftPart(UriPartial.Authority));
                        resolvedActionUrl = new Uri(origin, actionUrl).ToString();
                    }
                }
                var safeActionUrl = resolvedActionUrl is null
                    ? null
                    : System.Net.WebUtility.HtmlEncode(resolvedActionUrl);
                var action = safeActionUrl is null
                    ? string.Empty
                    : $"<p style='margin:24px 0 0'><a href='{safeActionUrl}' style='display:inline-block;background:#FF6B35;color:#fff;padding:12px 20px;border-radius:8px;text-decoration:none;font-weight:700'>Open in TripMate</a></p>";

                var email = new MimeMessage();
                email.From.Add(new MailboxAddress("TripMate", smtpUser));
                email.To.Add(new MailboxAddress(safeName, toEmail));
                email.Subject = $"[TripMate] {title}";
                email.Body = new TextPart(TextFormat.Html)
                {
                    Text = $"""
                        <div style='font-family:Arial,sans-serif;max-width:600px;margin:0 auto;border:1px solid #eee;border-radius:16px;overflow:hidden'>
                          <div style='background:#FF6B35;color:#fff;padding:24px'><h1 style='margin:0;font-size:22px'>TripMate</h1></div>
                          <div style='padding:28px'>
                            <p style='color:#555'>Hello {safeName},</p>
                            <h2 style='color:#222'>{safeTitle}</h2>
                            <p style='color:#555;line-height:1.6'>{safeMessage}</p>
                            {action}
                          </div>
                        </div>
                        """
                };

                using var smtp = new SmtpClient();
                await smtp.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(smtpUser, smtpPass);
                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);
                _logger.LogInformation("Notification email sent to {Email} for {Title}", toEmail, title);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification email to {Email}", toEmail);
            }
        }
    }
}
