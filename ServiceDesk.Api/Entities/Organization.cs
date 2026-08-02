namespace ServiceDesk.Api.Entities
{
    public class Organization
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;


        //navigation property za tikete koji pripadaju organizaciji
        public ICollection<Ticket> Tickets { get; set; }
            = new List<Ticket>();


        //navigation property za korisnike koji pripadaju organizaciji
        public ICollection<ApplicationUser> Users { get; set; }
            = new List<ApplicationUser>();
    }
}
