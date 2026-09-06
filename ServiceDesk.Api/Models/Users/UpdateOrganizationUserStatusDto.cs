using System.ComponentModel.DataAnnotations;

namespace ServiceDesk.Api.Models.Users
{
    public class UpdateOrganizationUserStatusDto
    {
        [Required]
        public bool? IsActive { get; set; }
    }
}
