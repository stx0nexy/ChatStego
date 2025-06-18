using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PixChat.Core.Entities;
using PixChat.Core.Interfaces;
using PixChat.Core.Interfaces.Repositories;
using PixChat.Infrastructure.Database;
using PixChat.Infrastructure.ExternalServices;

namespace PixChat.Infrastructure.Repositories;

public class UserRepository : BaseDataService, IUserRepository
{
    private readonly string _imageDirectory;
    public UserRepository(
        IDbContextWrapper<ApplicationDbContext> dbContextWrapper,
        ILogger<UserRepository> logger,
        string imageDirectory) : base(dbContextWrapper, logger)
    {
        _imageDirectory = imageDirectory;
    }

    public async Task<UserEntity?> GetByIdAsync(int userId)
    {
        return await ExecuteSafeAsync(async () =>
        {
            return await Context.Users.FindAsync(userId);
        });
    }

    public async Task<UserEntity?> GetByUsernameAsync(string username)
    {
        return await ExecuteSafeAsync(async () =>
        {
            return await Context.Users
                .FirstOrDefaultAsync(u => u.Username == username);
        });
    }

    public async Task<UserEntity?> GetByEmailAsync(string email)
    {
        return await ExecuteSafeAsync(async () =>
        {
            return await Context.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == email);
        });
    }

    public async Task<IEnumerable<UserEntity>> GetAllAsync()
    {
        return await ExecuteSafeAsync(async () =>
        {
            return await Context.Users.ToListAsync();
        });
    }

    public async Task AddAsync(UserEntity user)
    {
        await ExecuteSafeAsync(async () =>
        {
            await Context.Users.AddAsync(user);
            await Context.SaveChangesAsync();
        });
    }

    public async Task UpdateAsync(UserEntity user)
    {
        await ExecuteSafeAsync(async () =>
        {
            Context.Users.Update(user);
            await Context.SaveChangesAsync();
        });
    }

    public async Task DeleteAsync(int userId)
    {
        await ExecuteSafeAsync(async () =>
        {
            var user = await Context.Users.FindAsync(userId);

            if (user != null)
            {
                Context.Users.Remove(user);
                await Context.SaveChangesAsync();
            }
            else
            {
                _logger.LogWarning("User with ID {UserId} not found for deletion.", userId);
            }
        });
    }

    public async Task<string?> GetUserStatusAsync(int userId)
    {
        return await ExecuteSafeAsync(async () =>
        {
            var user = await Context.Users.FindAsync(userId);
            return user?.Status.ToString();
        });
    }

    public async Task UpdateUserStatusAsync(int userId, string status)
    {
        await ExecuteSafeAsync(async () =>
        {
            var user = await Context.Users.FindAsync(userId);

            if (user != null)
            {
                user.Status = bool.Parse(status);
                await Context.SaveChangesAsync();
            }
            else
            {
                _logger.LogWarning("User with ID {UserId} not found for status update.", userId);
            }
        });
    }

    public async Task<string?> SaveUserImageAsync(int userId, Stream imageStream, string imageFileName)
    {
        string? uniqueFileName = null;

        try
        {
            var user = await GetByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User with ID {UserId} not found for image save.", userId);
                return null;
            }

            uniqueFileName = $"{Guid.NewGuid()}_{imageFileName}";
            var filePath = Path.Combine(_imageDirectory, uniqueFileName);

            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await imageStream.CopyToAsync(fileStream);
            }

            await ExecuteSafeAsync(async () =>
            {
                var userToUpdate = await Context.Users.FindAsync(userId);
                if (userToUpdate != null)
                {
                    userToUpdate.ProfilePictureFileName = uniqueFileName;
                    await Context.SaveChangesAsync();
                }
                else
                {
                    _logger.LogError("User disappeared during image save transaction: {UserId}", userId);
                    throw new InvalidOperationException($"User with ID {userId} not found for image update.");
                }
            });

            _logger.LogInformation($"User image saved and updated for userId: {userId}, filename: {uniqueFileName}");
            return uniqueFileName;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error saving user image for userId: {userId}");
            if (!string.IsNullOrEmpty(uniqueFileName))
            {
                var tempFilePath = Path.Combine(_imageDirectory, uniqueFileName);
                if (File.Exists(tempFilePath))
                {
                    // File.Delete(tempFilePath);
                    _logger.LogWarning("Saved file {FileName} might be orphaned due to DB update failure.", uniqueFileName);
                }
            }
            throw;
        }
    }
}