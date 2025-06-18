using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using AutoMapper;
using PixChat.API.Controllers;
using PixChat.Application.DTOs;
using PixChat.Application.Interfaces.Services;
using PixChat.Application.Requests;
using Microsoft.AspNetCore.Http;

namespace PixChat.Tests;

public class UsersControllerTests
{
    private readonly Mock<IUserService> _userServiceMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<ISteganographyService> _steganographyServiceMock;
    private readonly Mock<IKeyService> _keyServiceMock;
    private readonly Mock<IOfflineMessageService> _offlineMessageServiceMock;
    private readonly Mock<IOneTimeMessageService> _oneTimeMessageServiceMock;
    private readonly UsersController _usersController;

    public UsersControllerTests()
    {
        _userServiceMock = new Mock<IUserService>();
        _mapperMock = new Mock<IMapper>();
        _steganographyServiceMock = new Mock<ISteganographyService>();
        _keyServiceMock = new Mock<IKeyService>();
        _offlineMessageServiceMock = new Mock<IOfflineMessageService>();
        _oneTimeMessageServiceMock = new Mock<IOneTimeMessageService>();

        _usersController = new UsersController(
            _userServiceMock.Object,
            _mapperMock.Object,
            _steganographyServiceMock.Object,
            _keyServiceMock.Object,
            _offlineMessageServiceMock.Object,
            _oneTimeMessageServiceMock.Object
        );
    }

    private UserDto CreateDummyUserDto(int id = 1, string email = "test@example.com", string username = "testuser")
    {
        return new UserDto
        {
            Id = id,
            Email = email,
            Username = username,
            PasswordHash = "hashedpassword",
            ProfilePictureUrl = "http://example.com/pic.png",
            Status = true,
            IsVerified = true,
            LastSeen = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    [Fact]
    public async Task GetUserById_UserExists_ReturnsOkWithUser()
    {
        // Arrange
        var userId = 1;
        var userDto = CreateDummyUserDto(userId);
        _userServiceMock.Setup(s => s.GetByIdAsync(userId)).ReturnsAsync(userDto);

        // Act
        var result = await _usersController.GetUserById(userId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var actualUser = Assert.IsType<UserDto>(okResult.Value);
        Assert.Equal(userId, actualUser.Id);
        _userServiceMock.Verify(s => s.GetByIdAsync(userId), Times.Once);
    }

    [Fact]
    public async Task GetUserById_UserDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var userId = 99;
        _userServiceMock.Setup(s => s.GetByIdAsync(userId)).ReturnsAsync((UserDto?)null);

        // Act
        var result = await _usersController.GetUserById(userId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
        _userServiceMock.Verify(s => s.GetByIdAsync(userId), Times.Once);
    }

    [Fact]
    public async Task GetUserByEmail_UserExists_ReturnsOkWithUser()
    {
        // Arrange
        var userEmail = "test@example.com";
        var userDto = CreateDummyUserDto(email: userEmail);
        _userServiceMock.Setup(s => s.GetByEmailAsync(userEmail)).ReturnsAsync(userDto);

        // Act
        var result = await _usersController.GetUserByEmail(userEmail);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var actualUser = Assert.IsType<UserDto>(okResult.Value);
        Assert.Equal(userEmail, actualUser.Email);
        _userServiceMock.Verify(s => s.GetByEmailAsync(userEmail), Times.Once);
    }

    [Fact]
    public async Task GetUserByEmail_UserDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var userEmail = "nonexistent@example.com";
        _userServiceMock.Setup(s => s.GetByEmailAsync(userEmail)).ReturnsAsync((UserDto?)null);

        // Act
        var result = await _usersController.GetUserByEmail(userEmail);

        // Assert
        Assert.IsType<NotFoundResult>(result);
        _userServiceMock.Verify(s => s.GetByEmailAsync(userEmail), Times.Once);
    }

    [Fact]
    public async Task AddUser_ValidRequest_ReturnsOk()
    {
        // Arrange
        var userDto = CreateDummyUserDto();
        _userServiceMock.Setup(s => s.AddAsync(userDto)).Returns(Task.CompletedTask);

        // Act
        var result = await _usersController.AddUser(userDto);

        // Assert
        Assert.IsType<OkResult>(result);
        _userServiceMock.Verify(s => s.AddAsync(userDto), Times.Once);
    }
    
    [Fact]
    public async Task UpdateUser_ValidRequest_ReturnsOk()
    {
        // Arrange
        var userId = 1;
        var userDto = CreateDummyUserDto(userId);
        _userServiceMock.Setup(s => s.UpdateAsync(userDto)).Returns(Task.CompletedTask);

        // Act
        var result = await _usersController.UpdateUser(userId, userDto);

        // Assert
        Assert.IsType<OkResult>(result);
        _userServiceMock.Verify(s => s.UpdateAsync(userDto), Times.Once);
    }
    
    [Fact]
    public async Task DeleteUser_ValidRequest_ReturnsOk()
    {
        // Arrange
        var userId = 1;
        _userServiceMock.Setup(s => s.DeleteAsync(userId)).Returns(Task.CompletedTask);

        // Act
        var result = await _usersController.DeleteUser(userId);

        // Assert
        Assert.IsType<OkResult>(result);
        _userServiceMock.Verify(s => s.DeleteAsync(userId), Times.Once);
    }
    
    [Fact]
    public async Task ReceiveMessage_ValidRequest_ReturnsOkWithExtractedData()
    {
        // Arrange
        var userId = 1;
        var request = new MessageRequest
        {
            Base64Image = Convert.ToBase64String(Encoding.UTF8.GetBytes("dummyImageData")),
            EncryptedKey = "dummyEncryptedKey"
        };
        var extractedMessage = Encoding.UTF8.GetBytes("Hello from image!");
        var timestamp = DateTime.UtcNow;
        var encryptedAesKey = "extractedAESKey";
        var aesIv = new byte[] { 0x01, 0x02, 0x03, 0x04 };

        _steganographyServiceMock.Setup(s => s.ExtractFullMessage(
            It.IsAny<byte[]>(), request.EncryptedKey))
            .Returns((extractedMessage, timestamp, encryptedAesKey, aesIv));

        // Act
        var result = await _usersController.ReceiveMessage(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var valueType = okResult.Value.GetType();

        var messageProperty = valueType.GetProperty("message");
        var timestampProperty = valueType.GetProperty("timestamp");
        var encryptedAesKeyProperty = valueType.GetProperty("encryptedAesKey");
        var aesIvProperty = valueType.GetProperty("aesIv");

        Assert.NotNull(messageProperty);
        Assert.NotNull(timestampProperty);
        Assert.NotNull(encryptedAesKeyProperty);
        Assert.NotNull(aesIvProperty);

        Assert.Equal(extractedMessage, messageProperty.GetValue(okResult.Value));
        Assert.Equal(timestamp, (DateTime)timestampProperty.GetValue(okResult.Value));
        Assert.Equal(encryptedAesKey, (string)encryptedAesKeyProperty.GetValue(okResult.Value));
        Assert.Equal(aesIv, (byte[])aesIvProperty.GetValue(okResult.Value));

        _steganographyServiceMock.Verify(s => s.ExtractFullMessage(
            It.IsAny<byte[]>(), request.EncryptedKey), Times.Once);
    }

    [Fact]
    public async Task ReceiveMessage_SteganographyServiceThrowsException_ReturnsBadRequest()
    {
        // Arrange
        var userId = 1;
        var request = new MessageRequest
        {
            Base64Image = Convert.ToBase64String(Encoding.UTF8.GetBytes("invalidImageData")),
            EncryptedKey = "someKey"
        };
        var expectedException = new FormatException("Invalid image format or marker not found.");
        _steganographyServiceMock.Setup(s => s.ExtractFullMessage(
            It.IsAny<byte[]>(), request.EncryptedKey))
            .Throws(expectedException);

        // Act
        var result = await _usersController.ReceiveMessage(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var valueType = badRequestResult.Value.GetType();

        var errorProperty = valueType.GetProperty("error");
        Assert.NotNull(errorProperty);
        var actualError = errorProperty.GetValue(badRequestResult.Value) as string;

        Assert.Contains(expectedException.Message, actualError);
        _steganographyServiceMock.Verify(s => s.ExtractFullMessage(
            It.IsAny<byte[]>(), request.EncryptedKey), Times.Once);
    }
    
    [Fact]
    public async Task UploadUserProfilePicture_ValidImage_ReturnsOkWithUserDto()
    {
        // Arrange
        var userId = 1;
        var fileName = "profile.png";
        var fileContent = "dummy image data";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(fileContent));
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.FileName).Returns(fileName);
        mockFile.Setup(f => f.Length).Returns(stream.Length);
        mockFile.Setup(f => f.OpenReadStream()).Returns(stream);

        var userDtoAfterUpload = CreateDummyUserDto(userId);
        userDtoAfterUpload.ProfilePictureUrl = "new_profile_url.png";

        _userServiceMock.Setup(s => s.UploadUserProfilePictureAsync(userId, It.IsAny<Stream>(), fileName))
            .ReturnsAsync(userDtoAfterUpload);

        // Act
        var result = await _usersController.UploadUserProfilePicture(userId, mockFile.Object);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var actualUserDto = Assert.IsType<UserDto>(okResult.Value);
        Assert.Equal(userDtoAfterUpload.Id, actualUserDto.Id);
        Assert.Equal(userDtoAfterUpload.ProfilePictureUrl, actualUserDto.ProfilePictureUrl);
        _userServiceMock.Verify(s => s.UploadUserProfilePictureAsync(userId, It.IsAny<Stream>(), fileName), Times.Once);
    }
    
    [Fact]
    public async Task ConfirmMessageReceived_ValidRequest_ReturnsOk()
    {
        // Arrange
        var request = new MessageConfirmRequest { MessageId = "msg123" };
        _offlineMessageServiceMock.Setup(s => s.MarkMessageAsReceivedAsync(request.MessageId)).Returns(Task.CompletedTask);
        _offlineMessageServiceMock.Setup(s => s.DeleteMessageAsync(request.MessageId)).Returns(Task.CompletedTask);

        // Act
        var result = await _usersController.ConfirmMessageReceived(request);

        // Assert
        Assert.IsType<OkResult>(result);
        _offlineMessageServiceMock.Verify(s => s.MarkMessageAsReceivedAsync(request.MessageId), Times.Once);
        _offlineMessageServiceMock.Verify(s => s.DeleteMessageAsync(request.MessageId), Times.Once);
    }

    [Fact]
    public async Task ConfirmOneTimeMessageReceived_ValidRequest_ReturnsOk()
    {
        // Arrange
        var request = new MessageConfirmRequest { MessageId = "onetime_msg456" };
        _oneTimeMessageServiceMock.Setup(s => s.MarkOneTimeMessageAsReceivedAsync(request.MessageId)).Returns(Task.CompletedTask);

        // Act
        var result = await _usersController.ConfirmOneTimeMessageReceived(request);

        // Assert
        Assert.IsType<OkResult>(result);
        _oneTimeMessageServiceMock.Verify(s => s.MarkOneTimeMessageAsReceivedAsync(request.MessageId), Times.Once);
    }
}
