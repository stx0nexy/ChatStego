using PixChat.Application.DTOs;

namespace PixChat.Application.Interfaces.Services;

public interface IUserService
{
    Task<UserDto> GetByIdAsync(int userId);

    Task<UserDto> GetByUsernameAsync(string username);

    Task<UserDto> GetByEmailAsync(string email);
    Task<bool> UserExistsByEmailAsync(string email);

    Task<IEnumerable<UserDto>> GetAllAsync();

    Task AddAsync(UserDto user);

    Task UpdateAsync(UserDto user);

    Task DeleteAsync(int userId);

    Task<string> GetUserStatusAsync(int userId);

    Task UpdateUserStatusAsync(int userId, string status);
    
    Task<UserDto> UploadUserProfilePictureAsync(int userId, Stream imageStream, string imageFileName);
}