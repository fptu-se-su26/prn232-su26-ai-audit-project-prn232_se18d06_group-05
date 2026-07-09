using System.IdentityModel.Tokens.Jwt;
using TripMate_WebAPI.Services;

namespace TripMate_Webapi.Middlewares
{
    public class AutoRefreshMiddleware
    {
        private readonly RequestDelegate _next;

        public AutoRefreshMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, SupabaseAuthService authService)
        {
            var accessToken = context.Request.Cookies["access_token"];
            var refreshToken = context.Request.Cookies["refresh_token"];

            // Nếu có accessToken, ta kiểm tra hạn
            if (!string.IsNullOrEmpty(accessToken))
            {
                try
                {
                    var handler = new JwtSecurityTokenHandler();
                    var jwtToken = handler.ReadJwtToken(accessToken);

                    // Lấy thời gian hết hạn (ValidTo trả về UTC)
                    var exp = jwtToken.ValidTo;

                    // Nếu sắp hết hạn trong vòng 5 phút (hoặc đã hết hạn) và có refresh token
                    if (DateTime.UtcNow >= exp.AddMinutes(-5) && !string.IsNullOrEmpty(refreshToken))
                    {
                        Console.WriteLine($"[AutoRefresh] Token expires at {exp}. Attempting silent refresh...");
                        
                        // Gọi SupabaseAuthService để refresh
                        var newAuth = await authService.RefreshAsync(refreshToken);

                        if (newAuth != null && !string.IsNullOrEmpty(newAuth.AccessToken))
                        {
                            Console.WriteLine("[AutoRefresh] Successfully refreshed token.");
                            
                            // 1. Cập nhật cookie cho client
                            var accessOptions = new CookieOptions
                            {
                                HttpOnly = true,
                                Secure = false, // Set to true in production
                                SameSite = SameSiteMode.Lax,
                                Expires = DateTime.UtcNow.AddDays(7),
                                Path = "/"
                            };
                            
                            var refreshOptions = new CookieOptions
                            {
                                HttpOnly = true,
                                Secure = false,
                                SameSite = SameSiteMode.Lax,
                                Expires = DateTime.UtcNow.AddDays(30),
                                Path = "/"
                            };

                            context.Response.Cookies.Append("access_token", newAuth.AccessToken, accessOptions);
                            
                            if (!string.IsNullOrEmpty(newAuth.RefreshToken))
                            {
                                context.Response.Cookies.Append("refresh_token", newAuth.RefreshToken, refreshOptions);
                            }

                            // 2. Ghi đè header Authorization để JWT Middleware tiếp theo đọc được token mới thay vì token cũ
                            context.Request.Headers["Authorization"] = $"Bearer {newAuth.AccessToken}";
                            
                            // Ghi đè vào cookie context (nếu JWT Middleware cố tình đọc trực tiếp từ context.Request.Cookies)
                            // Lưu ý: Request.Cookies là read-only collection trong ASP.NET Core, nên việc đổi header là cách chuẩn xác nhất.
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[AutoRefresh] Error parsing token or refreshing: {ex.Message}");
                    // Nếu lỗi do refresh (VD: refresh_token chết), ta để request đi tiếp
                    // JWT Middleware sẽ bắt token cũ (đã hết hạn) và tự văng 401.
                }
            }
            else if (string.IsNullOrEmpty(accessToken) && !string.IsNullOrEmpty(refreshToken))
            {
                // Trường hợp đặc biệt: mất access_token nhưng còn refresh_token
                try
                {
                    Console.WriteLine("[AutoRefresh] Access token missing but refresh token exists. Attempting silent refresh...");
                    var newAuth = await authService.RefreshAsync(refreshToken);

                    if (newAuth != null && !string.IsNullOrEmpty(newAuth.AccessToken))
                    {
                        var accessOptions = new CookieOptions
                        {
                            HttpOnly = true,
                            Secure = false,
                            SameSite = SameSiteMode.Lax,
                            Expires = DateTime.UtcNow.AddDays(7),
                            Path = "/"
                        };
                        var refreshOptions = new CookieOptions
                        {
                            HttpOnly = true,
                            Secure = false,
                            SameSite = SameSiteMode.Lax,
                            Expires = DateTime.UtcNow.AddDays(30),
                            Path = "/"
                        };

                        context.Response.Cookies.Append("access_token", newAuth.AccessToken, accessOptions);
                        if (!string.IsNullOrEmpty(newAuth.RefreshToken))
                        {
                            context.Response.Cookies.Append("refresh_token", newAuth.RefreshToken, refreshOptions);
                        }

                        context.Request.Headers["Authorization"] = $"Bearer {newAuth.AccessToken}";
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[AutoRefresh] Error recovering from refresh_token: {ex.Message}");
                }
            }

            await _next(context);
        }
    }
}
