using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceDesk.Api.DbContexts;
using ServiceDesk.Api.Models.TicketComments;
using ServiceDesk.Api.Entities;
using ServiceDesk.Api.Enums;

namespace ServiceDesk.Api.Controllers
{
    [ApiController]
    [Route("api/organizations/{organizationId:int}/tickets/{ticketId:int}/comments")]
    public class TicketCommentsController : ControllerBase
    {
        private readonly ServiceDeskContext _context;

        public TicketCommentsController(ServiceDeskContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TicketCommentDto>>> GetComments(
            int organizationId,
            int ticketId)
        {
            var ticketExists = await _context.Tickets
                .AnyAsync(ticket => ticket.Id == ticketId &&
                ticket.OrganizationId == organizationId);

            if (!ticketExists)
            {
                return NotFound();
            }

            var comments = await _context.TicketComments
                .AsNoTracking()
                .Where(comment => comment.TicketId == ticketId)
                .OrderBy(comment => comment.CreatedAtUtc)
                .Select(comment => new TicketCommentDto
                {
                    Id = comment.Id,
                    Text = comment.Text,
                    CreatedAtUtc = comment.CreatedAtUtc,
                    TicketId = comment.TicketId
                })
                .ToListAsync();

            return Ok(comments);


        }

        [HttpGet("{commentId:int}")]
        public async Task<ActionResult<TicketCommentDto>> GetComment(
            int organizationId,
            int ticketId,
            int commentId)
        {
            var comment = await _context.TicketComments
                .AsNoTracking()
                .Where(comment => comment.Id == commentId &&
                    comment.TicketId == ticketId &&
                    comment.Ticket.OrganizationId == organizationId)
                .Select(comment => new TicketCommentDto
                {
                    Id = comment.Id,
                    Text = comment.Text,
                    CreatedAtUtc = comment.CreatedAtUtc,
                    TicketId = comment.TicketId
                })
                .FirstOrDefaultAsync();
            if (comment is null)
            {
                return NotFound();
            }
            return Ok(comment);
        }

        [HttpPost]
        public async Task<ActionResult<TicketCommentDto>> CreateComment(
            int organizationId,
            int ticketId,
            CreateTicketCommentDto createTicketCommentDto)
        {
            var ticket = await _context.Tickets
                .FirstOrDefaultAsync(ticket => ticket.Id == ticketId &&
                    ticket.OrganizationId == organizationId);
            if (ticket is null)
            {
                return NotFound();
            }

            if (ticket.Status == TicketStatus.Closed)
            {
                return Conflict("Cannot add comments to a closed ticket.");
            }

            if (string.IsNullOrWhiteSpace(createTicketCommentDto.Text))
            {
                return BadRequest("Comment text cannot be empty.");
            }

            var comment = new TicketComment
            {
                Text = createTicketCommentDto.Text.Trim(),
                TicketId = ticketId
            };

            ticket.UpdatedAtUtc = DateTime.UtcNow;

            _context.TicketComments.Add(comment);
            await _context.SaveChangesAsync();
            var commentDto = new TicketCommentDto
            {
                Id = comment.Id,
                Text = comment.Text,
                CreatedAtUtc = comment.CreatedAtUtc,
                TicketId = comment.TicketId
            };
            return CreatedAtAction(
                nameof(GetComment),
                new { organizationId, ticketId, commentId = comment.Id },
                commentDto);
        }
    }
}
