using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace TripMate_WebAPI.Infrastructure;

public static class SwaggerConfig
{
    private const string SchemeId = "Bearer";

    public static void AddJwtSecurity(SwaggerGenOptions c)
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "TripMate API", Version = "v1" });

        c.AddSecurityDefinition(SchemeId, new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Nhập access_token từ POST /api/auth/login",
        });

        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id   = SchemeId,
                    }
                },
                Array.Empty<string>()
            }
        });
    }
}
