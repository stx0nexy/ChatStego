using Moq;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using PixChat.API.Controllers;
using PixChat.Application.DTOs;
using PixChat.Application.Interfaces.Services;
using PixChat.Application.Requests;
using System.Text.Json;

namespace PixChat.Tests;

public class AuthControllerTests
{
    private readonly Mock<IPasswordHasher<UserDto>> _mockPasswordHasher;
    private readonly Mock<IJwtTokenService> _mockJwtTokenService;
    private readonly Mock<IUserService> _mockUserService;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<IMemoryCache> _mockMemoryCache;
    private readonly Mock<ITwoFactorService> _mockTwoFactorService;
    private readonly AuthController _authController;

    public AuthControllerTests()
    {
        _mockPasswordHasher = new Mock<IPasswordHasher<UserDto>>();
        _mockJwtTokenService = new Mock<IJwtTokenService>();
        _mockUserService = new Mock<IUserService>();
        _mockEmailService = new Mock<IEmailService>();
        _mockMemoryCache = new Mock<IMemoryCache>();
        _mockTwoFactorService = new Mock<ITwoFactorService>();

        _authController = new AuthController(
            _mockPasswordHasher.Object,
            _mockJwtTokenService.Object,
            _mockUserService.Object,
            _mockEmailService.Object,
            _mockTwoFactorService.Object
        );
    }

    [Fact]
    public async Task Register_ValidRequest_ReturnsOk()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "test@example.com",
            Username = "testuser",
            Phone = "1234567890",
            Password = "Password123!"
        };
        _mockUserService.Setup(s => s.UserExistsByEmailAsync(request.Email)).ReturnsAsync(false);
        _mockPasswordHasher.Setup(h => h.HashPassword(It.IsAny<UserDto>(), request.Password)).Returns("hashedpassword");
        _mockUserService.Setup(s => s.AddAsync(It.IsAny<UserDto>())).Returns(Task.CompletedTask);
        _mockTwoFactorService.Setup(s => s.SendTwoFactorCodeAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

        // Act
        var result = await _authController.Register(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        _mockUserService.Verify(s => s.AddAsync(It.IsAny<UserDto>()), Times.Once);
        _mockTwoFactorService.Verify(s => s.SendTwoFactorCodeAsync(request.Email), Times.Once);
    }

    [Fact]
    public async Task Register_EmailAlreadyExists_ReturnsBadRequest()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "existing@example.com",
            Username = "testuser",
            Phone = "1234567890",
            Password = "Password123!"
        };
        _mockUserService.Setup(s => s.UserExistsByEmailAsync(request.Email)).ReturnsAsync(true);

        // Act
        var result = await _authController.Register(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);
        Assert.Equal("Email already exists", badRequestResult.Value);
        _mockUserService.Verify(s => s.AddAsync(It.IsAny<UserDto>()), Times.Never);
        _mockTwoFactorService.Verify(s => s.SendTwoFactorCodeAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task VerifyRegistration_ValidCode_ReturnsOk()
    {
        // Arrange
        var request = new Verify2FARequest { Email = "test@example.com", Code = "123456" };
        var userDto = new UserDto { Email = "test@example.com", IsVerified = false };

        _mockTwoFactorService.Setup(s => s.VerifyCode(request.Email, request.Code)).Returns(true);
        _mockUserService.Setup(s => s.GetByEmailAsync(request.Email)).ReturnsAsync(userDto);
        _mockUserService.Setup(s => s.UpdateAsync(It.IsAny<UserDto>())).Returns(Task.CompletedTask);

        // Act
        var result = await _authController.VerifyRegistration(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        Assert.Equal("Registration completed successfully", okResult.Value);
        Assert.True(userDto.IsVerified);
        _mockUserService.Verify(s => s.UpdateAsync(userDto), Times.Once);
        _mockTwoFactorService.Verify(s => s.VerifyCode(request.Email, request.Code), Times.Once);
    }

    [Fact]
    public async Task VerifyRegistration_InvalidCode_ReturnsUnauthorized()
    {
        // Arrange
        var request = new Verify2FARequest { Email = "test@example.com", Code = "invalidcode" };
        _mockTwoFactorService.Setup(s => s.VerifyCode(request.Email, request.Code)).Returns(false);

        // Act
        var result = await _authController.VerifyRegistration(request);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(401, unauthorizedResult.StatusCode);
        Assert.Equal("Invalid or expired verification code", unauthorizedResult.Value);
        _mockUserService.Verify(s => s.GetByEmailAsync(It.IsAny<string>()), Times.Never);
        _mockUserService.Verify(s => s.UpdateAsync(It.IsAny<UserDto>()), Times.Never);
        _mockTwoFactorService.Verify(s => s.VerifyCode(request.Email, request.Code), Times.Once);
    }

    [Fact]
    public async Task VerifyRegistration_UserNotFound_ReturnsNotFound()
    {
        // Arrange
        var request = new Verify2FARequest { Email = "nonexistent@example.com", Code = "123456" };
        _mockTwoFactorService.Setup(s => s.VerifyCode(request.Email, request.Code)).Returns(true);
        _mockUserService.Setup(s => s.GetByEmailAsync(request.Email)).ReturnsAsync((UserDto)null);

        // Act
        var result = await _authController.VerifyRegistration(request);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(404, notFoundResult.StatusCode);
        Assert.Equal("User not found", notFoundResult.Value);
        _mockUserService.Verify(s => s.UpdateAsync(It.IsAny<UserDto>()), Times.Never);
        _mockTwoFactorService.Verify(s => s.VerifyCode(request.Email, request.Code), Times.Once);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsOkWithTokenAndUser()
    {
        // Arrange
        var request = new LoginRequest { Email = "test@example.com", Password = "Password123!" };
        var userDto = new UserDto { Id = 1, Email = "test@example.com", Username = "testuser", PasswordHash = "hashedpassword", IsVerified = true };
        var jwtToken = "mocked_jwt_token";

        _mockUserService.Setup(s => s.GetByEmailAsync(request.Email)).ReturnsAsync(userDto);
        _mockPasswordHasher.Setup(h => h.VerifyHashedPassword(userDto, userDto.PasswordHash, request.Password)).Returns(PasswordVerificationResult.Success);
        _mockJwtTokenService.Setup(s => s.GenerateToken(userDto)).Returns(jwtToken);

        // Act
        var result = await _authController.Login(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        var jsonString = JsonSerializer.Serialize(okResult.Value);
        var returnedObject = JsonSerializer.Deserialize<LoginResponseDto>(jsonString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(returnedObject);

        Assert.NotNull(returnedObject.token);
        Assert.Equal(jwtToken, returnedObject.token);

        Assert.NotNull(returnedObject.user);

        Assert.Equal(userDto.Id, returnedObject.user.Id);
        Assert.Equal(userDto.Email, returnedObject.user.Email);
        Assert.Equal(userDto.Username, returnedObject.user.Username);
    }

    [Fact]
    public async Task Login_UserNotFoundOrNotVerified_ReturnsUnauthorized()
    {
        // Arrange
        var request = new LoginRequest { Email = "unverified@example.com", Password = "Password123!" };
        var userDto = new UserDto { Email = "unverified@example.com", IsVerified = false };

        _mockUserService.Setup(s => s.GetByEmailAsync(request.Email)).ReturnsAsync(userDto);

        // Act
        var result = await _authController.Login(request);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(401, unauthorizedResult.StatusCode);
        Assert.Equal("User not verified", unauthorizedResult.Value);
        _mockPasswordHasher.Verify(h => h.VerifyHashedPassword(It.IsAny<UserDto>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _mockJwtTokenService.Verify(s => s.GenerateToken(It.IsAny<UserDto>()), Times.Never);
    }

    [Fact]
    public async Task Login_InvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var request = new LoginRequest { Email = "test@example.com", Password = "WrongPassword!" };
        var userDto = new UserDto { Email = "test@example.com", Username = "testuser", PasswordHash = "hashedpassword", IsVerified = true };

        _mockUserService.Setup(s => s.GetByEmailAsync(request.Email)).ReturnsAsync(userDto);
        _mockPasswordHasher.Setup(h => h.VerifyHashedPassword(userDto, userDto.PasswordHash, request.Password)).Returns(PasswordVerificationResult.Failed);

        // Act
        var result = await _authController.Login(request);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(401, unauthorizedResult.StatusCode);
        Assert.Equal("Invalid credentials", unauthorizedResult.Value);
        _mockJwtTokenService.Verify(s => s.GenerateToken(It.IsAny<UserDto>()), Times.Never);
    }
}