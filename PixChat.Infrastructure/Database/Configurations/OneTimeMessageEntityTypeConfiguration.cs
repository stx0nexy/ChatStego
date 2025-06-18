using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PixChat.Core.Entities;

namespace PixChat.Infrastructure.Database.Configurations;

public class OneTimeMessageEntityTypeConfiguration : IEntityTypeConfiguration<OneTimeMessage>
{
    public void Configure(EntityTypeBuilder<OneTimeMessage> builder)
    {
        builder.ToTable("OneTimeMessages");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Id)
            .IsRequired()
            .HasMaxLength(36);

        builder.Property(o => o.SenderId)
            .HasMaxLength(50);

        builder.Property(o => o.ReceiverId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(o => o.ChatId)
            .IsRequired();

        builder.Property(o => o.StegoImage)
            .IsRequired();

        builder.Property(o => o.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(o => o.Received)
            .IsRequired();
    }
}