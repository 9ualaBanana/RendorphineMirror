using Microsoft.EntityFrameworkCore;

namespace StatusNotifier;

public class NotificationDbContext : DbContext
{
    public DbSet<Notification> Notifications { get; set; } = null!;

    public NotificationDbContext(DbContextOptions options) : base(options) { }
}
