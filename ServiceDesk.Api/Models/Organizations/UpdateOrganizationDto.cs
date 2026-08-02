using System.ComponentModel.DataAnnotations;

namespace ServiceDesk.Api.Models.Organizations
{
    public class UpdateOrganizationDto
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public bool IsActive { get; set; }
    }
}
