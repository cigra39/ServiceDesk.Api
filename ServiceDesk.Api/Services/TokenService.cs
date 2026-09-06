using Microsoft.AspNetCore.Identity;
using ServiceDesk.Api.Entities;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using ServiceDesk.Api.Authentication;

namespace ServiceDesk.Api.Services
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _configuration;
        private readonly UserManager<ApplicationUser> _userManager;

        public TokenService(
            IConfiguration configuration,
            UserManager<ApplicationUser> userManager)
        {
            _configuration = configuration;
            _userManager = userManager;
        }

        public async Task<TokenResult> CreateTokenAsync(ApplicationUser user)
        {

            var roles = await _userManager.GetRolesAsync(user);
            var securityStamp = await _userManager.GetSecurityStampAsync(user);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName!),
                new Claim(ClaimTypes.Email, user.Email!),
                new Claim(
                    "organizationId",
                    user.OrganizationId.ToString()),
                new Claim(
                    CustomClaimTypes.SecurityStamp,
                    securityStamp)
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var secretForKey =
                _configuration["Authentication:SecretForKey"]
                ?? throw new InvalidOperationException(
                    "Authentication secret key was not found.");

            var issuer =
                _configuration["Authentication:Issuer"]
                ?? throw new InvalidOperationException(
                    "Authentication issuer was not found.");

            var audience =
                _configuration["Authentication:Audience"]
                ?? throw new InvalidOperationException(
                    "Authentication audience was not found.");

            var tokenLifetimeMinutes =
                _configuration.GetValue<int?>(
                    "Authentication:TokenLifetimeMinutes")
                ?? throw new InvalidOperationException(
                    "Token lifetime was not found.");

            if (tokenLifetimeMinutes <= 0)
            {
                throw new InvalidOperationException(
                    "Token lifetime must be greater than zero.");
            }

            var securityKey = new SymmetricSecurityKey(
                Convert.FromBase64String(secretForKey));

            var signingCredentials = new SigningCredentials(
                securityKey,
                SecurityAlgorithms.HmacSha256);

            var currentTime = DateTime.UtcNow;

            var expiresAtUtc = currentTime.AddMinutes(tokenLifetimeMinutes);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                notBefore: currentTime,
                expires: expiresAtUtc,
                signingCredentials: signingCredentials);

            var accessToken =
                new JwtSecurityTokenHandler().WriteToken(token);

            return new TokenResult
            {
                AccessToken = accessToken,
                ExpiresAtUtc = expiresAtUtc
            };
        }
    }
}
