using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using System.Text;
using AutoMapper;
using PixChat.Application.DTOs;
using PixChat.Application.Interfaces.Services;
using PixChat.Application.Services;
using PixChat.Core.Entities;
using PixChat.Core.Exceptions;
using PixChat.Core.Interfaces.Repositories;

namespace PixChat.Tests;

public class UserServiceTests
{
    private readonly Mock<ILogger<UserService>> _loggerMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IKeyService> _keyServiceMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly UserService _userService;

    public UserServiceTests()
    {
        _loggerMock = new Mock<ILogger<UserService>>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _keyServiceMock = new Mock<IKeyService>();
        _mapperMock = new Mock<IMapper>();

        _userService = new UserService(
            _loggerMock.Object,
            _userRepositoryMock.Object,
            _keyServiceMock.Object,
            _mapperMock.Object
        );
    }

    private UserDto CreateDummyUserDto(int id = 1, string username = "testuser", string email = "test@example.com")
    {
        return new UserDto { Id = id, Username = username, Email = email, ProfilePictureUrl = "test.png" };
    }

    private UserEntity CreateDummyUserEntity(int id = 1, string username = "testuser", string email = "test@example.com")
    {
        return new UserEntity { Id = id, Username = username, Email = email, ProfilePictureFileName = "test.png" };
    }
    
    [Fact]
    public async Task GetByIdAsync_UserExists_ReturnsUserDto()
    {
        // Arrange
        var userId = 1;
        var userEntity = CreateDummyUserEntity(userId);
        var userDto = CreateDummyUserDto(userId);

        _userRepositoryMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(userEntity);
        _mapperMock.Setup(m => m.Map<UserDto?>(userEntity)).Returns(userDto);

        // Act
        var result = await _userService.GetByIdAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.Id);
        _userRepositoryMock.Verify(r => r.GetByIdAsync(userId), Times.Once);
        _mapperMock.Verify(m => m.Map<UserDto?>(userEntity), Times.Once);
        _loggerMock.Verify(
            x => x.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
            Times.Never
        );
    }

    [Fact]
    public async Task GetByIdAsync_UserDoesNotExist_ReturnsNull()
    {
        // Arrange
        var userId = 99;
        _userRepositoryMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync((UserEntity?)null);
        _mapperMock.Setup(m => m.Map<UserDto?>(It.IsAny<UserEntity>())).Returns((UserDto?)null);

        // Act
        var result = await _userService.GetByIdAsync(userId);

        // Assert
        Assert.Null(result);
        _userRepositoryMock.Verify(r => r.GetByIdAsync(userId), Times.Once);
        _mapperMock.Verify(m => m.Map<UserDto?>(It.IsAny<UserEntity>()), Times.Once);
        _loggerMock.Verify(
            x => x.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(), (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
            Times.Never
        );
    }

    [Fact]
    public async Task GetByIdAsync_RepositoryThrowsException_ThrowsExceptionAndLogsError()
    {
        // Arrange
        var userId = 1;
        var expectedException = new InvalidOperationException("DB error");
        _userRepositoryMock.Setup(r => r.GetByIdAsync(userId)).ThrowsAsync(expectedException);

        // Act & Assert
        var caughtException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _userService.GetByIdAsync(userId)
        );

        Assert.Equal(expectedException, caughtException);
        _userRepositoryMock.Verify(r => r.GetByIdAsync(userId), Times.Once);
        _mapperMock.Verify(m => m.Map<UserDto?>(It.IsAny<UserEntity>()), Times.Never);
        _loggerMock.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Error),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains($"Error occurred while fetching user with ID: {userId}.")),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
            Times.Once
        );
    }

    [Fact]
    public async Task GetByUsernameAsync_UserExists_ReturnsUserDto()
    {
        // Arrange
        string username = "existinguser";
        var userEntity = CreateDummyUserEntity(username: username);
        var userDto = CreateDummyUserDto(username: username);

        _userRepositoryMock.Setup(r => r.GetByUsernameAsync(username)).ReturnsAsync(userEntity);
        _mapperMock.Setup(m => m.Map<UserDto?>(userEntity)).Returns(userDto);

        // Act
        var result = await _userService.GetByUsernameAsync(username);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(username, result.Username);
    }

    [Fact]
    public async Task GetByUsernameAsync_RepositoryThrowsException_ThrowsExceptionAndLogsError()
    {
        // Arrange
        string username = "erroruser";
        var expectedException = new TimeoutException("Network issue");
        _userRepositoryMock.Setup(r => r.GetByUsernameAsync(username)).ThrowsAsync(expectedException);

        // Act & Assert
        var caughtException = await Assert.ThrowsAsync<TimeoutException>(
            () => _userService.GetByUsernameAsync(username)
        );

        Assert.Equal(expectedException, caughtException);
        _loggerMock.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Error),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains($"Error occurred while fetching user with username: {username}.")),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
            Times.Once
        );
    }

    [Fact]
    public async Task GetByEmailAsync_UserExists_ReturnsUserDto()
    {
        // Arrange
        string email = "existing@example.com";
        var userEntity = CreateDummyUserEntity(email: email);
        var userDto = CreateDummyUserDto(email: email);

        _userRepositoryMock.Setup(r => r.GetByEmailAsync(email)).ReturnsAsync(userEntity);
        _mapperMock.Setup(m => m.Map<UserDto?>(userEntity)).Returns(userDto);

        // Act
        var result = await _userService.GetByEmailAsync(email);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(email, result.Email);
    }
    
    [Fact]
    public async Task GetByEmailAsync_RepositoryThrowsException_ThrowsExceptionAndLogsError()
    {
        // Arrange
        string email = "error@example.com";
        var expectedException = new ApplicationException("Repo down");
        _userRepositoryMock.Setup(r => r.GetByEmailAsync(email)).ThrowsAsync(expectedException);

        // Act & Assert
        var caughtException = await Assert.ThrowsAsync<ApplicationException>(
            () => _userService.GetByEmailAsync(email)
        );

        Assert.Equal(expectedException, caughtException);
        _loggerMock.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Error),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains($"Error occurred while fetching user with email: {email}.")),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
            Times.Once
        );
    }

    [Fact]
    public async Task UserExistsByEmailAsync_UserExists_ReturnsTrue()
    {
        // Arrange
        string email = "exists@example.com";
        var userEntity = CreateDummyUserEntity(email: email);
        _userRepositoryMock.Setup(r => r.GetByEmailAsync(email)).ReturnsAsync(userEntity);

        // Act
        var result = await _userService.UserExistsByEmailAsync(email);

        // Assert
        Assert.True(result);
        _userRepositoryMock.Verify(r => r.GetByEmailAsync(email), Times.Once);
    }

    [Fact]
    public async Task UserExistsByEmailAsync_UserDoesNotExist_ReturnsFalse()
    {
        // Arrange
        string email = "nonexistent@example.com";
        _userRepositoryMock.Setup(r => r.GetByEmailAsync(email)).ReturnsAsync((UserEntity?)null);

        // Act
        var result = await _userService.UserExistsByEmailAsync(email);

        // Assert
        Assert.False(result);
        _userRepositoryMock.Verify(r => r.GetByEmailAsync(email), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_UsersExist_ReturnsMappedUserDtos()
    {
        // Arrange
        var userEntities = new List<UserEntity> { CreateDummyUserEntity(1), CreateDummyUserEntity(2) };
        var userDtos = new List<UserDto> { CreateDummyUserDto(1), CreateDummyUserDto(2) };

        _userRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(userEntities);
        _mapperMock.Setup(m => m.Map<UserDto>(It.IsAny<UserEntity>()))
                   .Returns((UserEntity src) => CreateDummyUserDto(src.Id));

        // Act
        var result = await _userService.GetAllAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userEntities.Count, result.Count());
        _userRepositoryMock.Verify(r => r.GetAllAsync(), Times.Once);
        _mapperMock.Verify(m => m.Map<UserDto>(It.IsAny<UserEntity>()), Times.Exactly(userEntities.Count));
    }

    [Fact]
    public async Task GetAllAsync_NoUsersExist_ReturnsEmptyList()
    {
        // Arrange
        _userRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<UserEntity>());

        // Act
        var result = await _userService.GetAllAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
        _userRepositoryMock.Verify(r => r.GetAllAsync(), Times.Once);
        _mapperMock.Verify(m => m.Map<UserDto>(It.IsAny<UserEntity>()), Times.Never);
    }

    [Fact]
    public async Task AddAsync_ValidUser_CallsRepositoryWithDefaultProfilePictureWhenNotProvided()
    {
        // Arrange
        var userDto = CreateDummyUserDto();
        userDto.ProfilePictureUrl = null;
        var userEntity = CreateDummyUserEntity();
        userEntity.ProfilePictureFileName = "default_profile_picture.png";

        _mapperMock.Setup(m => m.Map<UserEntity>(userDto)).Returns(userEntity);
        _userRepositoryMock.Setup(r => r.AddAsync(userEntity)).Returns(Task.CompletedTask);

        // Act
        await _userService.AddAsync(userDto);

        // Assert
        _mapperMock.Verify(m => m.Map<UserEntity>(userDto), Times.Once);
        _userRepositoryMock.Verify(r => r.AddAsync(
            It.Is<UserEntity>(u => u.ProfilePictureFileName == "default_profile_picture.png")),
            Times.Once
        );
        _loggerMock.Verify(
            x => x.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
            Times.Never
        );
    }

    [Fact]
    public async Task AddAsync_RepositoryThrowsException_ThrowsExceptionAndLogsError()
    {
        // Arrange
        var userDto = CreateDummyUserDto();
        var userEntity = CreateDummyUserEntity();
        var expectedException = new ArgumentException("Invalid user data");
        _mapperMock.Setup(m => m.Map<UserEntity>(userDto)).Returns(userEntity);
        _userRepositoryMock.Setup(r => r.AddAsync(userEntity)).ThrowsAsync(expectedException);

        // Act & Assert
        var caughtException = await Assert.ThrowsAsync<ArgumentException>(
            () => _userService.AddAsync(userDto)
        );

        Assert.Equal(expectedException, caughtException);
        _loggerMock.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Error),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains($"Error occurred while adding user with username: {userDto.Username}.")),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
            Times.Once
        );
    }

    [Fact]
    public async Task UpdateAsync_ValidUser_CallsRepository()
    {
        // Arrange
        var userDto = CreateDummyUserDto();
        var userEntity = CreateDummyUserEntity();
        _mapperMock.Setup(m => m.Map<UserEntity>(userDto)).Returns(userEntity);
        _userRepositoryMock.Setup(r => r.UpdateAsync(userEntity)).Returns(Task.CompletedTask);

        // Act
        await _userService.UpdateAsync(userDto);

        // Assert
        _mapperMock.Verify(m => m.Map<UserEntity>(userDto), Times.Once);
        _userRepositoryMock.Verify(r => r.UpdateAsync(userEntity), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_RepositoryThrowsException_ThrowsExceptionAndLogsError()
    {
        // Arrange
        var userDto = CreateDummyUserDto();
        var userEntity = CreateDummyUserEntity();
        var expectedException = new InvalidOperationException("Update failed");
        _mapperMock.Setup(m => m.Map<UserEntity>(userDto)).Returns(userEntity);
        _userRepositoryMock.Setup(r => r.UpdateAsync(userEntity)).ThrowsAsync(expectedException);

        // Act & Assert
        var caughtException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _userService.UpdateAsync(userDto)
        );

        Assert.Equal(expectedException, caughtException);
        _loggerMock.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Error),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains($"Error occurred while updating user with ID: {userDto.Id}.")),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
            Times.Once
        );
    }

    [Fact]
    public async Task DeleteAsync_ValidUserId_CallsRepository()
    {
        // Arrange
        var userId = 1;
        _userRepositoryMock.Setup(r => r.DeleteAsync(userId)).Returns(Task.CompletedTask);

        // Act
        await _userService.DeleteAsync(userId);

        // Assert
        _userRepositoryMock.Verify(r => r.DeleteAsync(userId), Times.Once);
    }

    [Fact]
    public async Task GetUserStatusAsync_UserExists_ReturnsStatus()
    {
        // Arrange
        var userId = 1;
        string status = "Online";
        _userRepositoryMock.Setup(r => r.GetUserStatusAsync(userId)).ReturnsAsync(status);

        // Act
        var result = await _userService.GetUserStatusAsync(userId);

        // Assert
        Assert.Equal(status, result);
        _userRepositoryMock.Verify(r => r.GetUserStatusAsync(userId), Times.Once);
    }

    [Fact]
    public async Task UpdateUserStatusAsync_ValidData_CallsRepository()
    {
        // Arrange
        var userId = 1;
        string status = "Away";
        _userRepositoryMock.Setup(r => r.UpdateUserStatusAsync(userId, status)).Returns(Task.CompletedTask);

        // Act
        await _userService.UpdateUserStatusAsync(userId, status);

        // Assert
        _userRepositoryMock.Verify(r => r.UpdateUserStatusAsync(userId, status), Times.Once);
    }

    [Fact]
    public async Task UploadUserProfilePictureAsync_ValidImage_ReturnsUserDto()
    {
        // Arrange
        var userId = 1;
        var imageFileName = "profile.png";
        var imageStream = new MemoryStream(Encoding.UTF8.GetBytes("dummy_image_data"));
        var savedFileName = "saved_profile_1.png";
        var userEntityAfterSave = CreateDummyUserEntity(userId);
        userEntityAfterSave.ProfilePictureFileName = savedFileName;
        var userDtoAfterMap = CreateDummyUserDto(userId);
        userDtoAfterMap.ProfilePictureUrl = savedFileName;

        _userRepositoryMock.Setup(r => r.SaveUserImageAsync(userId, imageStream, imageFileName)).ReturnsAsync(savedFileName);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(userEntityAfterSave);
        _mapperMock.Setup(m => m.Map<UserDto>(userEntityAfterSave)).Returns(userDtoAfterMap);

        // Act
        var result = await _userService.UploadUserProfilePictureAsync(userId, imageStream, imageFileName);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.Id);
        Assert.Equal(savedFileName, result.ProfilePictureUrl);
        _userRepositoryMock.Verify(r => r.SaveUserImageAsync(userId, imageStream, imageFileName), Times.Once);
        _userRepositoryMock.Verify(r => r.GetByIdAsync(userId), Times.Once);
        _mapperMock.Verify(m => m.Map<UserDto>(userEntityAfterSave), Times.Once);
        _loggerMock.Verify(
            x => x.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
            Times.Never
        );
        _loggerMock.Verify(
            x => x.Log(LogLevel.Warning, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
            Times.Never
        );
    }

    [Fact]
    public async Task UploadUserProfilePictureAsync_SaveUserImageReturnsNull_ThrowsBusinessException()
    {
        // Arrange
        var userId = 1;
        var imageFileName = "profile.png";
        var imageStream = new MemoryStream(Encoding.UTF8.GetBytes("dummy_image_data"));

        _userRepositoryMock.Setup(r => r.SaveUserImageAsync(userId, imageStream, imageFileName)).ReturnsAsync((string?)null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessException>(() =>
            _userService.UploadUserProfilePictureAsync(userId, imageStream, imageFileName)
        );

        Assert.Contains($"Failed to upload profile picture for user {userId}. User not found or file save error.", ex.Message);
        _userRepositoryMock.Verify(r => r.SaveUserImageAsync(userId, imageStream, imageFileName), Times.Once);
        _userRepositoryMock.Verify(r => r.GetByIdAsync(userId), Times.Never);
        _loggerMock.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Warning),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains($"Failed to save image or user not found for userId: {userId}.")),
                null,
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
            Times.Once
        );
    }

    [Fact]
    public async Task UploadUserProfilePictureAsync_UserNotFoundAfterImageSave_ThrowsInvalidOperationException()
    {
        // Arrange
        var userId = 1;
        var imageFileName = "profile.png";
        var imageStream = new MemoryStream(Encoding.UTF8.GetBytes("dummy_image_data"));
        var savedFileName = "saved_profile_1.png";

        _userRepositoryMock.Setup(r => r.SaveUserImageAsync(userId, imageStream, imageFileName)).ReturnsAsync(savedFileName);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync((UserEntity?)null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _userService.UploadUserProfilePictureAsync(userId, imageStream, imageFileName)
        );

        Assert.Contains($"User {userId} not found after image upload process.", ex.Message);
        _userRepositoryMock.Verify(r => r.SaveUserImageAsync(userId, imageStream, imageFileName), Times.Once);
        _userRepositoryMock.Verify(r => r.GetByIdAsync(userId), Times.Once);
        _mapperMock.Verify(m => m.Map<UserDto>(It.IsAny<UserEntity>()), Times.Never);
        _loggerMock.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Error),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains($"User {userId} not found immediately after image save and update.")),
                null,
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
            Times.Once
        );
    }

    [Fact]
    public async Task UploadUserProfilePictureAsync_RepositoryThrowsException_ThrowsExceptionAndLogsError()
    {
        // Arrange
        var userId = 1;
        var imageFileName = "profile.png";
        var imageStream = new MemoryStream(Encoding.UTF8.GetBytes("dummy_image_data"));
        var expectedException = new IOException("Disk full");

        _userRepositoryMock.Setup(r => r.SaveUserImageAsync(userId, imageStream, imageFileName)).ThrowsAsync(expectedException);

        // Act & Assert
        var caughtException = await Assert.ThrowsAsync<IOException>(() =>
            _userService.UploadUserProfilePictureAsync(userId, imageStream, imageFileName)
        );

        Assert.Equal(expectedException, caughtException);
        _userRepositoryMock.Verify(r => r.SaveUserImageAsync(userId, imageStream, imageFileName), Times.Once);
        _userRepositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
        _loggerMock.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Error),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains($"Error occurred while uploading profile picture for user ID: {userId}.")),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
            Times.Once
        );
    }
}
