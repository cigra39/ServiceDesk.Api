using Microsoft.EntityFrameworkCore;
using ServiceDesk.Api.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using ServiceDesk.Api.Services;
using ServiceDesk.Api.Authorization;

namespace ServiceDesk.Api.DbContexts;

public class ServiceDeskContext : IdentityDbContext<ApplicationUser>
{
    private readonly ICurrentUserService _currentUserService;
    public ServiceDeskContext(
        DbContextOptions<ServiceDeskContext> options,
        ICurrentUserService currentUserService)
        : base(options)
    {
        _currentUserService = currentUserService;
    }

    private int? CurrentOrganizationId =>
        _currentUserService.OrganizationId;

    private bool IsPlatformAdmin =>
        _currentUserService.IsInRole(RoleNames.PlatformAdmin);

    public DbSet<Organization> Organizations { get; set; } = null!;

    public DbSet<Ticket> Tickets { get; set; } = null!;

    public DbSet<TicketComment> TicketComments { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Organization>()
            .HasQueryFilter(organization =>
                IsPlatformAdmin ||
                (CurrentOrganizationId.HasValue &&
                organization.Id == CurrentOrganizationId.Value));

        modelBuilder.Entity<Ticket>()
            .HasQueryFilter(ticket =>
            CurrentOrganizationId.HasValue &&
            ticket.OrganizationId == CurrentOrganizationId.Value);

        modelBuilder.Entity<TicketComment>()
            .HasQueryFilter(comment =>
            CurrentOrganizationId.HasValue &&
            comment.Ticket.OrganizationId == CurrentOrganizationId.Value);
    }
}