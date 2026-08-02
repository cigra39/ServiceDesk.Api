namespace ServiceDesk.Api.Models.Organizations;

public class OrganizationDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public bool IsActive { get; set; }
}