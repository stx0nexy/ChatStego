using Moq;
using Microsoft.Extensions.Logging;
using PixChat.Application.Services;
using PixChat.Core.Interfaces.Repositories;
using AutoMapper;
using PixChat.Application.DTOs;
using PixChat.Core.Entities;

namespace PixChat.Tests;


public class OneTimeMessageServiceTests
{
    private readonly Mock<ILogger<OneTimeMessageService>> _loggerMock;
    private readonly Mock<IOneTimeMessageRepository> _oneTimeMessageRepositoryMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly OneTimeMessageService _oneTimeMessageService;

    public OneTimeMessageServiceTests()
    {
        _loggerMock = new Mock<ILogger<OneTimeMessageService>>();
        _oneTimeMessageRepositoryMock = new Mock<IOneTimeMessageRepository>();
        _mapperMock = new Mock<IMapper>();

        _oneTimeMessageService = new OneTimeMessageService(
            _loggerMock.Object,
            _oneTimeMessageRepositoryMock.Object,
            _mapperMock.Object
        );
    }

    private OneTimeMessageDto CreateDummyOneTimeMessageDto(string id = null, string receiverId = "receiver@example.com")
    {
        return new OneTimeMessageDto
        {
            Id = id ?? Guid.NewGuid().ToString(),
            SenderId = "sender@example.com",
            ReceiverId = receiverId,
            ChatId = 1,
            StegoImage = new byte[] { 0x01, 0x02, 0x03 },
            EncryptionKey = "someKey",
            MessageLength = 10,
            CreatedAt = DateTime.UtcNow,
            Received = false,
            Read = false
        };
    }

    private OneTimeMessage CreateDummyOneTimeMessageEntity(string id = null, string receiverId = "receiver@example.com")
    {
        return new OneTimeMessage
        {
            Id = id ?? Guid.NewGuid().ToString(),
            SenderId = "sender@example.com",
            ReceiverId = receiverId,
            ChatId = 1,
            StegoImage = new byte[] { 0x01, 0x02, 0x03 },
            CreatedAt = DateTime.UtcNow,
            Received = false,
        };
    }

    [Fact]
    public async Task GetMessageByIdAsync_ReturnsMappedMessage_WhenFound()
    {
        // Arrange
        string messageId = "testMsgId";
        var entity = CreateDummyOneTimeMessageEntity(messageId);
        var dto = CreateDummyOneTimeMessageDto(messageId);

        _oneTimeMessageRepositoryMock.Setup(r => r.GetByIdAsync(messageId)).ReturnsAsync(entity);
        _mapperMock.Setup(m => m.Map<OneTimeMessageDto?>(entity)).Returns(dto);

        // Act
        var result = await _oneTimeMessageService.GetMessageByIdAsync(messageId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(dto.Id, result.Id);
        _oneTimeMessageRepositoryMock.Verify(r => r.GetByIdAsync(messageId), Times.Once);
        _mapperMock.Verify(m => m.Map<OneTimeMessageDto?>(entity), Times.Once);
    }

    [Fact]
    public async Task GetMessageByIdAsync_ReturnsNull_WhenNotFound()
    {
        // Arrange
        string messageId = "nonExistentMsgId";
        _oneTimeMessageRepositoryMock.Setup(r => r.GetByIdAsync(messageId)).ReturnsAsync((OneTimeMessage)null);
        _mapperMock.Setup(m => m.Map<OneTimeMessageDto?>(It.IsAny<OneTimeMessage>())).Returns((OneTimeMessageDto)null);

        // Act
        var result = await _oneTimeMessageService.GetMessageByIdAsync(messageId);

        // Assert
        Assert.Null(result);
        _oneTimeMessageRepositoryMock.Verify(r => r.GetByIdAsync(messageId), Times.Once);
        _mapperMock.Verify(m => m.Map<OneTimeMessageDto?>(It.IsAny<OneTimeMessage>()), Times.Once);
        _loggerMock.Verify(
            x => x.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
            Times.Never
        );
    }

    [Fact]
    public async Task GetMessagesByReceiverIdAsync_ReturnsMappedMessages_WhenFound()
    {
        // Arrange
        string receiverId = "receiver1@example.com";
        var entityList = new List<OneTimeMessage>
        {
            CreateDummyOneTimeMessageEntity(Guid.NewGuid().ToString(), receiverId),
            CreateDummyOneTimeMessageEntity(Guid.NewGuid().ToString(), receiverId)
        };
        var dtoList = new List<OneTimeMessageDto>
        {
            CreateDummyOneTimeMessageDto(entityList[0].Id, receiverId),
            CreateDummyOneTimeMessageDto(entityList[1].Id, receiverId)
        };

        _oneTimeMessageRepositoryMock.Setup(r => r.GetByReceiverIdAsync(receiverId)).ReturnsAsync(entityList);
        _mapperMock.Setup(m => m.Map<OneTimeMessageDto>(It.IsAny<OneTimeMessage>()))
                   .Returns((OneTimeMessage src) => dtoList.First(d => d.Id == src.Id));

        // Act
        var result = await _oneTimeMessageService.GetMessagesByReceiverIdAsync(receiverId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(entityList.Count, result.Count());
        _oneTimeMessageRepositoryMock.Verify(r => r.GetByReceiverIdAsync(receiverId), Times.Once);
        _mapperMock.Verify(m => m.Map<OneTimeMessageDto>(It.IsAny<OneTimeMessage>()), Times.Exactly(entityList.Count));
    }
    
    [Fact]
    public async Task GetMessagesByReceiverIdAsync_ThrowsException_WhenRepositoryThrows()
    {
        // Arrange
        string receiverId = "errorReceiver@example.com";
        var expectedException = new InvalidOperationException("DB error during fetch by receiver ID");
        _oneTimeMessageRepositoryMock.Setup(r => r.GetByReceiverIdAsync(receiverId)).ThrowsAsync(expectedException);

        // Act & Assert
        var caughtException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _oneTimeMessageService.GetMessagesByReceiverIdAsync(receiverId)
        );

        Assert.Equal(expectedException, caughtException);
        _oneTimeMessageRepositoryMock.Verify(r => r.GetByReceiverIdAsync(receiverId), Times.Once);
        _mapperMock.Verify(m => m.Map<OneTimeMessageDto>(It.IsAny<OneTimeMessage>()), Times.Never);
        _loggerMock.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Error),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains($"Error occurred while fetching one-time messages for receiver: {receiverId}.")),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
            Times.Once
        );
    }

    [Fact]
    public async Task SendMessageAsync_CallsRepositoryAddAsync_OnSuccess()
    {
        // Arrange
        string? senderId = "sender@test.com";
        string receiverId = "receiver@test.com";
        int chatId = 10;
        byte[] stegoImage = new byte[] { 0x01, 0x02 };
        DateTime createdAt = DateTime.UtcNow;
        bool received = false;
        string expectedMessageId = Guid.NewGuid().ToString();

        _oneTimeMessageRepositoryMock.Setup(r => r.AddAsync(It.IsAny<OneTimeMessage>()))
            .ReturnsAsync(expectedMessageId)
            .Callback<OneTimeMessage>(msg =>
            {
                Assert.Equal(senderId, msg.SenderId);
                Assert.Equal(receiverId, msg.ReceiverId);
                Assert.Equal(chatId, msg.ChatId);
                Assert.Equal(stegoImage, msg.StegoImage);
                Assert.Equal(createdAt, msg.CreatedAt);
                Assert.Equal(received, msg.Received);
            });

        // Act
        string resultId = await _oneTimeMessageService.SendMessageAsync(senderId, receiverId, chatId, stegoImage, createdAt, received);

        // Assert
        Assert.Equal(expectedMessageId, resultId);
        _oneTimeMessageRepositoryMock.Verify(r => r.AddAsync(It.IsAny<OneTimeMessage>()), Times.Once);
        _loggerMock.Verify(
            x => x.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
            Times.Never
        );
    }

    [Fact]
    public async Task SendMessageAsync_ThrowsException_WhenRepositoryThrows()
    {
        // Arrange
        string receiverId = "receiver@error.com";
        var expectedException = new InvalidOperationException("DB error during add");
        _oneTimeMessageRepositoryMock.Setup(r => r.AddAsync(It.IsAny<OneTimeMessage>())).ThrowsAsync(expectedException);

        // Act & Assert
        var caughtException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _oneTimeMessageService.SendMessageAsync(null, receiverId, 0, null, DateTime.UtcNow, false)
        );

        Assert.Equal(expectedException, caughtException);
        _oneTimeMessageRepositoryMock.Verify(r => r.AddAsync(It.IsAny<OneTimeMessage>()), Times.Once);
        _loggerMock.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Error),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains($"Error occurred while sending one-time message for receiver: {receiverId}.")),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
            Times.Once
        );
    }

    [Fact]
    public async Task DeleteMessageAsync_CallsRepositoryDeleteAsync_OnSuccess()
    {
        // Arrange
        string messageId = "msgToDeleteId";
        _oneTimeMessageRepositoryMock.Setup(r => r.DeleteAsync(messageId)).Returns(Task.CompletedTask);

        // Act
        await _oneTimeMessageService.DeleteMessageAsync(messageId);

        // Assert
        _oneTimeMessageRepositoryMock.Verify(r => r.DeleteAsync(messageId), Times.Once);
        _loggerMock.Verify(
            x => x.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
            Times.Never
        );
    }
    
    [Fact]
    public async Task MarkOneTimeMessageAsReceivedAsync_CallsRepository_OnSuccess()
    {
        // Arrange
        string messageId = "msgToMarkReceived";
        _oneTimeMessageRepositoryMock.Setup(r => r.MarkOneTimeMessageAsReceivedAsync(messageId)).Returns(Task.CompletedTask);

        // Act
        await _oneTimeMessageService.MarkOneTimeMessageAsReceivedAsync(messageId);

        // Assert
        _oneTimeMessageRepositoryMock.Verify(r => r.MarkOneTimeMessageAsReceivedAsync(messageId), Times.Once);
        _loggerMock.Verify(
            x => x.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
            Times.Never
        );
    }

    [Fact]
    public async Task MarkOneTimeMessageAsReceivedAsync_ThrowsException_WhenRepositoryThrows()
    {
        // Arrange
        string messageId = "errorMarkReceived";
        var expectedException = new InvalidOperationException("Repository error during mark as received");
        _oneTimeMessageRepositoryMock.Setup(r => r.MarkOneTimeMessageAsReceivedAsync(messageId)).ThrowsAsync(expectedException);

        // Act & Assert
        var caughtException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _oneTimeMessageService.MarkOneTimeMessageAsReceivedAsync(messageId)
        );

        Assert.Equal(expectedException, caughtException);
        _oneTimeMessageRepositoryMock.Verify(r => r.MarkOneTimeMessageAsReceivedAsync(messageId), Times.Once);
        _loggerMock.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Error),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains($"Error occurred while marking one-time message with ID: {messageId} as received.")),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
            Times.Once
        );
    }
}
