using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PixChat.Core.Entities;

namespace PixChat.Infrastructure.Database.Configurations;

public class FriendRequestEntityTypeConfiguration: IEntityTypeConfiguration<FriendRequestEntity>
{
    public void Configure(EntityTypeBuilder<FriendRequestEntity> builder)
    {
        builder.HasKey(c => c.Id);
        
        builder.Property(fr => fr.Id)
            .IsRequired()
            .ValueGeneratedOnAdd();
        
        builder.Property(fr => fr.UserId)
            .IsRequired();
        
        builder.Property(fr => fr.ContactUserId)
            .IsRequired();
        
        builder.Property(fr => fr.Status)
            .IsRequired();
    }
}