using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceDesk.Api.DbContexts;
using ServiceDesk.Api.Models.Organizations;
using ServiceDesk.Api.Entities;
using Microsoft.AspNetCore.Authorization;
using ServiceDesk.Api.Authorization;
using ServiceDesk.Api.Services;

namespace ServiceDesk.Api.Controllers
{

    [Authorize]
    [ApiController]
    [Route("api/organizations")]
    public class OrganizationsController : ControllerBase
    {
        private readonly ServiceDeskContext _context;
        private readonly ICurrentUserService _currentUserService;

        public OrganizationsController(
            ServiceDeskContext context,
            ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        [Authorize(Roles = RoleNames.PlatformAdmin)]
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
            if (!CanAccessOrganization(organizationId))
            {
                return Forbid();
            }

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

        [Authorize(Roles = RoleNames.PlatformAdmin)]
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
                new { organizationId = organization.Id },
                organizationDto);
        }

        [Authorize(Roles = RoleNames.Owner + "," + RoleNames.Admin)]
        [HttpPut("{organizationId:int}")]
        public async Task<IActionResult> UpdateOrganization(
            int organizationId,
            UpdateOrganizationDto updateOrganizationDto)
        {
            if (!CanAccessOrganization(organizationId))
            {
                return Forbid();
            }

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
        private bool CanAccessOrganization(int organizationId)
        {
            return _currentUserService.IsInRole(RoleNames.PlatformAdmin)
                || _currentUserService.OrganizationId == organizationId;
        }
    }
}