using Microsoft.Extensions.Logging.Abstractions;
using System.Security.Claims;

namespace ServiceDesk.Api.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private ClaimsPrincipal? CurrentUser =>
            _httpContextAccessor.HttpContext?.User;

        public bool IsAuthenticated =>
            CurrentUser?.Identity?.IsAuthenticated == true;

        public string? UserId =>
            CurrentUser?.FindFirstValue(ClaimTypes.NameIdentifier);

        public int? OrganizationId
        {
            get
            {
                var organizationIdValue =
                    CurrentUser?.FindFirstValue("organizationId");

                return int.TryParse(
                    organizationIdValue,
                    out var organizationId)
                    ? organizationId : null;
            }
        }

        public bool IsInRole(string role) =>
            CurrentUser?.IsInRole(role) == true;
    }
}
