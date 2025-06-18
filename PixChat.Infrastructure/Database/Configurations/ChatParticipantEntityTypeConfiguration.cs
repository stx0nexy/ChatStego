using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PixChat.Core.Entities;

namespace PixChat.Infrastructure.Database.Configurations;

public class ChatParticipantEntityTypeConfiguration : IEntityTypeConfiguration<ChatParticipantEntity>
{
    public void Configure(EntityTypeBuilder<ChatParticipantEntity> builder)
    {
        builder.ToTable("ChatParticipants");

        builder.HasKey(cp => cp.Id);

        builder.Property(cp => cp.IsAdmin)
            .IsRequired();

        builder.Property(cp => cp.JoinedAt)
            .IsRequired();

        builder.HasOne(cp => cp.Chat)
            .WithMany(c => c.Participants)
            .HasForeignKey(cp => cp.ChatId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(cp => cp.User)
            .WithMany(u => u.ChatParticipations)
            .HasForeignKey(cp => cp.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}