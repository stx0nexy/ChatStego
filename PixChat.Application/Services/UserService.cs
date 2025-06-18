using AutoMapper;
using Microsoft.Extensions.Logging;
using PixChat.Application.DTOs;
using PixChat.Application.Interfaces.Services;
using PixChat.Core.Entities;
using PixChat.Core.Exceptions;
using PixChat.Core.Interfaces.Repositories;

namespace PixChat.Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IKeyService _keyService;
    private readonly IMapper _mapper;
    private readonly ILogger<UserService> _logger;


    public UserService(
        ILogger<UserService> logger,
        IUserRepository userRepository,
        IKeyService keyService,
        IMapper mapper)
    {
        _logger = logger;
        _userRepository = userRepository;
        _keyService = keyService;
        _mapper = mapper;
    }

    public async Task<UserDto?> GetByIdAsync(int userId)
    {
        try
        {
            var result = await _userRepository.GetByIdAsync(userId);
            return _mapper.Map<UserDto?>(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching user with ID: {UserId}.", userId);
            throw;
        }
    }

    public async Task<UserDto?> GetByUsernameAsync(string username)
    {
        try
        {
            var result = await _userRepository.GetByUsernameAsync(username);
            return _mapper.Map<UserDto?>(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching user with username: {Username}.", username);
            throw;
        }
    }

    public async Task<UserDto?> GetByEmailAsync(string email)
    {
        try
        {
            var result = await _userRepository.GetByEmailAsync(email);
            return _mapper.Map<UserDto?>(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching user with email: {Email}.", email);
            throw;
        }
    }
    
    public async Task<bool> UserExistsByEmailAsync(string email)
    {
        try
        {
            var user = await _userRepository.GetByEmailAsync(email);
            return user != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while checking if user exists by email: {Email}.", email);
            throw;
        }
    }

    public async Task<IEnumerable<UserDto>> GetAllAsync()
    {
        try
        {
            var result = await _userRepository.GetAllAsync();
            return result.Select(s => _mapper.Map<UserDto>(s)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching all users.");
            throw;
        }
    }

    public async Task AddAsync(UserDto user)
    {
        try
        {
            var userEntity = _mapper.Map<UserEntity>(user);
            if (string.IsNullOrEmpty(userEntity.ProfilePictureFileName))
            {
                userEntity.ProfilePictureFileName = "default_profile_picture.png";
            }

            await _userRepository.AddAsync(userEntity);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while adding user with username: {Username}.", user.Username);
            throw;
        }
    }

    public async Task UpdateAsync(UserDto user)
    {
        try
        {
            var userEntity = _mapper.Map<UserEntity>(user);
            await _userRepository.UpdateAsync(userEntity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating user with ID: {Id}.", user.Id);
            throw;
        }
    }

    public async Task DeleteAsync(int userId)
    {
        try
        {
            await _userRepository.DeleteAsync(userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting user with ID: {UserId}.", userId);
            throw;
        }
    }

    public async Task<string?> GetUserStatusAsync(int userId)
    {
        try
        {
            var status = await _userRepository.GetUserStatusAsync(userId);
            return status;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching user status for user ID: {UserId}.", userId);
            throw;
        }
    }

    public async Task UpdateUserStatusAsync(int userId, string status)
    {
        try
        {
            await _userRepository.UpdateUserStatusAsync(userId, status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating status for user ID: {UserId}.", userId);
            throw;
        }
    }

    public async Task<UserDto?> UploadUserProfilePictureAsync(int userId, Stream imageStream, string imageFileName)
    {
        try
        {
            var fileName = await _userRepository.SaveUserImageAsync(userId, imageStream, imageFileName);
            
            if (fileName == null)
            {
                _logger.LogWarning("Failed to save image or user not found for userId: {UserId}.", userId);
                throw new BusinessException($"Failed to upload profile picture for user {userId}. User not found or file save error.");
                return null;
            }

            var userEntity = await _userRepository.GetByIdAsync(userId);
            if (userEntity == null)
            {
                _logger.LogError("User {UserId} not found immediately after image save and update.", userId);
                throw new InvalidOperationException($"User {userId} not found after image upload process.");
            }
            return _mapper.Map<UserDto>(userEntity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while uploading profile picture for user ID: {UserId}.", userId);
            throw;
        }
    }
    
}