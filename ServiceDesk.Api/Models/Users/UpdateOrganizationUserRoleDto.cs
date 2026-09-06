using System.ComponentModel.DataAnnotations;

namespace ServiceDesk.Api.Models.Users
{
    public class UpdateOrganizationUserRoleDto
    {
        [Required]
        public string Role { get; set; } = string.Empty;
    }
}
