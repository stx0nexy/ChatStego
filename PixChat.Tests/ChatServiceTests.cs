using Xunit;
using Moq;
using AutoMapper;
using Microsoft.Extensions.Logging;
using PixChat.Application.DTOs;
using PixChat.Application.Interfaces.Services;
using PixChat.Application.Services;
using PixChat.Core.Entities;
using PixChat.Core.Interfaces.Repositories;
using PixChat.Core.Exceptions;
using System.Linq;

namespace PixChat.Tests;

public class ChatServiceTests
{
    private readonly Mock<IChatRepository> _mockChatRepository;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<IChatParticipantRepository> _mockParticipantRepository;
    private readonly Mock<ILogger<ChatService>> _mockLogger;
    private readonly ChatService _chatService;

    public ChatServiceTests()
    {
        _mockChatRepository = new Mock<IChatRepository>();
        _mockMapper = new Mock<IMapper>();
        _mockParticipantRepository = new Mock<IChatParticipantRepository>();
        _mockLogger = new Mock<ILogger<ChatService>>();
        _chatService = new ChatService(
            _mockLogger.Object,
            _mockChatRepository.Object,
            _mockMapper.Object,
            _mockParticipantRepository.Object
        );
    }

    [Fact]
    public async Task GetAllChatsAsync_ReturnsAllChats()
    {
        // Arrange
        var chatEntities = new List<ChatEntity> { new ChatEntity(), new ChatEntity() };
        var chatDtos = new List<ChatDto> { new ChatDto(), new ChatDto() };
        _mockChatRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(chatEntities);
        _mockMapper.Setup(m => m.Map<IEnumerable<ChatDto>>(It.IsAny<IEnumerable<ChatEntity>>())).Returns(chatDtos);

        // Act
        var result = await _chatService.GetAllChatsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(chatDtos.Count, result.Count());
        _mockChatRepository.Verify(r => r.GetAllAsync(), Times.Once);
        _mockMapper.Verify(m => m.Map<IEnumerable<ChatDto>>(chatEntities), Times.Once);
    }

    [Fact]
    public async Task GetAllChatsAsync_RepositoryThrowsException_ThrowsException()
    {
        // Arrange
        _mockChatRepository.Setup(r => r.GetAllAsync()).ThrowsAsync(new Exception("DB error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _chatService.GetAllChatsAsync());
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
    public async Task GetChatByIdAsync_ChatExists_ReturnsChat()
    {
        // Arrange
        var chatId = 1;
        var chatEntity = new ChatEntity { Id = chatId };
        var chatDto = new ChatDto { Id = chatId };
        _mockChatRepository.Setup(r => r.GetByIdAsync(chatId)).ReturnsAsync(chatEntity);
        _mockMapper.Setup(m => m.Map<ChatDto>(It.IsAny<ChatEntity>())).Returns(chatDto);

        // Act
        var result = await _chatService.GetChatByIdAsync(chatId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(chatId, result.Id);
        _mockChatRepository.Verify(r => r.GetByIdAsync(chatId), Times.Once);
        _mockMapper.Verify(m => m.Map<ChatDto>(chatEntity), Times.Once);
    }

    [Fact]
    public async Task GetChatByIdAsync_ChatDoesNotExist_ReturnsNull()
    {
        // Arrange
        var chatId = 99;
        _mockChatRepository.Setup(r => r.GetByIdAsync(chatId)).ReturnsAsync((ChatEntity)null);

        // Act
        var result = await _chatService.GetChatByIdAsync(chatId);

        // Assert
        Assert.Null(result);
        _mockChatRepository.Verify(r => r.GetByIdAsync(chatId), Times.Once);
        _mockMapper.Verify(m => m.Map<ChatDto>(It.IsAny<ChatEntity>()), Times.Never);
    }

    [Fact]
    public async Task GetPrivateChatIfExists_ChatExists_ReturnsChat()
    {
        // Arrange
        var userId = 1;
        var contactUserId = 2;
        var chatEntity = new ChatEntity { Id = 10, IsGroup = false };
        var chatDto = new ChatDto { Id = 10, IsGroup = false };
        _mockChatRepository.Setup(r => r.GetPrivateChatIfExistsAsync(userId, contactUserId)).ReturnsAsync(chatEntity);
        _mockMapper.Setup(m => m.Map<ChatDto>(It.IsAny<ChatEntity>())).Returns(chatDto);

        // Act
        var result = await _chatService.GetPrivateChatIfExists(userId, contactUserId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(chatDto.Id, result.Id);
        _mockChatRepository.Verify(r => r.GetPrivateChatIfExistsAsync(userId, contactUserId), Times.Once);
        _mockMapper.Verify(m => m.Map<ChatDto>(chatEntity), Times.Once);
    }

    [Fact]
    public async Task GetPrivateChatIfExists_ChatDoesNotExist_ReturnsNull()
    {
        // Arrange
        var userId = 1;
        var contactUserId = 2;
        _mockChatRepository.Setup(r => r.GetPrivateChatIfExistsAsync(userId, contactUserId)).ReturnsAsync((ChatEntity)null);

        _mockMapper.Setup(m => m.Map<ChatDto>(null)).Returns((ChatDto)null);

        // Act
        var result = await _chatService.GetPrivateChatIfExists(userId, contactUserId);

        // Assert
        Assert.Null(result);
        _mockChatRepository.Verify(r => r.GetPrivateChatIfExistsAsync(userId, contactUserId), Times.Once);
        _mockMapper.Verify(m => m.Map<ChatDto>(It.Is<ChatEntity>(e => e == null)), Times.Once);
    }

    [Fact]
    public async Task CreatePrivateChatAsync_CreatesChatAndParticipants()
    {
        // Arrange
        var userId = 1;
        var contactUserId = 2;
        var chatId = 5;

        _mockChatRepository.Setup(r => r.AddAsync(It.IsAny<ChatEntity>())).ReturnsAsync(chatId);
        
        // Act
        await _chatService.CreatePrivateChatAsync(userId, contactUserId);

        // Assert
        _mockChatRepository.Verify(r => r.AddAsync(It.Is<ChatEntity>(c =>
            c.IsGroup == false &&
            c.CreatorId == userId &&
            c.Name == $"{userId}-{contactUserId}" &&
            c.Participants != null &&
            c.Participants.Count == 2 &&
            c.Participants.Any(p => p.UserId == userId && p.IsAdmin == true) &&
            c.Participants.Any(p => p.UserId == contactUserId && p.IsAdmin == false)
        )), Times.Once);

        _mockParticipantRepository.Verify(r => r.AddAsync(It.IsAny<ChatParticipantEntity>()), Times.Never);
    }

    [Fact]
    public async Task CreateChatAsync_CreatesGroupChatAndParticipants()
    {
        // Arrange
        var createChatDto = new CreateChatDto { Name = "NewGroup", CreatorId = 1, IsGroup = true, ParticipantIds = new List<int> { 2, 3 } };
        var chatEntity = new ChatEntity { Id = 10 };

        _mockMapper.Setup(m => m.Map<ChatEntity>(It.IsAny<CreateChatDto>())).Returns(chatEntity);
        _mockChatRepository.Setup(r => r.AddAsync(It.IsAny<ChatEntity>())).ReturnsAsync(chatEntity.Id);

        _mockMapper.Setup(m => m.Map<ChatParticipantEntity>(It.IsAny<AddParticipantDto>()))
                   .Returns((AddParticipantDto dto) => new ChatParticipantEntity
                   {
                       ChatId = dto.ChatId,
                       UserId = dto.UserId,
                       IsAdmin = dto.IsAdmin,
                       JoinedAt = DateTime.UtcNow 
                   });

        _mockParticipantRepository.Setup(r => r.AddAsync(It.IsAny<ChatParticipantEntity>())).Returns(Task.CompletedTask);

        // Act
        await _chatService.CreateChatAsync(createChatDto);

        // Assert
        _mockMapper.Verify(m => m.Map<ChatEntity>(createChatDto), Times.Once);
        _mockChatRepository.Verify(r => r.AddAsync(chatEntity), Times.Once);

        _mockParticipantRepository.Verify(r => r.AddAsync(It.Is<ChatParticipantEntity>(p => p.UserId == createChatDto.CreatorId && p.ChatId == chatEntity.Id && p.IsAdmin == true)), Times.Once);
        _mockParticipantRepository.Verify(r => r.AddAsync(It.Is<ChatParticipantEntity>(p => p.UserId == 2 && p.ChatId == chatEntity.Id && p.IsAdmin == false)), Times.Once);
        _mockParticipantRepository.Verify(r => r.AddAsync(It.Is<ChatParticipantEntity>(p => p.UserId == 3 && p.ChatId == chatEntity.Id && p.IsAdmin == false)), Times.Once);

        _mockParticipantRepository.Verify(r => r.AddAsync(It.IsAny<ChatParticipantEntity>()), Times.Exactly(3));
    }

    [Fact]
    public async Task UpdateChatAsync_UpdatesExistingChat()
    {
        // Arrange
        var updateChatDto = new UpdateChatDto { Id = 1, Name = "UpdatedName", Description = "New Desc" };
        var chatEntity = new ChatEntity { Id = 1, Name = "OldName", Description = "Old Desc" };
        _mockChatRepository.Setup(r => r.GetByIdAsync(updateChatDto.Id)).ReturnsAsync(chatEntity);
        _mockMapper.Setup(m => m.Map(updateChatDto, chatEntity)).Returns(chatEntity);
        _mockChatRepository.Setup(r => r.UpdateAsync(It.IsAny<ChatEntity>())).Returns(Task.CompletedTask);

        // Act
        await _chatService.UpdateChatAsync(updateChatDto);

        // Assert
        _mockChatRepository.Verify(r => r.GetByIdAsync(updateChatDto.Id), Times.Once);
        _mockMapper.Verify(m => m.Map(updateChatDto, chatEntity), Times.Once);
        _mockChatRepository.Verify(r => r.UpdateAsync(chatEntity), Times.Once);
    }

    [Fact]
    public async Task UpdateChatAsync_ChatDoesNotExist_ThrowsException()
    {
        // Arrange
        var updateChatDto = new UpdateChatDto { Id = 99, Name = "NonExistent" };
        _mockChatRepository.Setup(r => r.GetByIdAsync(updateChatDto.Id)).ReturnsAsync((ChatEntity)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _chatService.UpdateChatAsync(updateChatDto));
        _mockChatRepository.Verify(r => r.GetByIdAsync(updateChatDto.Id), Times.Once);
        _mockChatRepository.Verify(r => r.UpdateAsync(It.IsAny<ChatEntity>()), Times.Never);
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
    public async Task DeleteChatAsync_DeletesChat()
    {
        // Arrange
        var chatId = 1;
        _mockChatRepository.Setup(r => r.DeleteAsync(chatId)).Returns(Task.CompletedTask);

        // Act
        await _chatService.DeleteChatAsync(chatId);

        // Assert
        _mockChatRepository.Verify(r => r.DeleteAsync(chatId), Times.Once);
    }

    [Fact]
    public async Task AddParticipantAsync_AddsParticipant()
    {
        // Arrange
        var addParticipantDto = new AddParticipantDto { ChatId = 1, UserId = 2, IsAdmin = false };
        var participantEntity = new ChatParticipantEntity();
        _mockMapper.Setup(m => m.Map<ChatParticipantEntity>(It.IsAny<AddParticipantDto>())).Returns(participantEntity);
        _mockParticipantRepository.Setup(r => r.AddAsync(It.IsAny<ChatParticipantEntity>())).Returns(Task.CompletedTask);

        // Act
        await _chatService.AddParticipantAsync(addParticipantDto);

        // Assert
        _mockMapper.Verify(m => m.Map<ChatParticipantEntity>(addParticipantDto), Times.Once);
        _mockParticipantRepository.Verify(r => r.AddAsync(It.Is<ChatParticipantEntity>(p => p.JoinedAt != default)), Times.Once);
    }

    [Fact]
    public async Task RemoveParticipantAsync_RemovesParticipant()
    {
        // Arrange
        var participantId = 1;
        _mockParticipantRepository.Setup(r => r.DeleteAsync(participantId)).Returns(Task.CompletedTask);

        // Act
        await _chatService.RemoveParticipantAsync(participantId);

        // Assert
        _mockParticipantRepository.Verify(r => r.DeleteAsync(participantId), Times.Once);
    }

    [Fact]
    public async Task GetParticipantsByChatIdAsync_ReturnsParticipants()
    {
        // Arrange
        var chatId = 1;
        var participantEntities = new List<UserEntity> { new UserEntity(), new UserEntity() };
        var userDtos = new List<UserDto> { new UserDto(), new UserDto() };
        _mockChatRepository.Setup(r => r.GetParticipantsByChatIdAsync(chatId)).ReturnsAsync(participantEntities);
        _mockMapper.Setup(m => m.Map<IEnumerable<UserDto>>(It.IsAny<IEnumerable<UserEntity>>())).Returns(userDtos);

        // Act
        var result = await _chatService.GetParticipantsByChatIdAsync(chatId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userDtos.Count, result.Count());
        _mockChatRepository.Verify(r => r.GetParticipantsByChatIdAsync(chatId), Times.Once);
        _mockMapper.Verify(m => m.Map<IEnumerable<UserDto>>(participantEntities), Times.Once);
    }

    [Fact]
    public async Task GetParticipantsByChatIdAsync_NoParticipants_ReturnsEmptyList()
    {
        // Arrange
        var chatId = 1;
        _mockChatRepository.Setup(r => r.GetParticipantsByChatIdAsync(chatId)).ReturnsAsync(Enumerable.Empty<UserEntity>());

        // Act
        var result = await _chatService.GetParticipantsByChatIdAsync(chatId);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
        _mockChatRepository.Verify(r => r.GetParticipantsByChatIdAsync(chatId), Times.Once);
        _mockMapper.Verify(m => m.Map<IEnumerable<UserDto>>(It.IsAny<IEnumerable<UserEntity>>()), Times.Never);
        _mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Warning),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                null,
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
            Times.Once);
    }

    [Fact]
    public async Task GetUserChatsAsync_ReturnsChatsForUser()
    {
        // Arrange
        var userId = 1;
        var chatEntities = new List<ChatEntity> { new ChatEntity(), new ChatEntity() };
        var chatDtos = new List<ChatDto> { new ChatDto(), new ChatDto() };
        _mockChatRepository.Setup(r => r.GetChatsByUserIdAsync(userId)).ReturnsAsync(chatEntities);
        _mockMapper.Setup(m => m.Map<List<ChatDto>>(It.IsAny<List<ChatEntity>>())).Returns(chatDtos);

        // Act
        var result = await _chatService.GetUserChatsAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(chatDtos.Count, result.Count());
        _mockChatRepository.Verify(r => r.GetChatsByUserIdAsync(userId), Times.Once);
        _mockMapper.Verify(m => m.Map<List<ChatDto>>(chatEntities), Times.Once);
    }
}