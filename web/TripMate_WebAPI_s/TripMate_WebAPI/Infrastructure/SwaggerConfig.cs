using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace TripMate_WebAPI.Infrastructure;

public static class SwaggerConfig
{
    private const string SchemeId = "Bearer";

    public static void AddJwtSecurity(SwaggerGenOptions c)
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "TripMate API", Version = "v1" });

        // Định nghĩa scheme Bearer
        c.AddSecurityDefinition(SchemeId, new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Nhập access_token lấy từ POST /api/auth/login (không cần prefix 'Bearer')",
        });

        // Áp dụng cho tất cả endpoint — Swashbuckle 10 dùng Func<OpenApiDocument, ...>
        c.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecuritySchemeReference(SchemeId),
                new List<string>()
            }
        });
    }
}
