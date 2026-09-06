using Microsoft.EntityFrameworkCore;
using ServiceDesk.Api.DbContexts;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;
using ServiceDesk.Api.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using ServiceDesk.Api.Services;
using ServiceDesk.Api.OpenApi;
using ServiceDesk.Api.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using ServiceDesk.Api.RateLimiting;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Security.Claims;
using ServiceDesk.Api.Authentication;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter());
    });
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();

    options.AddOperationTransformer<BearerSecurityRequirementTransformer>();
});

var connectionString = builder.Configuration
    .GetConnectionString("ServiceDeskConnectionString")
    ?? throw new InvalidOperationException(
        "Connection string 'ServiceDeskConnectionString' was not found.");

builder.Services.AddDbContext<ServiceDeskContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDataProtection();

builder.Services.AddIdentityCore<ApplicationUser>(options =>
{
    options.User.RequireUniqueEmail = true;

    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
})
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ServiceDeskContext>()
    .AddDefaultTokenProviders();

var secretForKey =
    builder.Configuration["Authentication:SecretForKey"]
    ?? throw new InvalidOperationException(
        "Authentication secret key was not found.");

var issuer =
    builder.Configuration["Authentication:Issuer"]
    ?? throw new InvalidOperationException(
        "Authentication issuer was not found.");

var audience =
    builder.Configuration["Authentication:Audience"]
    ?? throw new InvalidOperationException(
        "Authentication audience was not found.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,

            ValidateAudience = true,
            ValidAudience = audience,

            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            IssuerSigningKey = new SymmetricSecurityKey(
                    Convert.FromBase64String(secretForKey)),

            ClockSkew = TimeSpan.Zero

        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var userId = context.Principal?
                .FindFirstValue(ClaimTypes.NameIdentifier);

                if (string.IsNullOrWhiteSpace(userId))
                {
                    context.Fail("The token does not contain a user identifier.");

                    return;
                }

                var tokenSecurityStamp = context.Principal?
                    .FindFirstValue(CustomClaimTypes.SecurityStamp);

                if (string.IsNullOrWhiteSpace(tokenSecurityStamp))
                {
                    context.Fail("The token does not contain a security stamp.");
                    return;
                }

                var dbContext = context.HttpContext.RequestServices
                .GetRequiredService<ServiceDeskContext>();

                var account = await dbContext.Users
                    .AsNoTracking()
                    .Where(user => user.Id == userId)
                    .Select(user => new
                    {
                        user.IsActive,
                        user.SecurityStamp,
                        OrganizationIsActive = user.Organization.IsActive
                    })
                    .SingleOrDefaultAsync(
                       context.HttpContext.RequestAborted);

                if (account is null ||
                    !account.IsActive ||
                    !account.OrganizationIsActive ||
                    !string.Equals(
                        account.SecurityStamp,
                        tokenSecurityStamp,
                        StringComparison.Ordinal))
                {
                    context.Fail("The user account or token is no longer valid.");
                }
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.OnRejected = async (
        context,
        cancellationToken) =>
    {
        int? retryAfterSeconds = null;

        if (context.Lease.TryGetMetadata(
            MetadataName.RetryAfter,
            out var retryAfter))
        {
            retryAfterSeconds =
            (int)Math.Ceiling(retryAfter.TotalSeconds);

            context.HttpContext.Response.Headers["Retry-After"] =
                retryAfterSeconds.Value.ToString(
                    CultureInfo.InvariantCulture);
        }

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status429TooManyRequests,
            Title = "Too many requests",
            Detail = retryAfterSeconds is null
                ? "Try again later."
                : $"Try again in {retryAfterSeconds} seconds."
        };

        await context.HttpContext.Response.WriteAsJsonAsync(
            problemDetails,
            cancellationToken);
    };

    options.AddPolicy(
        RateLimitPolicyNames.Login,
        httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey:
                httpContext.Connection.RemoteIpAddress?.ToString()
                ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    QueueProcessingOrder =
                        QueueProcessingOrder.OldestFirst,
                    AutoReplenishment = true
                }));
    options.AddPolicy(
        RateLimitPolicyNames.Onboarding,
        httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey:
                httpContext.Connection.RemoteIpAddress?.ToString()
                ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 3,
                Window = TimeSpan.FromHours(1),
                QueueLimit = 0,
                QueueProcessingOrder =
                    QueueProcessingOrder.OldestFirst,
                AutoReplenishment = true
            }));
});

builder.Services.AddScoped<ITokenService, TokenService>();

builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider
        .GetRequiredService<RoleManager<IdentityRole>>();

    string[] roleNames =
    [
            RoleNames.PlatformAdmin,
            RoleNames.Owner,
            RoleNames.Admin,
            RoleNames.Agent,
            RoleNames.Customer
    ];

    foreach (var roleName in roleNames)
    {
        var roleExists = await roleManager.RoleExistsAsync(roleName);
        if (!roleExists)
        {
            var result = await roleManager.CreateAsync(
                new IdentityRole(roleName));

            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Role '{roleName}' could not be created.");
            }
        }
    }
}
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint(
            "/openapi/v1.json",
            "ServiceDesk API v1");
    });
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
