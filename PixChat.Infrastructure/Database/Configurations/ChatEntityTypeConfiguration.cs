using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PixChat.Core.Entities;

namespace PixChat.Infrastructure.Database.Configurations;

public class ChatEntityTypeConfiguration : IEntityTypeConfiguration<ChatEntity>
{
    public void Configure(EntityTypeBuilder<ChatEntity> builder)
    {
        builder.ToTable("Chats");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .HasMaxLength(255)
            .IsRequired(false);

        builder.Property(c => c.Description)
            .HasMaxLength(1000);

        builder.Property(c => c.IsGroup)
            .IsRequired();

        builder.Property(c => c.CreatedAt)
            .IsRequired();

        builder.Property(c => c.UpdatedAt)
            .IsRequired();

        builder.HasOne(c => c.Creator)
            .WithMany()
            .HasForeignKey(c => c.CreatorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(c => c.Participants)
            .WithOne(p => p.Chat)
            .HasForeignKey(p => p.ChatId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}