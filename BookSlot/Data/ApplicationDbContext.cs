using BookSlot.Features.AiAssistant.Models;
using BookSlot.Models;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BookSlot.Data;

public class ApplicationUser : IdentityUser
{
    public string? FullName { get; set; }
    public Business? Business { get; set; }
}

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>, IDataProtectionKeyContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = null!;
    public DbSet<Business> Businesses { get; set; }
    public DbSet<Service> Services { get; set; }
    public DbSet<WorkSchedule> WorkSchedules { get; set; }
    public DbSet<Booking> Bookings { get; set; }
    public DbSet<Subscription> Subscriptions { get; set; }
    public DbSet<ManualBlock> ManualBlocks { get; set; }
    public DbSet<AiAssistantSettings> AiAssistantSettings { get; set; }
    public DbSet<TelegramBotConnection> TelegramBotConnections { get; set; }
    public DbSet<AiConversation> AiConversations { get; set; }
    public DbSet<AiConversationMessage> AiConversationMessages { get; set; }
    public DbSet<AppointmentDraft> AppointmentDrafts { get; set; }

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

        builder.Entity<ManualBlock>(e =>
        {
            e.HasOne(m => m.Business).WithMany(b => b.ManualBlocks).HasForeignKey(m => m.BusinessId);
            e.HasIndex(m => new { m.BusinessId, m.Date, m.BlockedTime });
        });

        builder.Entity<AiAssistantSettings>(e =>
        {
            e.HasOne<Business>().WithOne().HasForeignKey<AiAssistantSettings>(s => s.BusinessId);
            e.HasIndex(s => s.BusinessId).IsUnique();
            e.Property(s => s.WelcomeMessage).IsRequired().HasMaxLength(500);
            e.Property(s => s.BusinessDescription).HasMaxLength(1000);
            e.Property(s => s.ToneOfVoice).IsRequired().HasMaxLength(120);
        });

        builder.Entity<TelegramBotConnection>(e =>
        {
            e.HasOne<Business>().WithOne().HasForeignKey<TelegramBotConnection>(t => t.BusinessId);
            e.HasIndex(t => t.BusinessId).IsUnique();
            e.Property(t => t.BotUsername).IsRequired().HasMaxLength(100);
        });

        builder.Entity<AiConversation>(e =>
        {
            e.HasOne<Business>().WithMany().HasForeignKey(c => c.BusinessId);
            e.HasIndex(c => new { c.BusinessId, c.Channel, c.ExternalChatId }).IsUnique();
            e.Property(c => c.ExternalChatId).IsRequired().HasMaxLength(120);
            e.Property(c => c.CustomerName).HasMaxLength(200);
            e.Property(c => c.CustomerContact).HasMaxLength(200);
        });

        builder.Entity<AiConversationMessage>(e =>
        {
            e.HasOne(m => m.Conversation).WithMany(c => c.Messages).HasForeignKey(m => m.ConversationId);
            e.Property(m => m.Text).IsRequired().HasMaxLength(2000);
            e.HasIndex(m => new { m.ConversationId, m.CreatedAt });
        });

        builder.Entity<AppointmentDraft>(e =>
        {
            e.HasOne(d => d.Conversation).WithOne().HasForeignKey<AppointmentDraft>(d => d.ConversationId);
            e.HasOne<Service>().WithMany().HasForeignKey(d => d.ServiceId);
            e.HasIndex(d => d.ConversationId).IsUnique();
            e.Property(d => d.CustomerName).HasMaxLength(200);
            e.Property(d => d.CustomerContact).HasMaxLength(200);
        });
    }
}
