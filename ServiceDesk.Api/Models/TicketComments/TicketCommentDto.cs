namespace ServiceDesk.Api.Models.TicketComments
{
    public class TicketCommentDto
    {
        public int Id { get; set; }

        public string Text { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; }

        public int TicketId { get; set; }
    }
}
