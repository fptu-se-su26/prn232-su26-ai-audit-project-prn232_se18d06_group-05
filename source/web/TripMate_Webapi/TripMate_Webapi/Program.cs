using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Supabase;
using TripMate_WebAPI.Services;
using TripMate_Webapi.Repositories;
using DotNetEnv;

var builder = WebApplication.CreateBuilder(args);

// Load environment variables from .env file
var envPath = System.IO.Path.Combine(builder.Environment.ContentRootPath, ".env");
if (System.IO.File.Exists(envPath))
{
    Env.Load(envPath);
}
else
{
    Env.Load();
}

// Override configuration with environment variables
builder.Configuration["Supabase:Url"] = Environment.GetEnvironmentVariable("SUPABASE_URL") ?? builder.Configuration["Supabase:Url"];
builder.Configuration["Supabase:AnonKey"] = Environment.GetEnvironmentVariable("SUPABASE_ANON_KEY") ?? builder.Configuration["Supabase:AnonKey"];
builder.Configuration["Supabase:ServiceRoleKey"] = Environment.GetEnvironmentVariable("SUPABASE_SERVICE_ROLE_KEY") ?? builder.Configuration["Supabase:ServiceRoleKey"];
builder.Configuration["GoogleOAuth:ClientId"] = Environment.GetEnvironmentVariable("GOOGLE_OAUTH_CLIENT_ID") ?? builder.Configuration["GoogleOAuth:ClientId"];
builder.Configuration["GoogleOAuth:ClientSecret"] = Environment.GetEnvironmentVariable("GOOGLE_OAUTH_CLIENT_SECRET") ?? builder.Configuration["GoogleOAuth:ClientSecret"];
builder.Configuration["ReCaptcha:SiteKey"] = Environment.GetEnvironmentVariable("RECAPTCHA_SITE_KEY") ?? builder.Configuration["ReCaptcha:SiteKey"];
builder.Configuration["ReCaptcha:SecretKey"] = Environment.GetEnvironmentVariable("RECAPTCHA_SECRET_KEY") ?? builder.Configuration["ReCaptcha:SecretKey"];
builder.Configuration["SerpApi:ApiKey"] = Environment.GetEnvironmentVariable("SERPAPI_KEY") ?? builder.Configuration["SerpApi:ApiKey"];
builder.Configuration["Cloudinary:CloudName"] = Environment.GetEnvironmentVariable("CLOUDINARY_CLOUD_NAME") ?? builder.Configuration["Cloudinary:CloudName"];
builder.Configuration["Cloudinary:ApiKey"] = Environment.GetEnvironmentVariable("CLOUDINARY_API_KEY") ?? builder.Configuration["Cloudinary:ApiKey"];
builder.Configuration["Cloudinary:ApiSecret"] = Environment.GetEnvironmentVariable("CLOUDINARY_API_SECRET") ?? builder.Configuration["Cloudinary:ApiSecret"];
builder.Configuration["EmailSettings:SmtpHost"] = Environment.GetEnvironmentVariable("SMTP_HOST") ?? builder.Configuration["EmailSettings:SmtpHost"];
builder.Configuration["EmailSettings:SmtpPort"] = Environment.GetEnvironmentVariable("SMTP_PORT") ?? builder.Configuration["EmailSettings:SmtpPort"];
builder.Configuration["EmailSettings:SmtpUser"] = Environment.GetEnvironmentVariable("SMTP_USER") ?? builder.Configuration["EmailSettings:SmtpUser"];
builder.Configuration["EmailSettings:SmtpPass"] = Environment.GetEnvironmentVariable("SMTP_PASS") ?? builder.Configuration["EmailSettings:SmtpPass"];

// ── Supabase Client (singleton) ───────────────────────────────────────────────
var supabaseUrl = builder.Configuration["Supabase:Url"]!;
var supabaseKey = builder.Configuration["Supabase:AnonKey"]!;

// Dynamically construct JWKS URI and Issuer from the active Supabase URL
var jwksUri = $"{supabaseUrl.TrimEnd('/')}/auth/v1/.well-known/jwks.json";
var issuer = $"{supabaseUrl.TrimEnd('/')}/auth/v1";

builder.Services.AddSingleton(_ =>
{
    var options = new SupabaseOptions
    {
        AutoRefreshToken = true,
        AutoConnectRealtime = false,
    };
    options.Headers.Add("Authorization", $"Bearer {supabaseKey}");
    var client = new Client(supabaseUrl, supabaseKey, options);
    client.InitializeAsync().GetAwaiter().GetResult();
    return client;
});

// ── Auth Service ──────────────────────────────────────────────────────────────
builder.Services.AddHttpClient<SupabaseAuthService>();
builder.Services.AddScoped<SupabaseAuthService>();
builder.Services.AddHttpClient<GoogleAuthService>();
builder.Services.AddScoped<IGoogleAuthService, GoogleAuthService>();
builder.Services.AddHttpClient<SupabasePasswordResetService>();
builder.Services.AddScoped<ISupabasePasswordResetService, SupabasePasswordResetService>();
builder.Services.AddScoped<DatabaseSeeder>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddHttpClient<PasswordResetService>();
builder.Services.AddScoped<IPasswordResetService, PasswordResetService>();

// ── Tour Service ──────────────────────────────────────────────────────────────
builder.Services.AddHttpClient<TourService>();
builder.Services.AddScoped<TourService>();

// ── Booking Service ───────────────────────────────────────────────────────────
builder.Services.AddHttpClient<BookingService>();
builder.Services.AddScoped<BookingService>();

// ── Repositories ──────────────────────────────────────────────────────────────
builder.Services.AddScoped<ITripRequestRepository, TripRequestRepository>();
builder.Services.AddScoped<IBookingRepository, BookingRepository>();
builder.Services.AddScoped<IGuideRepository, GuideRepository>();

// ── Guide Approval Service ────────────────────────────────────────────────────
builder.Services.AddHttpClient<GuideApprovalService>();
builder.Services.AddScoped<GuideApprovalService>();

// ── Admin Service ─────────────────────────────────────────────────────────────
builder.Services.AddHttpClient<AdminService>();
builder.Services.AddScoped<AdminService>();

// ── Chat & Notification Services ─────────────────────────────────────────────
builder.Services.AddHttpClient<ChatService>();
builder.Services.AddScoped<ChatService>();
builder.Services.AddHttpClient<NotificationService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

// ── Survey Service ────────────────────────────────────────────────────────────
builder.Services.AddHttpClient<SurveyService>();
builder.Services.AddScoped<SurveyService>();

// ── Location (SerpAPI) ────────────────────────────────────────────────────────
builder.Services.AddHttpClient<LocationService>();
builder.Services.AddScoped<LocationService>();

// ── JWT Bearer — RS256 via JWKS ──────────────────────────────────────────────
// Fetch public keys từ Supabase JWKS endpoint khi startup, cache trong memory
IssuerSigningKeyResolver jwksKeyResolver;
{
    using var http = new HttpClient();
    var jwksJson = http.GetStringAsync(jwksUri).GetAwaiter().GetResult();
    var keySet = new Microsoft.IdentityModel.Tokens.JsonWebKeySet(jwksJson);
    var keys = keySet.GetSigningKeys();
    jwksKeyResolver = (_, _, _, _) => keys;
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKeyResolver = jwksKeyResolver,
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = "authenticated",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = ctx =>
            {
                Console.WriteLine($"[JWT] Auth failed: {ctx.Exception.Message}");
                return Task.CompletedTask;
            },
            OnTokenValidated = ctx =>
            {
                var sub = ctx.Principal?.FindFirst("sub")?.Value;
                Console.WriteLine($"[JWT] Token valid — sub: {sub}");
                return Task.CompletedTask;
            },
        };
    });

builder.Services.AddAuthorization();

// ── CORS ──────────────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy
            .SetIsOriginAllowed(_ => true)  // cho phép mọi origin khi dev
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials());
});

// ── Swagger + Controllers + MVC Views ────────────────────────────────────────
builder.Services.AddControllersWithViews(); // Add MVC with Views support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(TripMate_WebAPI.Infrastructure.SwaggerConfig.AddJwtSecurity);
builder.Services.AddOpenApi();

var app = builder.Build();

// ── Middleware pipeline ───────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapOpenApi();
}

// Enable static files (HTML, CSS, JS)
app.UseDefaultFiles();  // Serve index.html as default
app.UseStaticFiles();   // Serve files from wwwroot

app.UseCors("AllowAll");

// Allow Google Sign-In popups to communicate with the main window
app.Use(async (context, next) =>
{
    context.Response.Headers["Cross-Origin-Opener-Policy"] = "same-origin-allow-popups";
    await next();
});

// Chỉ redirect HTTPS trên production — dev để Flutter Web gọi http được
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();   // phải trước UseAuthorization
app.UseAuthorization();
app.MapControllers(); // Map API controllers
app.MapControllerRoute( // Map MVC routes
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Seed database
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
    await seeder.SeedAsync();
}

app.Run();
