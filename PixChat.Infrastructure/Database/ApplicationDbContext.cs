using Microsoft.EntityFrameworkCore;
using PixChat.Core.Entities;
using PixChat.Infrastructure.Database.Configurations;

namespace PixChat.Infrastructure.Database;

public class ApplicationDbContext: DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
        
    }
    
    public DbSet<UserEntity> Users { get; set; }
    public DbSet<ContactEntity> Contacts { get; set; }
    public DbSet<OfflineMessageEntity> OfflineMessages { get; set; }
    public DbSet<FriendRequestEntity> FriendRequests { get; set; }
    public DbSet<ChatEntity> Chats { get; set; }
    public DbSet<ChatParticipantEntity> ChatParticipants { get; set; }
    public DbSet<OneTimeMessage> OneTimeMessages  { get; set; }
    public DbSet<UserKeyEntity> UserKeys { get; set; }
    public DbSet<OfflineMessageFileEntity> OfflineMessageFiles { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        modelBuilder.ApplyConfiguration(new UserEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new ContactEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new OfflineMessageEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new FriendRequestEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new ChatEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new ChatParticipantEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new OneTimeMessageEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new UserKeyEntityConfiguration());
        modelBuilder.ApplyConfiguration(new OfflineMessageFileEntityConfiguration());
    }
}