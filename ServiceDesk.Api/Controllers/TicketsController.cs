using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceDesk.Api.DbContexts;
using ServiceDesk.Api.Models.Tickets;
using ServiceDesk.Api.Entities;
using ServiceDesk.Api.Enums;

namespace ServiceDesk.Api.Controllers
{
    [ApiController]
    [Route("api/organizations/{organizationId:int}/tickets")]
    public class TicketsController : ControllerBase
    {
        private readonly ServiceDeskContext _context;

        public TicketsController(ServiceDeskContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TicketDto>>> GetTickets(
            int organizationId)
        {
            var organizationExists = await _context.Organizations
                .AnyAsync(organization =>
                    organization.Id == organizationId);

            if (!organizationExists)
            {
                return NotFound($"Organization with ID {organizationId} not found.");
            }


            var tickets = await _context.Tickets
                .AsNoTracking()
                .Where(ticket => ticket.OrganizationId == organizationId)
                .Select(ticket => new TicketDto
                {
                    Id = ticket.Id,
                    Title = ticket.Title,
                    Description = ticket.Description,
                    Status = ticket.Status,
                    Priority = ticket.Priority,
                    CreatedAtUtc = ticket.CreatedAtUtc,
                    UpdatedAtUtc = ticket.UpdatedAtUtc,
                    OrganizationId = ticket.OrganizationId
                })
                .ToListAsync();
            return Ok(tickets);
        }

        [HttpGet("{ticketId:int}")]
        public async Task<ActionResult<TicketDto>> GetTicket(
            int organizationId, int ticketId)
        {
            var ticket = await _context.Tickets
                .AsNoTracking()
                .Where(ticket =>
                    ticket.OrganizationId == organizationId &&
                    ticket.Id == ticketId)
                .Select(ticket => new TicketDto
                {
                    Id = ticket.Id,
                    Title = ticket.Title,
                    Description = ticket.Description,
                    Status = ticket.Status,
                    Priority = ticket.Priority,
                    CreatedAtUtc = ticket.CreatedAtUtc,
                    UpdatedAtUtc = ticket.UpdatedAtUtc,
                    OrganizationId = ticket.OrganizationId
                })
                .FirstOrDefaultAsync();
            if (ticket is null)
            {
                return NotFound($"Ticket with ID {ticketId} not found.");
            }
            return Ok(ticket);
        }

        [HttpPost]
        public async Task<ActionResult<TicketDto>> CreateTicket(
            int organizationId, CreateTicketDto createTicketDto)
        {
            var organizationExists = await _context.Organizations
                .AnyAsync(organization =>
                    organization.Id == organizationId);
            if (!organizationExists)
            {
                return NotFound($"Organization with ID {organizationId} not found.");
            }
            var ticket = new Ticket
            {
                Title = createTicketDto.Title.Trim(),
                Description = createTicketDto.Description.Trim(),
                Priority = createTicketDto.Priority,
                OrganizationId = organizationId
            };
            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();
            var ticketDto = new TicketDto
            {
                Id = ticket.Id,
                Title = ticket.Title,
                Description = ticket.Description,
                Status = ticket.Status,
                Priority = ticket.Priority,
                CreatedAtUtc = ticket.CreatedAtUtc,
                UpdatedAtUtc = ticket.UpdatedAtUtc,
                OrganizationId = ticket.OrganizationId
            };
            return CreatedAtAction(
                nameof(GetTicket),
                new { organizationId, ticketId = ticket.Id },
                ticketDto);
        }

        [HttpPut("{ticketId:int}")]
        public async Task<IActionResult> UpdateTicket(
            int organizationId,
            int ticketId,
            UpdateTicketDto updateTicketDto)
        {
            var ticket = await _context.Tickets
                .FirstOrDefaultAsync(ticket =>
                    ticket.OrganizationId == organizationId &&
                    ticket.Id == ticketId);
            if (ticket is null)
            {
                return NotFound($"Ticket with ID {ticketId} not found.");
            }
            ticket.Title = updateTicketDto.Title.Trim();
            ticket.Description = updateTicketDto.Description.Trim();
            ticket.Priority = updateTicketDto.Priority;
            ticket.UpdatedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpPatch("{ticketId:int}/status")]
        public async Task<IActionResult> UpdateTicketStatus(
            int organizationId,
            int ticketId,
            UpdateTicketStatusDto updateTicketStatusDto)
        {
            var ticket = await _context.Tickets
                .FirstOrDefaultAsync(ticket =>
                    ticket.OrganizationId == organizationId &&
                    ticket.Id == ticketId);
            if (ticket is null)
            {
                return NotFound($"Ticket with ID {ticketId} not found.");
            }

            if (updateTicketStatusDto.Status is not TicketStatus newStatus)
            {
                return BadRequest("Status is required.");
            }

            if (!IsValidStatusTransition(ticket.Status, newStatus))
            {
                return Conflict(new
                {
                    message = $"Ticket cannot move from {ticket.Status} to {newStatus}."
                });
            }
            ticket.Status = newStatus;
            ticket.UpdatedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        private static bool IsValidStatusTransition(
            TicketStatus currentStatus,
            TicketStatus newStatus)
        {
            if (currentStatus == newStatus)
            {
                return true;
            }

            return currentStatus switch
            {
                TicketStatus.Open =>
                newStatus == TicketStatus.InProgress,

                TicketStatus.InProgress =>
                newStatus is TicketStatus.Open
                or TicketStatus.Resolved,

                TicketStatus.Resolved =>
                newStatus is TicketStatus.InProgress
                or TicketStatus.Closed,

                TicketStatus.Closed => false,

                _ => false
            };
        }
    }
}
