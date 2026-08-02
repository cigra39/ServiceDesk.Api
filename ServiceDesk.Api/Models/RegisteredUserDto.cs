namespace ServiceDesk.Api.Models
{
    public class RegisteredUserDto
    {
        public string Id { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int OrganizationId { get; set; }
        public string Role { get; set; } = string.Empty;
    }
}
