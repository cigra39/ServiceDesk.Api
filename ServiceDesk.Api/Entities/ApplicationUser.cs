using Microsoft.AspNetCore.Identity;

namespace ServiceDesk.Api.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public int OrganizationId { get; set; }

        //navigation property preko kojeg mozemo pristupiti organizaciji korisnika
        public Organization Organization { get; set; } = null!;
    }
}
