namespace ServiceDesk.Api.Services
{
    public interface ICurrentUserService
    {
        bool IsAuthenticated { get; }
        String? UserId { get; }

        int? OrganizationId { get; }

        bool IsInRole(string role);
    }
}
