namespace ServiceDesk.Api.Models.Users
{
    public class OrganizationUserDto
    {
        public string Id { get; set; } = string.Empty;

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public int OrganizationId { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public IEnumerable<string> Roles { get; set; } = [];
    }
}
