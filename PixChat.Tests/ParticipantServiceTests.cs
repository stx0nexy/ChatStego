using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using PixChat.Application.Services;
using PixChat.Core.Interfaces.Repositories;
using PixChat.Application.DTOs;
using PixChat.Core.Entities;
using AutoMapper;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace PixChat.Tests;

public class ParticipantServiceTests
{
    private readonly Mock<ILogger<ParticipantService>> _mockLogger;
    private readonly Mock<IChatParticipantRepository> _mockChatParticipantRepository;
    private readonly Mock<IMapper> _mockMapper;
    private readonly ParticipantService _participantService;

    public ParticipantServiceTests()
    {
        _mockLogger = new Mock<ILogger<ParticipantService>>();
        _mockChatParticipantRepository = new Mock<IChatParticipantRepository>();
        _mockMapper = new Mock<IMapper>();
        _participantService = new ParticipantService(_mockLogger.Object, _mockChatParticipantRepository.Object, _mockMapper.Object);
    }

    [Fact]
    public async Task GetParticipantsByChatIdAsync_ReturnsParticipants()
    {
        // Arrange
        var chatId = 1;
        var participantEntities = new List<ChatParticipantEntity>
        {
            new ChatParticipantEntity { Id = 1, ChatId = chatId, UserId = 10, IsAdmin = true },
            new ChatParticipantEntity { Id = 2, ChatId = chatId, UserId = 11, IsAdmin = false }
        };
        var participantDtos = new List<ChatParticipantDto>
        {
            new ChatParticipantDto { Id = 1, ChatId = chatId, UserId = 10, IsAdmin = true },
            new ChatParticipantDto { Id = 2, ChatId = chatId, UserId = 11, IsAdmin = false }
        };

        _mockChatParticipantRepository.Setup(r => r.GetParticipantsByChatIdAsync(chatId)).ReturnsAsync(participantEntities);
        _mockMapper.Setup(m => m.Map<IEnumerable<ChatParticipantDto>>(participantEntities)).Returns(participantDtos);

        // Act
        var result = await _participantService.GetParticipantsByChatIdAsync(chatId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(participantDtos.Count, result.Count());
        Assert.Equal(participantDtos, result);
        _mockChatParticipantRepository.Verify(r => r.GetParticipantsByChatIdAsync(chatId), Times.Once);
        _mockMapper.Verify(m => m.Map<IEnumerable<ChatParticipantDto>>(participantEntities), Times.Once);
        _mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Error),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Never);
    }

    [Fact]
    public async Task AddParticipantAsync_AddsParticipant()
    {
        // Arrange
        var addParticipantDto = new AddParticipantDto
        {
            ChatId = 1,
            UserId = 10,
            IsAdmin = false
        };
        var participantEntity = new ChatParticipantEntity
        {
            ChatId = 1,
            UserId = 10,
            IsAdmin = false,
            JoinedAt = DateTime.UtcNow
        };

        _mockMapper.Setup(m => m.Map<ChatParticipantEntity>(It.IsAny<AddParticipantDto>()))
                   .Returns(participantEntity);
        _mockChatParticipantRepository.Setup(r => r.AddAsync(It.IsAny<ChatParticipantEntity>())).Returns(Task.CompletedTask);

        // Act
        await _participantService.AddParticipantAsync(addParticipantDto);

        // Assert
        _mockMapper.Verify(m => m.Map<ChatParticipantEntity>(addParticipantDto), Times.Once);
        _mockChatParticipantRepository.Verify(r => r.AddAsync(It.Is<ChatParticipantEntity>(
            p => p.ChatId == addParticipantDto.ChatId &&
                 p.UserId == addParticipantDto.UserId &&
                 p.IsAdmin == addParticipantDto.IsAdmin &&
                 p.JoinedAt != default
        )), Times.Once);
        _mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Error),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Never);
    }

    [Fact]
    public async Task RemoveParticipantAsync_RemovesParticipant()
    {
        // Arrange
        var participantId = 1;
        _mockChatParticipantRepository.Setup(r => r.DeleteAsync(participantId)).Returns(Task.CompletedTask);

        // Act
        await _participantService.RemoveParticipantAsync(participantId);

        // Assert
        _mockChatParticipantRepository.Verify(r => r.DeleteAsync(participantId), Times.Once);
        _mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Error),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Never);
    }

    [Fact]
    public async Task UpdateParticipantAsync_UpdatesParticipant()
    {
        // Arrange
        var updateParticipantDto = new UpdateParticipantDto
        {
            Id = 1,
            IsAdmin = true
        };
        var existingParticipantEntity = new ChatParticipantEntity
        {
            Id = 1,
            ChatId = 10,
            UserId = 100,
            IsAdmin = false,
            JoinedAt = DateTime.UtcNow
        };

        _mockChatParticipantRepository.Setup(r => r.GetByIdAsync(updateParticipantDto.Id)).ReturnsAsync(existingParticipantEntity);
        _mockChatParticipantRepository.Setup(r => r.UpdateAsync(It.IsAny<ChatParticipantEntity>())).Returns(Task.CompletedTask);
        _mockMapper.Setup(m => m.Map(updateParticipantDto, existingParticipantEntity));

        // Act
        await _participantService.UpdateParticipantAsync(updateParticipantDto);

        // Assert
        _mockChatParticipantRepository.Verify(r => r.GetByIdAsync(updateParticipantDto.Id), Times.Once);
        _mockChatParticipantRepository.Verify(r => r.UpdateAsync(It.Is<ChatParticipantEntity>(
            p => p.Id == updateParticipantDto.Id &&
                 p.IsAdmin == updateParticipantDto.IsAdmin
        )), Times.Once);
        _mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Warning),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Never);
        _mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Error),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Never);
    }

    [Fact]
    public async Task UpdateParticipantAsync_ParticipantDoesNotExist_ThrowsKeyNotFoundException()
    {
        // Arrange
        var updateParticipantDto = new UpdateParticipantDto
        {
            Id = 999,
            IsAdmin = true
        };

        _mockChatParticipantRepository.Setup(r => r.GetByIdAsync(updateParticipantDto.Id)).ReturnsAsync((ChatParticipantEntity)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() => _participantService.UpdateParticipantAsync(updateParticipantDto));
        Assert.Equal($"Participant with ID {updateParticipantDto.Id} not found.", exception.Message);

        _mockChatParticipantRepository.Verify(r => r.GetByIdAsync(updateParticipantDto.Id), Times.Once);
        _mockChatParticipantRepository.Verify(r => r.UpdateAsync(It.IsAny<ChatParticipantEntity>()), Times.Never);
        _mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Warning),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
        _mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Error),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.Is<Exception>(ex => ex is KeyNotFoundException),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }
}