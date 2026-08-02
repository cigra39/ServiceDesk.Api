namespace ServiceDesk.Api.Entities
{
    public class TicketComment
    {
        public int Id { get; set; }

        public string Text { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public int TicketId { get; set; }

        //navigation property za ticket kojem pripada komentar
        public Ticket Ticket { get; set; } = null!;
    }
}
