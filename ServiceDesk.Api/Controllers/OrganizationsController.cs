using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceDesk.Api.DbContexts;
using ServiceDesk.Api.Models.Organizations;
using ServiceDesk.Api.Entities;

namespace ServiceDesk.Api.Controllers;

[ApiController]
[Route("api/organizations")]
public class OrganizationsController : ControllerBase
{
    private readonly ServiceDeskContext _context;

    public OrganizationsController(ServiceDeskContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrganizationDto>>> GetOrganizations()
    {
        var organizations = await _context.Organizations
            .AsNoTracking()
            .Select(organization => new OrganizationDto
            {
                Id = organization.Id,
                Name = organization.Name,
                CreatedAtUtc = organization.CreatedAtUtc,
                IsActive = organization.IsActive
            })
            .ToListAsync();

        return Ok(organizations);
    }

    [HttpGet("{organizationId:int}")]
    public async Task<ActionResult<OrganizationDto>> GetOrganization(
        int organizationId)
    {
        var organization = await _context.Organizations
            .AsNoTracking()
            .Where(organization => organization.Id == organizationId)
            .Select(organization => new OrganizationDto
            {
                Id = organization.Id,
                Name = organization.Name,
                CreatedAtUtc = organization.CreatedAtUtc,
                IsActive = organization.IsActive
            })
            .FirstOrDefaultAsync();

        if (organization == null)
        {
            return NotFound();
        }

        return Ok(organization);
    }

    [HttpPost]
    public async Task<ActionResult<OrganizationDto>> CreateOrganization(
        CreateOrganizationDto createOrganizationDto)
    {
        var organization = new Organization
        {
            Name = createOrganizationDto.Name.Trim()
        };

        _context.Organizations.Add(organization);
        await _context.SaveChangesAsync();

        var organizationDto = new OrganizationDto
        {
            Id = organization.Id,
            Name = organization.Name,
            CreatedAtUtc = organization.CreatedAtUtc,
            IsActive = organization.IsActive
        };

        return CreatedAtAction(
            nameof(GetOrganization),
            new { organizaitonId = organization.Id },
            organizationDto);
    }

    [HttpPut("{organizationId:int}")]
    public async Task<IActionResult> UpdateOrganization(
        int organizationId,
        UpdateOrganizationDto updateOrganizationDto)
    {
        var organization = await _context.Organizations
            .FirstOrDefaultAsync(organization =>
            organization.Id == organizationId);

        if (organization is null)
        {
            return NotFound();
        }

        organization.Name = updateOrganizationDto.Name.Trim();
        organization.IsActive = updateOrganizationDto.IsActive;

        await _context.SaveChangesAsync();

        return NoContent();
    }
}