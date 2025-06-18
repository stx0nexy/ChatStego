using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PixChat.API.Controllers;
using PixChat.Application.DTOs;
using PixChat.Application.Interfaces.Services;
using PixChat.Core.Entities;
using PixChat.Core.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace PixChat.Tests;

public class ChatControllerTests
{
    private readonly Mock<IChatService> _mockChatService;
    private readonly Mock<AutoMapper.IMapper> _mockMapper;
    private readonly Mock<ILogger<ChatController>> _mockLogger;
    private readonly ChatController _chatController;

    public ChatControllerTests()
    {
        _mockChatService = new Mock<IChatService>();
        _mockMapper = new Mock<AutoMapper.IMapper>();
        _mockLogger = new Mock<ILogger<ChatController>>();
        _chatController = new ChatController(_mockChatService.Object, _mockMapper.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetAllChats_ReturnsAllChats()
    {
        // Arrange
        var chats = new List<ChatDto> { new ChatDto { Id = 1, Name = "Chat1" }, new ChatDto { Id = 2, Name = "Chat2" } };
        _mockChatService.Setup(s => s.GetAllChatsAsync()).ReturnsAsync(chats);

        // Act
        var result = await _chatController.GetAllChats();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedChats = Assert.IsAssignableFrom<IEnumerable<ChatDto>>(okResult.Value);
        Assert.Equal(2, returnedChats.Count());
        _mockChatService.Verify(s => s.GetAllChatsAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAllChats_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        _mockChatService.Setup(s => s.GetAllChatsAsync()).ThrowsAsync(new Exception("Test exception"));

        // Act
        var actionResult = await _chatController.GetAllChats();

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(actionResult.Result);
        Assert.Equal(500, objectResult.StatusCode);

        var errorMessage = Assert.IsType<string>(objectResult.Value);
        Assert.Equal("Internal server error", errorMessage);

        _mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Error),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
            Times.Once);
    }

    [Fact]
    public async Task GetChatById_ChatExists_ReturnsChat()
    {
        // Arrange
        var chatId = 1;
        var chat = new ChatDto { Id = chatId, Name = "TestChat" };
        _mockChatService.Setup(s => s.GetChatByIdAsync(chatId)).ReturnsAsync(chat);

        // Act
        var result = await _chatController.GetChatById(chatId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedChat = Assert.IsType<ChatDto>(okResult.Value);
        Assert.Equal(chatId, returnedChat.Id);
        _mockChatService.Verify(s => s.GetChatByIdAsync(chatId), Times.Once);
    }

    [Fact]
    public async Task GetChatById_ChatDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var chatId = 99;
        _mockChatService.Setup(s => s.GetChatByIdAsync(chatId)).ReturnsAsync((ChatDto)null);

        // Act
        var result = await _chatController.GetChatById(chatId);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
        _mockChatService.Verify(s => s.GetChatByIdAsync(chatId), Times.Once);
    }

    [Fact]
    public async Task GetPrivateChatIfExists_ChatExists_ReturnsChat()
    {
        // Arrange
        var userId = 1;
        var contactUserId = 2;
        var chat = new ChatDto { Id = 10, Name = "PrivateChat", IsGroup = false };
        _mockChatService.Setup(s => s.GetPrivateChatIfExists(userId, contactUserId)).ReturnsAsync(chat);

        // Act
        var result = await _chatController.GetPrivateChatIfExists(userId, contactUserId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedChat = Assert.IsType<ChatDto>(okResult.Value);
        Assert.Equal(chat.Id, returnedChat.Id);
        _mockChatService.Verify(s => s.GetPrivateChatIfExists(userId, contactUserId), Times.Once);
    }

    [Fact]
    public async Task GetPrivateChatIfExists_ChatDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var userId = 1;
        var contactUserId = 2;
        _mockChatService.Setup(s => s.GetPrivateChatIfExists(userId, contactUserId)).ReturnsAsync((ChatDto)null);

        // Act
        var result = await _chatController.GetPrivateChatIfExists(userId, contactUserId);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
        _mockChatService.Verify(s => s.GetPrivateChatIfExists(userId, contactUserId), Times.Once);
    }

    [Fact]
    public async Task CreateChat_ValidDto_ReturnsOk()
    {
        // Arrange
        var createChatDto = new CreateChatDto { Name = "NewGroup", CreatorId = 1, IsGroup = true, ParticipantIds = new List<int> { 1, 2, 3 } };
        _mockChatService.Setup(s => s.CreateChatAsync(createChatDto)).Returns(Task.CompletedTask);

        // Act
        var result = await _chatController.CreateChat(createChatDto);

        // Assert
        Assert.IsType<OkResult>(result);
        _mockChatService.Verify(s => s.CreateChatAsync(createChatDto), Times.Once);
    }

    [Fact]
    public async Task UpdateChat_ValidDto_ReturnsOk()
    {
        // Arrange
        var updateChatDto = new UpdateChatDto { Id = 1, Name = "UpdatedChat" };
        _mockChatService.Setup(s => s.UpdateChatAsync(updateChatDto)).Returns(Task.CompletedTask);

        // Act
        var result = await _chatController.UpdateChat(updateChatDto);

        // Assert
        Assert.IsType<OkResult>(result);
        _mockChatService.Verify(s => s.UpdateChatAsync(updateChatDto), Times.Once);
    }

    [Fact]
    public async Task UpdateChat_ChatDoesNotExist_ReturnsInternalServerError()
    {
        // Arrange
        var updateChatDto = new UpdateChatDto { Id = 99, Name = "NonExistent" };
        _mockChatService.Setup(s => s.UpdateChatAsync(updateChatDto)).ThrowsAsync(new KeyNotFoundException("Chat not found."));

        // Act
        var result = await _chatController.UpdateChat(updateChatDto);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, objectResult.StatusCode);

        var errorMessage = Assert.IsType<string>(objectResult.Value);
        Assert.Equal("Internal server error", errorMessage);

        _mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Error),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteChat_ChatExists_ReturnsNoContent()
    {
        // Arrange
        var chatId = 1;
        _mockChatService.Setup(s => s.DeleteChatAsync(chatId)).Returns(Task.CompletedTask);

        // Act
        var result = await _chatController.DeleteChat(chatId);

        // Assert
        Assert.IsType<NoContentResult>(result);
        _mockChatService.Verify(s => s.DeleteChatAsync(chatId), Times.Once);
    }

    [Fact]
    public async Task DeleteChat_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var chatId = 1;
        _mockChatService.Setup(s => s.DeleteChatAsync(chatId)).ThrowsAsync(new Exception("Test exception"));

        // Act
        var result = await _chatController.DeleteChat(chatId);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, objectResult.StatusCode);

        var errorMessage = Assert.IsType<string>(objectResult.Value);
        Assert.Equal("Internal server error", errorMessage);

        _mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Error),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
            Times.Once);
    }

    [Fact]
    public async Task AddParticipant_ValidDto_ReturnsOk()
    {
        // Arrange
        var addParticipantDto = new AddParticipantDto { ChatId = 1, UserId = 2, IsAdmin = false };
        _mockChatService.Setup(s => s.AddParticipantAsync(addParticipantDto)).Returns(Task.CompletedTask);

        // Act
        var result = await _chatController.AddParticipant(addParticipantDto);

        // Assert
        Assert.IsType<OkResult>(result);
        _mockChatService.Verify(s => s.AddParticipantAsync(addParticipantDto), Times.Once);
    }

    [Fact]
    public async Task RemoveParticipant_ParticipantExists_ReturnsNoContent()
    {
        // Arrange
        var participantId = 1;
        _mockChatService.Setup(s => s.RemoveParticipantAsync(participantId)).Returns(Task.CompletedTask);

        // Act
        var result = await _chatController.RemoveParticipant(participantId);

        // Assert
        Assert.IsType<NoContentResult>(result);
        _mockChatService.Verify(s => s.RemoveParticipantAsync(participantId), Times.Once);
    }

    [Fact]
    public async Task GetParticipantsByChatId_ReturnsParticipants()
    {
        // Arrange
        var chatId = 1;
        var participants = new List<UserDto> { new UserDto { Id = 1, Username = "User1" }, new UserDto { Id = 2, Username = "User2" } };
        _mockChatService.Setup(s => s.GetParticipantsByChatIdAsync(chatId)).ReturnsAsync(participants);

        // Act
        var result = await _chatController.GetParticipantsByChatId(chatId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedParticipants = Assert.IsAssignableFrom<IEnumerable<UserDto>>(okResult.Value);
        Assert.Equal(2, returnedParticipants.Count());
        _mockChatService.Verify(s => s.GetParticipantsByChatIdAsync(chatId), Times.Once);
    }

    [Fact]
    public async Task GetUserChats_ReturnsUserChats()
    {
        // Arrange
        var userId = 1;
        var chats = new List<ChatDto> { new ChatDto { Id = 1, Name = "Chat1" }, new ChatDto { Id = 2, Name = "Chat2" } };
        _mockChatService.Setup(s => s.GetUserChatsAsync(userId)).ReturnsAsync(chats);

        // Act
        var result = await _chatController.GetUserChats(userId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedChats = Assert.IsAssignableFrom<IEnumerable<ChatDto>>(okResult.Value);
        Assert.Equal(2, returnedChats.Count());
        _mockChatService.Verify(s => s.GetUserChatsAsync(userId), Times.Once);
    }
}