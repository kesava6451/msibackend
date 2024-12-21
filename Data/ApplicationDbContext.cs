using Microsoft.EntityFrameworkCore;

public class ApplicationDbContext : DbContext
{
    // Constructor that accepts DbContextOptions
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    // DbSet for Users
    public DbSet<User> Users { get; set; }

    // Configure the model using OnModelCreating
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Explicitly define the primary key (optional, Entity Framework should auto-detect this)
        modelBuilder.Entity<User>()
            .HasKey(u => u.Id); // Ensure that 'Id' is recognized as the primary key

        // Additional configurations can be added here if necessary
    }
}
