using System.ComponentModel.DataAnnotations;

namespace ServiceDesk.Api.Models.Organizations;

public class CreateOrganizationDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
}
