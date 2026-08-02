using System.ComponentModel.DataAnnotations;
using ServiceDesk.Api.Enums;

namespace ServiceDesk.Api.Models.Tickets
{
    public class CreateTicketDto
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(4000)]
        public string Description { get; set; } = string.Empty;

        public TicketPriority Priority { get; set; }
            = TicketPriority.Medium;
    }
}
