namespace ServiceDesk.Api.Services
{
    public interface ICurrentUserService
    {
        bool IsAuthenticated { get; }
        string? UserId { get; }

        int? OrganizationId { get; }

        bool IsInRole(string role);
    }
}
