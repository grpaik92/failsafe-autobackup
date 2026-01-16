using Microsoft.EntityFrameworkCore;

namespace FailsafeAutoBackup.BackendApi.Models;

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string ClioUserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    
    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
    public ICollection<Device> Devices { get; set; } = new List<Device>();
}

public class Subscription
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string StripeSubscriptionId { get; set; } = string.Empty;
    public string StripeCustomerId { get; set; } = string.Empty;
    public string Status { get; set; } = "active";
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime? EndDate { get; set; }
    public int MaxDevices { get; set; } = 1;
    
    public User User { get; set; } = null!;
}

public class Device
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string DeviceFingerprint { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
    public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    
    public User User { get; set; } = null!;
}

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Subscription> Subscriptions { get; set; }
    public DbSet<Device> Devices { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.ClioUserId)
            .IsUnique();

        modelBuilder.Entity<Device>()
            .HasIndex(d => d.DeviceFingerprint);

        modelBuilder.Entity<Subscription>()
            .HasIndex(s => s.StripeSubscriptionId)
            .IsUnique();
    }
}
