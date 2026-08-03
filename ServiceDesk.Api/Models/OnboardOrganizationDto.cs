using System.ComponentModel.DataAnnotations;

namespace ServiceDesk.Api.Models
{
    public class OnboardOrganizationDto
    {
        [Required]
        [MaxLength(100)]
        public string OrganizationName { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }
}
