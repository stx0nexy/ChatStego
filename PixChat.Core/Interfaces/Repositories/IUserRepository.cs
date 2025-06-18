using PixChat.Core.Entities;

namespace PixChat.Core.Interfaces.Repositories;

public interface IUserRepository
{
    Task<UserEntity> GetByIdAsync(int userId);

    Task<UserEntity> GetByUsernameAsync(string username);

    Task<UserEntity> GetByEmailAsync(string email);

    Task<IEnumerable<UserEntity>> GetAllAsync();

    Task AddAsync(UserEntity user);

    Task UpdateAsync(UserEntity user);

    Task DeleteAsync(int userId);

    Task<string> GetUserStatusAsync(int userId);

    Task UpdateUserStatusAsync(int userId, string status);

    Task<string> SaveUserImageAsync(int userId, Stream imageStream, string imageFileName);
}