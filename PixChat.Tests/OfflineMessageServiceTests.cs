using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using PixChat.Application.Interfaces.Services;
using PixChat.Application.Services;
using PixChat.Core.Interfaces.Repositories;
using PixChat.Application.DTOs;
using PixChat.Core.Entities;
using AutoMapper;

namespace PixChat.Tests;

public class OfflineMessageServiceTests
{
    private readonly Mock<ILogger<OfflineMessageService>> _loggerMock;
    private readonly Mock<IOfflineMessageRepository> _offlineMessageRepositoryMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly OfflineMessageService _offlineMessageService;

    public OfflineMessageServiceTests()
    {
        _loggerMock = new Mock<ILogger<OfflineMessageService>>();
        _offlineMessageRepositoryMock = new Mock<IOfflineMessageRepository>();
        _mapperMock = new Mock<IMapper>();

        _offlineMessageService = new OfflineMessageService(
            _loggerMock.Object,
            _offlineMessageRepositoryMock.Object,
            _mapperMock.Object
        );
    }
    private OfflineMessageDto CreateDummyMessageDto(string receiverId = "test@example.com")
    {
        return new OfflineMessageDto
        {
            Id = Guid.NewGuid().ToString(),
            SenderId = "sender@example.com",
            ReceiverId = receiverId,
            ChatId = 123,
            StegoImage = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF },
            CreatedAt = DateTime.UtcNow,
            Received = false,
            IsGroup = false,
        };
    }

    private OfflineMessageEntity CreateDummyMessageEntity(string receiverId = "test@example.com", string? messageId = null)
    {
        return new OfflineMessageEntity
        {
            SenderId = "sender@example.com",
            ReceiverId = receiverId,
            ChatId = 123,
            StegoImage = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF },
            CreatedAt = DateTime.UtcNow,
            Received = false,
            IsGroup = false,
        };
    }

    private OfflineMessageFileDto CreateDummyFileMessageDto(string receiverId = "file_receiver@example.com")
    {
        return new OfflineMessageFileDto
        {
            Id = Guid.NewGuid().ToString(),
            SenderId = "file_sender@example.com",
            ReceiverId = receiverId,
            ChatId = 456,
            FileData = "dummyBase64Data",
            FileName = "test.txt",
            FileType = "text/plain",
            EncryptedAESKey = "encryptedAESKeyBase64",
            AESIV = "aesIVBase64",
            CreatedAt = DateTime.UtcNow,
            Received = false,
            IsGroup = false,
            IsFile = true,
        };
    }

    private OfflineMessageFileEntity CreateDummyFileMessageEntity(string receiverId = "file_receiver@example.com", string? messageId = null)
    {
        return new OfflineMessageFileEntity
        {
            SenderId = "file_sender@example.com",
            ReceiverId = receiverId,
            ChatId = 456,
            FileData = "dummyBase64Data",
            FileName = "test.txt",
            FileType = "text/plain",
            EncryptedAESKey = "encryptedAESKeyBase64",
            AESIV = "aesIVBase64",
            CreatedAt = DateTime.UtcNow,
            Received = false,
            IsGroup = false,
            IsFile = true,
        };
    }


    [Fact]
    public async Task SaveMessageAsync_MessageDto_CallsRepositoryAndMapper_OnSuccess()
    {
        // Arrange
        var messageDto = CreateDummyMessageDto();
        var messageEntity = CreateDummyMessageEntity();
        _mapperMock.Setup(m => m.Map<OfflineMessageEntity>(messageDto)).Returns(messageEntity);
        _offlineMessageRepositoryMock.Setup(r => r.SaveMessageAsync(messageEntity)).Returns(Task.CompletedTask);

        // Act
        await _offlineMessageService.SaveMessageAsync(messageDto);

        // Assert
        _mapperMock.Verify(m => m.Map<OfflineMessageEntity>(messageDto), Times.Once);
        _offlineMessageRepositoryMock.Verify(r => r.SaveMessageAsync(messageEntity), Times.Once);
        _loggerMock.Verify(
            x => x.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
            Times.Never
        );
    }

    [Fact]
    public async Task SaveMessageAsync_MessageDto_ThrowsException_WhenRepositoryThrows()
    {
        // Arrange
        var messageDto = CreateDummyMessageDto();
        var messageEntity = CreateDummyMessageEntity();
        var expectedException = new InvalidOperationException("DB write error");
        _mapperMock.Setup(m => m.Map<OfflineMessageEntity>(messageDto)).Returns(messageEntity);
        _offlineMessageRepositoryMock.Setup(r => r.SaveMessageAsync(messageEntity)).ThrowsAsync(expectedException);

        // Act & Assert
        var caughtException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _offlineMessageService.SaveMessageAsync(messageDto)
        );

        Assert.Equal(expectedException, caughtException);
        _mapperMock.Verify(m => m.Map<OfflineMessageEntity>(messageDto), Times.Once);
        _offlineMessageRepositoryMock.Verify(r => r.SaveMessageAsync(messageEntity), Times.Once);
        _loggerMock.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Error),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains($"Error occurred while saving offline message for receiver: {messageDto.ReceiverId}.")),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
            Times.Once
        );
    }

    [Fact]
    public async Task SaveMessageAsync_FileDto_CallsRepositoryAndMapper_OnSuccess()
    {
        // Arrange
        var fileMessageDto = CreateDummyFileMessageDto();
        var fileMessageEntity = CreateDummyFileMessageEntity();
        _mapperMock.Setup(m => m.Map<OfflineMessageFileEntity>(fileMessageDto)).Returns(fileMessageEntity);
        _offlineMessageRepositoryMock.Setup(r => r.SaveMessageAsync(fileMessageEntity)).Returns(Task.CompletedTask);

        // Act
        await _offlineMessageService.SaveMessageAsync(fileMessageDto);

        // Assert
        _mapperMock.Verify(m => m.Map<OfflineMessageFileEntity>(fileMessageDto), Times.Once);
        _offlineMessageRepositoryMock.Verify(r => r.SaveMessageAsync(fileMessageEntity), Times.Once);
        _loggerMock.Verify(
            x => x.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
            Times.Never
        );
    }

    [Fact]
    public async Task SaveMessageAsync_FileDto_ThrowsException_WhenRepositoryThrows()
    {
        // Arrange
        var fileMessageDto = CreateDummyFileMessageDto();
        var fileMessageEntity = CreateDummyFileMessageEntity();
        var expectedException = new ApplicationException("File DB write error");
        _mapperMock.Setup(m => m.Map<OfflineMessageFileEntity>(fileMessageDto)).Returns(fileMessageEntity);
        _offlineMessageRepositoryMock.Setup(r => r.SaveMessageAsync(fileMessageEntity)).ThrowsAsync(expectedException);

        // Act & Assert
        var caughtException = await Assert.ThrowsAsync<ApplicationException>(
            () => _offlineMessageService.SaveMessageAsync(fileMessageDto)
        );

        Assert.Equal(expectedException, caughtException);
        _mapperMock.Verify(m => m.Map<OfflineMessageFileEntity>(fileMessageDto), Times.Once);
        _offlineMessageRepositoryMock.Verify(r => r.SaveMessageAsync(fileMessageEntity), Times.Once);
        _loggerMock.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Error),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains($"Error occurred while saving offline file message for receiver: {fileMessageDto.ReceiverId}.")),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
            Times.Once
        );
    }

    [Fact]
    public async Task GetPendingMessagesAsync_ReturnsMappedMessages_WhenFound()
    {
        // Arrange
        string receiverId = "recipient@example.com";
        var entityList = new List<OfflineMessageEntity>
        {
            CreateDummyMessageEntity(receiverId, "msg1"),
            CreateDummyMessageEntity(receiverId, "msg2")
        };
        var dtoList = new List<OfflineMessageDto>
        {
            CreateDummyMessageDto(receiverId),
            CreateDummyMessageDto(receiverId)
        };

        _offlineMessageRepositoryMock.Setup(r => r.GetPendingMessagesAsync(receiverId)).ReturnsAsync(entityList);
        _mapperMock.Setup(m => m.Map<OfflineMessageDto>(It.IsAny<OfflineMessageEntity>()))
                   .Returns((OfflineMessageEntity src) => new OfflineMessageDto { Id = Guid.NewGuid().ToString(), ReceiverId = src.ReceiverId, SenderId = src.SenderId, ChatId = src.ChatId, StegoImage = src.StegoImage, CreatedAt = src.CreatedAt, Received = src.Received, IsGroup = src.IsGroup });

        // Act
        var result = await _offlineMessageService.GetPendingMessagesAsync(receiverId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(entityList.Count, result.Count());
        _offlineMessageRepositoryMock.Verify(r => r.GetPendingMessagesAsync(receiverId), Times.Once);
        _mapperMock.Verify(m => m.Map<OfflineMessageDto>(It.IsAny<OfflineMessageEntity>()), Times.Exactly(entityList.Count));
    }

    [Fact]
    public async Task GetPendingMessagesAsync_ThrowsException_WhenRepositoryThrows()
    {
        // Arrange
        string receiverId = "error_receiver@example.com";
        var expectedException = new InvalidOperationException("Repository error during fetch");
        _offlineMessageRepositoryMock.Setup(r => r.GetPendingMessagesAsync(receiverId)).ThrowsAsync(expectedException);

        // Act & Assert
        var caughtException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _offlineMessageService.GetPendingMessagesAsync(receiverId)
        );

        Assert.Equal(expectedException, caughtException);
        _offlineMessageRepositoryMock.Verify(r => r.GetPendingMessagesAsync(receiverId), Times.Once);
        _loggerMock.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Error),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains($"Error occurred while fetching pending messages for receiver: {receiverId}.")),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
            Times.Once
        );
    }
    
    [Fact]
    public async Task GetPendingFileMessagesAsync_ReturnsMappedMessages_WhenFound()
    {
        // Arrange
        string receiverId = "file_recipient@example.com";
        var entityList = new List<OfflineMessageFileEntity>
        {
            CreateDummyFileMessageEntity(receiverId, "file_msg1"),
            CreateDummyFileMessageEntity(receiverId, "file_msg2")
        };
        var dtoList = new List<OfflineMessageFileDto>
        {
            CreateDummyFileMessageDto(receiverId),
            CreateDummyFileMessageDto(receiverId)
        };

        _offlineMessageRepositoryMock.Setup(r => r.GetPendingFileMessagesAsync(receiverId)).ReturnsAsync(entityList);
        _mapperMock.Setup(m => m.Map<OfflineMessageFileDto>(It.IsAny<OfflineMessageFileEntity>()))
                   .Returns((OfflineMessageFileEntity src) => new OfflineMessageFileDto { Id = Guid.NewGuid().ToString(), ReceiverId = src.ReceiverId, SenderId = src.SenderId, ChatId = src.ChatId, FileData = src.FileData, FileName = src.FileName, FileType = src.FileType, EncryptedAESKey = src.EncryptedAESKey, AESIV = src.AESIV, CreatedAt = src.CreatedAt, Received = src.Received, IsGroup = src.IsGroup, IsFile = src.IsFile }); 

        // Act
        var result = await _offlineMessageService.GetPendingFileMessagesAsync(receiverId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(entityList.Count, result.Count());
        _offlineMessageRepositoryMock.Verify(r => r.GetPendingFileMessagesAsync(receiverId), Times.Once);
        _mapperMock.Verify(m => m.Map<OfflineMessageFileDto>(It.IsAny<OfflineMessageFileEntity>()), Times.Exactly(entityList.Count));
    }

    [Fact]
    public async Task GetPendingFileMessagesAsync_ThrowsException_WhenRepositoryThrows()
    {
        // Arrange
        string receiverId = "error_file_receiver@example.com";
        var expectedException = new InvalidOperationException("Repository error during file fetch");
        _offlineMessageRepositoryMock.Setup(r => r.GetPendingFileMessagesAsync(receiverId)).ThrowsAsync(expectedException);

        // Act & Assert
        var caughtException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _offlineMessageService.GetPendingFileMessagesAsync(receiverId)
        );

        Assert.Equal(expectedException, caughtException);
        _offlineMessageRepositoryMock.Verify(r => r.GetPendingFileMessagesAsync(receiverId), Times.Once);
        _loggerMock.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Error),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains($"Error occurred while fetching pending file messages for receiver: {receiverId}.")),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
            Times.Once
        );
    }

    [Fact]
    public async Task DeleteMessageAsync_CallsRepository_OnSuccess()
    {
        // Arrange
        string messageId = "msg_to_delete_123";
        _offlineMessageRepositoryMock.Setup(r => r.DeleteMessageAsync(messageId)).Returns(Task.CompletedTask);

        // Act
        await _offlineMessageService.DeleteMessageAsync(messageId);

        // Assert
        _offlineMessageRepositoryMock.Verify(r => r.DeleteMessageAsync(messageId), Times.Once);
        _loggerMock.Verify(
            x => x.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
            Times.Never
        );
    }

    [Fact]
    public async Task DeleteMessageAsync_ThrowsException_WhenRepositoryThrows()
    {
        // Arrange
        string messageId = "msg_to_delete_error";
        var expectedException = new InvalidOperationException("Repository error during delete");
        _offlineMessageRepositoryMock.Setup(r => r.DeleteMessageAsync(messageId)).ThrowsAsync(expectedException);

        // Act & Assert
        var caughtException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _offlineMessageService.DeleteMessageAsync(messageId)
        );

        Assert.Equal(expectedException, caughtException);
        _offlineMessageRepositoryMock.Verify(r => r.DeleteMessageAsync(messageId), Times.Once);
        _loggerMock.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Error),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains($"Error occurred while deleting offline message with ID: {messageId}.")),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
            Times.Once
        );
    }

    [Fact]
    public async Task MarkMessageAsReceivedAsync_CallsRepository_OnSuccess()
    {
        // Arrange
        string messageId = "msg_to_mark_123";
        _offlineMessageRepositoryMock.Setup(r => r.MarkMessageAsReceivedAsync(messageId)).Returns(Task.CompletedTask);

        // Act
        await _offlineMessageService.MarkMessageAsReceivedAsync(messageId);

        // Assert
        _offlineMessageRepositoryMock.Verify(r => r.MarkMessageAsReceivedAsync(messageId), Times.Once);
        _loggerMock.Verify(
            x => x.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
            Times.Never
        );
    }
}