using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using ServiceDesk.Api.DbContexts;
using ServiceDesk.Api.Entities;
using Microsoft.EntityFrameworkCore;
using ServiceDesk.Api.Models;
using ServiceDesk.Api.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace ServiceDesk.Api.Controllers
{
    [ApiController]
    [Route("api/authentication")]
    public class AuthenticationController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ServiceDeskContext _context;
        private readonly ITokenService _tokenService;

        public AuthenticationController(
            UserManager<ApplicationUser> userManager,
            ServiceDeskContext context,
            ITokenService tokenService)
        {
            _userManager = userManager;
            _context = context;
            _tokenService = tokenService;
        }

        [HttpPost("register")]
        public async Task<ActionResult<RegisteredUserDto>> Register(
            RegisterUserDto registerUserDto)
        {
            var organizationExists = await _context.Organizations
                .AnyAsync(organization =>
                organization.Id == registerUserDto.OrganizationId);

            if (!organizationExists)
            {
                return BadRequest("Organization does not exist.");
            }

            var user = new ApplicationUser
            {
                UserName = registerUserDto.Email.Trim(),
                Email = registerUserDto.Email.Trim(),
                FirstName = registerUserDto.FirstName.Trim(),
                LastName = registerUserDto.LastName.Trim(),
                OrganizationId = registerUserDto.OrganizationId,
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
                user, "Customer");

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
                Role = "Customer"
            };

            return StatusCode(StatusCodes.Status201Created, registeredUserDto);
        }

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
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var email = User.FindFirstValue(ClaimTypes.Email);

            var organizationIdValue = User.FindFirstValue("organizationId");

            if (userId is null ||
                email is null ||
                !int.TryParse(organizationIdValue, out var organizationId))
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
                OrganizationId = organizationId,
                Roles = roles
            };

            return Ok(currentUserDto);
        }
    }
}
