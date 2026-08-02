using Microsoft.EntityFrameworkCore;
using ServiceDesk.Api.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace ServiceDesk.Api.DbContexts;

public class ServiceDeskContext : IdentityDbContext<ApplicationUser>
{
    public ServiceDeskContext(
        DbContextOptions<ServiceDeskContext> options)
        : base(options)
    {
    }

    public DbSet<Organization> Organizations { get; set; } = null!;

    public DbSet<Ticket> Tickets { get; set; } = null!;

    public DbSet<TicketComment> TicketComments { get; set; } = null!;
}