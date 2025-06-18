using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PixChat.Core.Entities;

namespace PixChat.Infrastructure.Database.Configurations;

public class UserKeyEntityConfiguration : IEntityTypeConfiguration<UserKeyEntity>
{
    public void Configure(EntityTypeBuilder<UserKeyEntity> builder)
    {
        builder.ToTable("UserKeys");

        builder.HasKey(k => k.Id);

        builder.Property(k => k.Id)
            .HasColumnName("Id")
            .ValueGeneratedOnAdd();

        builder.Property(k => k.UserId)
            .HasColumnName("UserId")
            .IsRequired();

        builder.Property(k => k.PublicKey)
            .HasColumnName("PublicKey")
            .IsRequired();

        builder.HasIndex(k => k.UserId)
            .IsUnique();

        builder.HasOne(k => k.User)
            .WithOne(u => u.UserKey)
            .HasForeignKey<UserKeyEntity>(k => k.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}