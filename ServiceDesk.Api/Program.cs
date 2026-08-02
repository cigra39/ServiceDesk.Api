using Microsoft.EntityFrameworkCore;
using ServiceDesk.Api.DbContexts;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;
using ServiceDesk.Api.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using ServiceDesk.Api.Services;
using ServiceDesk.Api.OpenApi;

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

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer =
            builder.Configuration["Authenticaiton:Issuer"],

            ValidateAudience = true,
            ValidAudience =
            builder.Configuration["Authenticaiton:Audience"],

            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            IssuerSigningKey = new SymmetricSecurityKey(
                    Convert.FromBase64String(secretForKey)),

            ClockSkew = TimeSpan.Zero

        };
    });

builder.Services.AddAuthorization();

builder.Services.AddScoped<ITokenService, TokenService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider
        .GetRequiredService<RoleManager<IdentityRole>>();

    string[] roleNames =
    [
            "Owner",
            "Admin",
            "Agent",
            "Customer"

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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
