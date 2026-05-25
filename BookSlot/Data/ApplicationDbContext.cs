using BookSlot.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BookSlot.Data;

public class ApplicationUser : IdentityUser
{
    public string? FullName { get; set; }
    public Business? Business { get; set; }
}

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Business> Businesses { get; set; }
    public DbSet<Service> Services { get; set; }
    public DbSet<WorkSchedule> WorkSchedules { get; set; }
    public DbSet<Booking> Bookings { get; set; }
    public DbSet<Subscription> Subscriptions { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Business>(e =>
        {
            e.HasOne<ApplicationUser>().WithOne(u => u.Business)
                .HasForeignKey<Business>(b => b.UserId);
            e.HasIndex(b => b.Slug).IsUnique();
            e.Property(b => b.Name).IsRequired().HasMaxLength(200);
            e.Property(b => b.Slug).IsRequired().HasMaxLength(100);
        });

        builder.Entity<Service>(e =>
        {
            e.HasOne(s => s.Business).WithMany(b => b.Services).HasForeignKey(s => s.BusinessId);
            e.Property(s => s.Price).HasColumnType("decimal(10,2)");
        });

        builder.Entity<WorkSchedule>(e =>
        {
            e.HasOne(w => w.Business).WithMany(b => b.WorkSchedules).HasForeignKey(w => w.BusinessId);
        });

        builder.Entity<Booking>(e =>
        {
            e.HasOne(b => b.Business).WithMany(bus => bus.Bookings).HasForeignKey(b => b.BusinessId);
            e.HasOne(b => b.Service).WithMany(s => s.Bookings).HasForeignKey(b => b.ServiceId);
        });

        builder.Entity<Subscription>(e =>
        {
            e.HasOne(s => s.Business).WithOne(b => b.Subscription).HasForeignKey<Subscription>(s => s.BusinessId);
        });
    }
}
