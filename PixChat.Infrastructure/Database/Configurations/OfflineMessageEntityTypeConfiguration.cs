using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PixChat.Core.Entities;

namespace PixChat.Infrastructure.Database.Configurations;

public class OfflineMessageEntityTypeConfiguration: IEntityTypeConfiguration<OfflineMessageEntity>
{
    public void Configure(EntityTypeBuilder<OfflineMessageEntity> builder)
    {

        builder.ToTable("OfflineMessages");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Id)
            .IsRequired()
            .HasMaxLength(36);

        builder.Property(o => o.SenderId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(o => o.ReceiverId)
            .HasMaxLength(50);

        builder.Property(o => o.ChatId)
            .IsRequired();

        builder.Property(o => o.StegoImage)
            .IsRequired();

        builder.Property(o => o.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
        
    }
}

