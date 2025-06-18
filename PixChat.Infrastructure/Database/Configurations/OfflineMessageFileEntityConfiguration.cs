using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PixChat.Core.Entities;

namespace PixChat.Infrastructure.Database.Configurations;

public class OfflineMessageFileEntityConfiguration : IEntityTypeConfiguration<OfflineMessageFileEntity>
{
    public void Configure(EntityTypeBuilder<OfflineMessageFileEntity> builder)
    {
        builder.ToTable("OfflineMessageFiles");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
            .HasColumnName("Id")
            .IsRequired();

        builder.Property(m => m.SenderId)
            .HasColumnName("SenderId")
            .IsRequired();

        builder.Property(m => m.ReceiverId)
            .HasColumnName("ReceiverId")
            .IsRequired(false);

        builder.Property(m => m.ChatId)
            .HasColumnName("ChatId")
            .IsRequired();

        builder.Property(m => m.CreatedAt)
            .HasColumnName("CreatedAt")
            .IsRequired();

        builder.Property(m => m.Received)
            .HasColumnName("Received")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(m => m.IsGroup)
            .HasColumnName("IsGroup")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(m => m.IsFile)
            .HasColumnName("IsFile")
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(m => m.FileData)
            .HasColumnName("FileData")
            .IsRequired();

        builder.Property(m => m.FileName)
            .HasColumnName("FileName")
            .IsRequired();

        builder.Property(m => m.FileType)
            .HasColumnName("FileType")
            .IsRequired();

        builder.Property(m => m.EncryptedAESKey)
            .HasColumnName("EncryptedAESKey")
            .IsRequired();

        builder.Property(m => m.AESIV)
            .HasColumnName("AESIV")
            .IsRequired();

        builder.HasIndex(m => m.SenderId);
        builder.HasIndex(m => m.ReceiverId);
        builder.HasIndex(m => m.ChatId);
    }
}