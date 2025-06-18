using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using PixChat.API.Controllers;
using PixChat.Application.DTOs;
using PixChat.Application.Interfaces.Services;

namespace PixChat.Tests;

public class KeysControllerTests
{
    private readonly Mock<IKeyService> _keyServiceMock;
    private readonly KeysController _keysController;

    public KeysControllerTests()
    {
        _keyServiceMock = new Mock<IKeyService>();
        _keysController = new KeysController(_keyServiceMock.Object);
    }
    
    [Fact]
    public async Task GetPublicKey_KeyFound_ReturnsOkWithPublicKey()
    {
        // Arrange
        var userId = 1;
        var publicKey = "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAy";
        _keyServiceMock.Setup(s => s.GetPublicKeyAsync(userId)).ReturnsAsync(publicKey);

        // Act
        var result = await _keysController.GetPublicKey(userId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        
        var publicKeyProperty = okResult.Value.GetType().GetProperty("PublicKey");
        Assert.NotNull(publicKeyProperty);
        var actualPublicKey = publicKeyProperty.GetValue(okResult.Value) as string;

        Assert.Equal(publicKey, actualPublicKey);
        _keyServiceMock.Verify(s => s.GetPublicKeyAsync(userId), Times.Once);
    }

    [Fact]
    public async Task GetPublicKey_KeyNotFound_ReturnsNotFound()
    {
        // Arrange
        var userId = 99;
        _keyServiceMock.Setup(s => s.GetPublicKeyAsync(userId)).ReturnsAsync((string?)null);

        // Act
        var result = await _keysController.GetPublicKey(userId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        
        var messageProperty = notFoundResult.Value.GetType().GetProperty("message");
        Assert.NotNull(messageProperty);
        var actualMessage = messageProperty.GetValue(notFoundResult.Value) as string;

        Assert.Contains($"Public key not found for user {userId}.", actualMessage);
        _keyServiceMock.Verify(s => s.GetPublicKeyAsync(userId), Times.Once);
    }

    [Fact]
    public async Task GetPublicKey_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var userId = 1;
        var expectedException = new InvalidOperationException("Database connection failed");
        _keyServiceMock.Setup(s => s.GetPublicKeyAsync(userId)).ThrowsAsync(expectedException);

        // Act
        var result = await _keysController.GetPublicKey(userId);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        
        var messageProperty = statusCodeResult.Value.GetType().GetProperty("message");
        var errorProperty = statusCodeResult.Value.GetType().GetProperty("error");

        Assert.NotNull(messageProperty);
        Assert.NotNull(errorProperty);

        var actualMessage = messageProperty.GetValue(statusCodeResult.Value) as string;
        var actualError = errorProperty.GetValue(statusCodeResult.Value) as string;

        Assert.Contains("Internal server error while retrieving public key.", actualMessage);
        Assert.Equal(expectedException.Message, actualError);
        _keyServiceMock.Verify(s => s.GetPublicKeyAsync(userId), Times.Once);
    }

    
    [Fact]
    public async Task SavePublicKey_ValidRequest_ReturnsOk()
    {
        // Arrange
        var userId = 1;
        var request = new SavePublicKeyRequest { PublicKey = "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAy" };
        _keyServiceMock.Setup(s => s.SavePublicKeyAsync(userId, request.PublicKey)).Returns(Task.CompletedTask);

        // Act
        var result = await _keysController.SavePublicKey(userId, request);

        // Assert
        Assert.IsType<OkResult>(result);
        _keyServiceMock.Verify(s => s.SavePublicKeyAsync(userId, request.PublicKey), Times.Once);
    }

    [Fact]
    public async Task SavePublicKey_ServiceThrowsException_ReturnsBadRequest()
    {
        // Arrange
        var userId = 1;
        var request = new SavePublicKeyRequest { PublicKey = "InvalidKey" };
        var expectedException = new ArgumentException("Invalid public key format");
        _keyServiceMock.Setup(s => s.SavePublicKeyAsync(userId, request.PublicKey)).ThrowsAsync(expectedException);

        // Act
        var result = await _keysController.SavePublicKey(userId, request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        
        var messageProperty = badRequestResult.Value.GetType().GetProperty("message");
        var errorProperty = badRequestResult.Value.GetType().GetProperty("error");

        Assert.NotNull(messageProperty);
        Assert.NotNull(errorProperty);

        var actualMessage = messageProperty.GetValue(badRequestResult.Value) as string;
        var actualError = errorProperty.GetValue(badRequestResult.Value) as string;

        Assert.Contains("Error saving public key", actualMessage);
        Assert.Equal(expectedException.Message, actualError);
        _keyServiceMock.Verify(s => s.SavePublicKeyAsync(userId, request.PublicKey), Times.Once);
    }
}
