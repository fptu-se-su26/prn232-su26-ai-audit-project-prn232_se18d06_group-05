using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Supabase;
using TripMate_WebAPI.Services;
var builder = WebApplication.CreateBuilder(args);

// ── Supabase Client (singleton) ───────────────────────────────────────────────
var supabaseUrl = builder.Configuration["Supabase:Url"]!;
var supabaseKey = builder.Configuration["Supabase:AnonKey"]!;
var jwksUri     = builder.Configuration["Supabase:JwksUri"]!;
var issuer      = builder.Configuration["Supabase:Issuer"]!;

builder.Services.AddSingleton(_ =>
{
    var client = new Client(supabaseUrl, supabaseKey, new SupabaseOptions
    {
        AutoRefreshToken = true,
        AutoConnectRealtime = false,
    });
    client.InitializeAsync().GetAwaiter().GetResult();
    return client;
});

// ── Auth Service ──────────────────────────────────────────────────────────────
builder.Services.AddHttpClient<SupabaseAuthService>();
builder.Services.AddScoped<SupabaseAuthService>();
builder.Services.AddScoped<DatabaseSeeder>();
builder.Services.AddScoped<IEmailService, EmailService>();

// ── Tour Service ──────────────────────────────────────────────────────────────
builder.Services.AddHttpClient<TourService>();
builder.Services.AddScoped<TourService>();

// ── Booking Service ───────────────────────────────────────────────────────────
builder.Services.AddHttpClient<BookingService>();
builder.Services.AddScoped<BookingService>();

// ── Guide Approval Service ────────────────────────────────────────────────────
builder.Services.AddHttpClient<GuideApprovalService>();
builder.Services.AddScoped<GuideApprovalService>();

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
