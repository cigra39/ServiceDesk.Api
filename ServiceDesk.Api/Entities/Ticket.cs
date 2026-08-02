using ServiceDesk.Api.Enums;
namespace ServiceDesk.Api.Entities
{
    public class Ticket
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public TicketStatus Status { get; set; } = TicketStatus.Open;

        public TicketPriority Priority { get; set; } = TicketPriority.Medium;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAtUtc { get; set; }

        public int OrganizationId { get; set; }


        //navigation property za organizaciju kojoj pripada ticket
        public Organization Organization { get; set; } = null!;


        //navigation property za komentare koji pripadaju ticketu
        public ICollection<TicketComment> Comments { get; set; }
        = new List<TicketComment>();
    }
}
