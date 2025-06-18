using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PixChat.Core.Entities;
using PixChat.Core.Interfaces;
using PixChat.Core.Interfaces.Repositories;
using PixChat.Infrastructure.Database;
using PixChat.Infrastructure.ExternalServices;

namespace PixChat.Infrastructure.Repositories;

public class UserKeyRepository : BaseDataService, IUserKeyRepository
{
    public UserKeyRepository(
        IDbContextWrapper<ApplicationDbContext> dbContextWrapper,
        ILogger<UserKeyRepository> logger) : base(dbContextWrapper, logger)
    {
    }
    
    public async Task<string?> GetPublicKeyAsync(int userId)
    {
        return await ExecuteSafeAsync(async () =>
        {
            var user = await Context.UserKeys.FirstOrDefaultAsync(u => u.UserId == userId);
            return user?.PublicKey;
        });
    }

    public async Task SavePublicKeyAsync(int userId, string publicKey)
    {
        await ExecuteSafeAsync(async () =>
        {
            var user = await Context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                throw new ArgumentException($"User with ID {userId} not found.");
            }

            var existingKey = await Context.UserKeys.FirstOrDefaultAsync(k => k.UserId == user.Id);
            if (existingKey != null)
            {
                existingKey.PublicKey = publicKey;
                Context.UserKeys.Update(existingKey);
            }
            else
            {
                Context.UserKeys.Add(new UserKeyEntity { UserId = user.Id, PublicKey = publicKey });
            }

            await Context.SaveChangesAsync();

        });
    }
}