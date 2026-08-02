using System.ComponentModel.DataAnnotations;
using ServiceDesk.Api.Enums;

namespace ServiceDesk.Api.Models.Tickets
{
    public class UpdateTicketStatusDto
    {
        [Required]
        [EnumDataType(typeof(TicketStatus))]
        public TicketStatus? Status { get; set; }
    }
}
