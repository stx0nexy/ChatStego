using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using PixChat.Application.Interfaces.Services;
using PixChat.Application.Services;
using PixChat.Core.Interfaces.Repositories;
using AutoMapper; 

namespace PixChat.Tests;

public class KeyServiceTests
{
    private readonly Mock<ILogger<KeyService>> _loggerMock;
    private readonly Mock<IUserKeyRepository> _userKeyRepositoryMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly KeyService _keyService;

    public KeyServiceTests()
    {
        _loggerMock = new Mock<ILogger<KeyService>>();
        _userKeyRepositoryMock = new Mock<IUserKeyRepository>();
        _mapperMock = new Mock<IMapper>();

        _keyService = new KeyService(
            _loggerMock.Object,
            _userKeyRepositoryMock.Object,
            _mapperMock.Object
        );
    }

    [Fact]
    public async Task GetPublicKeyAsync_ReturnsPublicKey_WhenFound()
    {
        // Arrange
        int userId = 1;
        string expectedPublicKey = "testPublicKeyString";
        _userKeyRepositoryMock.Setup(r => r.GetPublicKeyAsync(userId))
            .ReturnsAsync(expectedPublicKey);

        // Act
        string? result = await _keyService.GetPublicKeyAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedPublicKey, result);
        _userKeyRepositoryMock.Verify(r => r.GetPublicKeyAsync(userId), Times.Once);
        _loggerMock.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Information),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
            Times.Never
        );
    }

    [Fact]
    public async Task GetPublicKeyAsync_ReturnsNull_WhenNotFound()
    {
        // Arrange
        int userId = 2;
        _userKeyRepositoryMock.Setup(r => r.GetPublicKeyAsync(userId))
            .ReturnsAsync((string?)null);

        // Act
        string? result = await _keyService.GetPublicKeyAsync(userId);

        // Assert
        Assert.Null(result);
        _userKeyRepositoryMock.Verify(r => r.GetPublicKeyAsync(userId), Times.Once);
        _loggerMock.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Information),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains($"Public key not found for user {userId}.")),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
            Times.Once
        );
    }

    [Fact]
    public async Task GetPublicKeyAsync_ThrowsException_WhenRepositoryThrows()
    {
        // Arrange
        int userId = 3;
        var expectedException = new InvalidOperationException("Database error");
        _userKeyRepositoryMock.Setup(r => r.GetPublicKeyAsync(userId))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var caughtException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _keyService.GetPublicKeyAsync(userId)
        );

        Assert.Equal(expectedException, caughtException);
        _userKeyRepositoryMock.Verify(r => r.GetPublicKeyAsync(userId), Times.Once);
        _loggerMock.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Error),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains($"Error occurred while retrieving public key for user {userId}.")),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
            Times.Once
        );
    }

    [Fact]
    public async Task SavePublicKeyAsync_CallsRepository_OnSuccess()
    {
        // Arrange
        int userId = 4;
        string publicKey = "newPublicKeyString";
        _userKeyRepositoryMock.Setup(r => r.SavePublicKeyAsync(userId, publicKey))
            .Returns(Task.CompletedTask);

        // Act
        await _keyService.SavePublicKeyAsync(userId, publicKey);

        // Assert
        _userKeyRepositoryMock.Verify(r => r.SavePublicKeyAsync(userId, publicKey), Times.Once);
        _loggerMock.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Error),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
            Times.Never
        );
    }

    [Fact]
    public async Task SavePublicKeyAsync_ThrowsException_WhenRepositoryThrows()
    {
        // Arrange
        int userId = 5;
        string publicKey = "anotherPublicKey";
        var expectedException = new ApplicationException("Failed to save key");
        _userKeyRepositoryMock.Setup(r => r.SavePublicKeyAsync(userId, publicKey))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var caughtException = await Assert.ThrowsAsync<ApplicationException>(
            () => _keyService.SavePublicKeyAsync(userId, publicKey)
        );

        Assert.Equal(expectedException, caughtException);
        _userKeyRepositoryMock.Verify(r => r.SavePublicKeyAsync(userId, publicKey), Times.Once);
        _loggerMock.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Error),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains($"Error occurred while saving key for user {userId}.")),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
            Times.Once
        );
    }
}
