using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using ServiceDesk.Api.Authorization;
using ServiceDesk.Api.Entities;
using ServiceDesk.Api.Services;
using Microsoft.EntityFrameworkCore;
using ServiceDesk.Api.Models.Users;
using ServiceDesk.Api.DbContexts;

namespace ServiceDesk.Api.Controllers
{
    [ApiController]
    [Authorize(Roles = RoleNames.Owner + "," + RoleNames.Admin)]
    [Route("api/organizations/{organizationId:int}/users")]
    public class OrganizationUsersController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICurrentUserService _currentUserService;
        private readonly ServiceDeskContext _context;

        public OrganizationUsersController(
            UserManager<ApplicationUser> userManager,
            ICurrentUserService currentUserService,
            ServiceDeskContext context)
        {
            _userManager = userManager;
            _currentUserService = currentUserService;
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<OrganizationUserDto>>>
            GetOrganizationUsers(int organizationId)
        {
            if (_currentUserService.OrganizationId != organizationId)
            {
                return Forbid();
            }

            var users = await _userManager.Users
                .AsNoTracking()
                .Where(user => user.OrganizationId == organizationId)
                .OrderBy(user => user.LastName)
                .ThenBy(user => user.FirstName)
                .ToListAsync();

            var userDtos = new List<OrganizationUserDto>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);

                userDtos.Add(new OrganizationUserDto
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email ?? string.Empty,
                    OrganizationId = user.OrganizationId,
                    IsActive = user.IsActive,
                    CreatedAtUtc = user.CreatedAtUtc,
                    Roles = roles
                });
            }

            return Ok(userDtos);
        }

        [Authorize(Roles = RoleNames.Owner)]
        [HttpPatch("{userId}/role")]
        public async Task<IActionResult> UpdateOrganizationUserRole(
            int organizationId,
            string userId,
            UpdateOrganizationUserRoleDto updateUserRoleDto)
        {
            if (_currentUserService.OrganizationId != organizationId)
            {
                return Forbid();
            }

            if (_currentUserService.UserId == userId)
            {
                return BadRequest("You cannot change your own role.");
            }

            var allowedRoles = new[]
            {
                RoleNames.Admin,
                RoleNames.Agent,
                RoleNames.Customer
            };

            var newRole = allowedRoles.FirstOrDefault(role =>
            string.Equals(
                role,
                updateUserRoleDto.Role.Trim(),
                StringComparison.OrdinalIgnoreCase));

            if (newRole is null)
            {
                return BadRequest(
                    $"Role must be one of: {string.Join(", ", allowedRoles)}");
            }

            var user = await _userManager.Users
                .SingleOrDefaultAsync(user =>
                user.Id == userId &&
                user.OrganizationId == organizationId);

            if (user is null)
            {
                return NotFound("User was not found.");
            }

            var currentRoles = await _userManager.GetRolesAsync(user);

            if (currentRoles.Contains(RoleNames.Owner))
            {
                return Conflict("The Owner role cannot be changed.");
            }

            if (currentRoles.Count == 1 &&
                currentRoles.Contains(newRole))
            {
                return NoContent();
            }

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            if (currentRoles.Count > 0)
            {
                var removeResult =
                    await _userManager.RemoveFromRolesAsync(user, currentRoles);

                if (!removeResult.Succeeded)
                {
                    await transaction.RollbackAsync();

                    return Problem(
                        detail: "The existing user role could not be removed.",
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            }

            var addResult = await _userManager.AddToRoleAsync(user, newRole);

            if (!addResult.Succeeded)
            {
                await transaction.RollbackAsync();

                return Problem(
                    detail: "The new user role could not be assigned.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }

            var stampResult =
                await _userManager.UpdateSecurityStampAsync(user);

            if (!stampResult.Succeeded)
            {
                await transaction.RollbackAsync();

                return Problem(
                    detail: "The user's existing tokens could not be invalidated.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }

            await transaction.CommitAsync();

            return NoContent();
        }

        [HttpPatch("{userId}/status")]
        public async Task<IActionResult> UpdateOrganizationUserStatus(
            int organizationId,
            string userId,
            UpdateOrganizationUserStatusDto updateUserStatusDto)
        {
            if (_currentUserService.OrganizationId != organizationId)
            {
                return Forbid();
            }

            if (_currentUserService.UserId == userId)
            {
                return BadRequest("You cannot change your own account status.");
            }

            if (!updateUserStatusDto.IsActive.HasValue)
            {
                return BadRequest("The IsActive field is required.");
            }

            var user = await _userManager.Users
                .SingleOrDefaultAsync(user =>
                user.Id == userId &&
                user.OrganizationId == organizationId);

            if (user is null)
            {
                return NotFound("User was not found.");
            }

            var userRoles = await _userManager.GetRolesAsync(user);

            if (userRoles.Contains(RoleNames.Owner))
            {
                return Conflict("The owner account status cannot be changed.");
            }

            if (_currentUserService.IsInRole(RoleNames.Admin) &&
                userRoles.Contains(RoleNames.Admin))
            {
                return Forbid();
            }

            var newIsActive = updateUserStatusDto.IsActive.Value;

            if (user.IsActive == newIsActive)
            {
                return NoContent();
            }

            user.IsActive = newIsActive;

            var updateResult =
                await _userManager.UpdateSecurityStampAsync(user);

            if (!updateResult.Succeeded)
            {
                return Problem(
                    detail: "The user account status could not be updated.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }

            return NoContent();
        }
    }
}
