namespace ServiceDesk.Api.Models
{
    public class CurrentUserDto
    {
        public string Id { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public int OrganizationId { get; set; }

        public IEnumerable<string> Roles { get; set; } = [];
    }
}
