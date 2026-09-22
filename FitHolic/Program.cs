using FitHolic;
using FitHolic.Class;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.Text;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// 1. Configure Forwarded Headers (Standard for reverse proxies like Render, Nginx, Cloudflare)
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.SetIsOriginAllowed(origin => true) // Handles dynamic URLs
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddControllers();

// 2. Output Cache Policies (Pinagsama sa iisang configuration)
builder.Services.AddOutputCache(options =>
{
    options.AddPolicy("ReportsListCache", policy =>
        policy.Expire(TimeSpan.FromMinutes(10))
              .SetVaryByQuery("search", "startDateCreated", "endDateCreated", "minRevenue", "maxRevenue", "lastUpdated", "page", "pageSize", "sortBy")
              .Tag("reports-cache-tag"));

    options.AddPolicy("ExpensesListCache", policy =>
        policy.Expire(TimeSpan.FromMinutes(5))
              .SetVaryByQuery("searchTerm", "startDateCreated", "endDateCreated", "minExpenses", "maxExpenses", "lastUpdated", "pageNumber", "pageSize", "sortBy")
              .Tag("expenses-data"));
});

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("StrictWritePolicy", opt =>
    {
        opt.PermitLimit = 15;
        opt.Window = TimeSpan.FromMinutes(1);
    });

    options.AddFixedWindowLimiter("DownloadPolicy", opt =>
    {
        opt.PermitLimit = 5;
        opt.Window = TimeSpan.FromMinutes(1);
    });
});

builder.Services.AddScoped<GenerateTokenJwt>();
builder.Services.AddScoped<SendEmailOtp>();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<FitHolicDbContext>(options =>
    options.UseNpgsql(connectionString));

var jwtKey = builder.Configuration["Jwt:Key"]!;
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

builder.Services.AddAuthorization();
builder.Services.AddOpenApi();

var app = builder.Build();

// Enable Forwarded Headers Middleware FIRST before routing/CORS/Auth
app.UseForwardedHeaders();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FitHolicDbContext>();
    await db.Database.MigrateAsync();
}

app.MapOpenApi();
app.MapScalarApiReference();

app.UseCors("AllowAll");
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();