using System.ComponentModel.DataAnnotations;

namespace ServiceDesk.Api.Models.TicketComments
{
    public class CreateTicketCommentDto
    {
        [Required]
        [MaxLength(2000)]
        public string Text { get; set; } = string.Empty;
    }
}
