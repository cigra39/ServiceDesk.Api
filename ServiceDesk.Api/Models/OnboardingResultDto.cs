using ServiceDesk.Api.Models.Organizations;

namespace ServiceDesk.Api.Models
{
    public class OnboardingResultDto
    {
        public OrganizationDto Organization { get; set; } = new();

        public RegisteredUserDto Owner { get; set; } = new();
    }
}
