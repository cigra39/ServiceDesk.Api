using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using ServiceDesk.Api.DbContexts;
using ServiceDesk.Api.Entities;
using Microsoft.EntityFrameworkCore;
using ServiceDesk.Api.Models;
using ServiceDesk.Api.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using ServiceDesk.Api.Authorization;
using ServiceDesk.Api.Models.Organizations;
using Microsoft.AspNetCore.RateLimiting;
using ServiceDesk.Api.RateLimiting;

namespace ServiceDesk.Api.Controllers
{
    [ApiController]
    [Route("api/authentication")]
    public class AuthenticationController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ServiceDeskContext _context;
        private readonly ITokenService _tokenService;
        private readonly ICurrentUserService _currentUserService;

        public AuthenticationController(
            UserManager<ApplicationUser> userManager,
            ServiceDeskContext context,
            ITokenService tokenService,
            ICurrentUserService currentUserService)
        {
            _userManager = userManager;
            _context = context;
            _tokenService = tokenService;
            _currentUserService = currentUserService;
        }

        [Authorize(Roles = RoleNames.Owner + "," + RoleNames.Admin)]
        [HttpPost("register")]
        public async Task<ActionResult<RegisteredUserDto>> Register(
            RegisterUserDto registerUserDto)
        {
            var organizationId = _currentUserService.OrganizationId;

            if (organizationId is null)
            {
                return Forbid();
            }

            var organizationExists = await _context.Organizations
                .AnyAsync(organization =>
                organization.Id == organizationId.Value &&
                organization.IsActive);

            if (!organizationExists)
            {
                return Forbid();
            }

            var user = new ApplicationUser
            {
                UserName = registerUserDto.Email.Trim(),
                Email = registerUserDto.Email.Trim(),
                FirstName = registerUserDto.FirstName.Trim(),
                LastName = registerUserDto.LastName.Trim(),
                OrganizationId = organizationId.Value,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            };

            var creationResult = await _userManager.CreateAsync(
                user, registerUserDto.Password);

            if (!creationResult.Succeeded)
            {
                foreach (var error in creationResult.Errors)
                {
                    ModelState.AddModelError(error.Code, error.Description);
                }

                return ValidationProblem(ModelState);
            }

            var roleResult = await _userManager.AddToRoleAsync(
                user, RoleNames.Customer);

            if (!roleResult.Succeeded)
            {
                await _userManager.DeleteAsync(user);

                return Problem(
                    detail: "The user role could not be assigned.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }

            var registeredUserDto = new RegisteredUserDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email!,
                OrganizationId = user.OrganizationId,
                Role = RoleNames.Customer
            };

            return StatusCode(StatusCodes.Status201Created, registeredUserDto);
        }

        [AllowAnonymous]
        [EnableRateLimiting(RateLimitPolicyNames.Onboarding)]
        [HttpPost("onboard")]
        public async Task<ActionResult<OnboardingResultDto>> Onboard(
            OnboardOrganizationDto onboardOrganizationDto)
        {
            var email = onboardOrganizationDto.Email.Trim();

            var existingUser = await _userManager.FindByEmailAsync(email);

            if (existingUser is not null)
            {
                ModelState.AddModelError(
                    nameof(onboardOrganizationDto.Email),
                    "A user with this email already exists.");

                return ValidationProblem(ModelState);
            }

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            var organization = new Organization
            {
                Name = onboardOrganizationDto.OrganizationName.Trim(),
            };

            _context.Organizations.Add(organization);
            await _context.SaveChangesAsync();

            var owner = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FirstName = onboardOrganizationDto.FirstName.Trim(),
                LastName = onboardOrganizationDto.LastName.Trim(),
                OrganizationId = organization.Id,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            };

            var creationResult = await _userManager.CreateAsync(
                owner, onboardOrganizationDto.Password);

            if (!creationResult.Succeeded)
            {
                await transaction.RollbackAsync();
                foreach (var error in creationResult.Errors)
                {
                    ModelState.AddModelError(error.Code, error.Description);
                }
                return ValidationProblem(ModelState);
            }

            var roleResult = await _userManager.AddToRoleAsync(
                owner, RoleNames.Owner);

            if (!roleResult.Succeeded)
            {
                await transaction.RollbackAsync();

                return Problem(
                    detail: "The owner role could not be assigned.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }

            await transaction.CommitAsync();

            var result = new OnboardingResultDto
            {
                Organization = new OrganizationDto
                {
                    Id = organization.Id,
                    Name = organization.Name,
                    CreatedAtUtc = organization.CreatedAtUtc,
                    IsActive = organization.IsActive
                },
                Owner = new RegisteredUserDto
                {
                    Id = owner.Id,
                    FirstName = owner.FirstName,
                    LastName = owner.LastName,
                    Email = owner.Email!,
                    OrganizationId = owner.OrganizationId,
                    Role = RoleNames.Owner
                }
            };

            return StatusCode(StatusCodes.Status201Created, result);
        }

        [EnableRateLimiting(RateLimitPolicyNames.Login)]
        [HttpPost("login")]
        public async Task<ActionResult<TokenResult>> Login(
            LoginUserDto loginUserDto)
        {
            var user = await _userManager.FindByEmailAsync(
                loginUserDto.Email.Trim());

            if (user is null || !user.IsActive)
            {
                return Unauthorized("Invalid email or password.");
            }

            var passwordIsValid =
                await _userManager.CheckPasswordAsync(
                    user,
                    loginUserDto.Password);

            if (!passwordIsValid)
            {
                return Unauthorized("Invalid email or password.");
            }

            var tokenResult = await _tokenService.CreateTokenAsync(user);

            return Ok(tokenResult);
        }

        [Authorize]
        [HttpGet("me")]
        public ActionResult<CurrentUserDto> GetCurrentUser()
        {
            var userId = _currentUserService.UserId;

            var organizationId = _currentUserService.OrganizationId;

            var email = User.FindFirstValue(ClaimTypes.Email);

            if (userId is null ||
                email is null ||
                organizationId is null)
            {
                return Unauthorized();
            }

            var roles = User.FindAll(ClaimTypes.Role)
                .Select(claim => claim.Value)
                .ToList();

            var currentUserDto = new CurrentUserDto
            {
                Id = userId,
                Email = email,
                OrganizationId = organizationId.Value,
                Roles = roles
            };

            return Ok(currentUserDto);
        }
    }
}
