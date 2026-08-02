using ServiceDesk.Api.Enums;

namespace ServiceDesk.Api.Models.Tickets
{
    public class TicketDto
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public TicketStatus Status { get; set; }

        public TicketPriority Priority { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public DateTime? UpdatedAtUtc { get; set; }

        public int OrganizationId { get; set; }
    }
}
