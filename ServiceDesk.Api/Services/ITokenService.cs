using ServiceDesk.Api.Entities;

namespace ServiceDesk.Api.Services
{
    public interface ITokenService
    {
        Task<TokenResult> CreateTokenAsync(ApplicationUser user);
    }
}
