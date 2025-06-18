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

public class ContactServiceTests
{
    private readonly Mock<IContactRepository> _mockContactRepository;
    private readonly Mock<IChatService> _mockChatService;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<ILogger<ContactService>> _mockLogger;
    private readonly ContactService _contactService;

    public ContactServiceTests()
    {
        _mockContactRepository = new Mock<IContactRepository>();
        _mockChatService = new Mock<IChatService>();
        _mockMapper = new Mock<IMapper>();
        _mockLogger = new Mock<ILogger<ContactService>>();
        _contactService = new ContactService(
            _mockLogger.Object,
            _mockContactRepository.Object,
            _mockChatService.Object,
            _mockMapper.Object
        );
    }

    [Fact]
    public async Task AddContact_AddsContactAndCreatesPrivateChat_ChatDoesNotExist()
    {
        // Arrange
        var userId = 1;
        var contactUserId = 2;
        var isBlocked = false;
        _mockContactRepository.Setup(r => r.AddContactAsync(userId, contactUserId, isBlocked)).Returns(Task.CompletedTask);
        _mockChatService.Setup(s => s.GetPrivateChatIfExists(userId, contactUserId)).ReturnsAsync((ChatDto)null);
        _mockChatService.Setup(s => s.CreatePrivateChatAsync(userId, contactUserId)).Returns(Task.CompletedTask);

        // Act
        await _contactService.AddContact(userId, contactUserId, isBlocked);

        // Assert
        _mockContactRepository.Verify(r => r.AddContactAsync(userId, contactUserId, isBlocked), Times.Once);
        _mockChatService.Verify(s => s.GetPrivateChatIfExists(userId, contactUserId), Times.Once);
        _mockChatService.Verify(s => s.CreatePrivateChatAsync(userId, contactUserId), Times.Once);
    }

    [Fact]
    public async Task AddContact_AddsContactAndDoesNotCreatePrivateChat_ChatExists()
    {
        // Arrange
        var userId = 1;
        var contactUserId = 2;
        var isBlocked = false;
        var existingChat = new ChatDto { Id = 10, IsGroup = false };
        _mockContactRepository.Setup(r => r.AddContactAsync(userId, contactUserId, isBlocked)).Returns(Task.CompletedTask);
        _mockChatService.Setup(s => s.GetPrivateChatIfExists(userId, contactUserId)).ReturnsAsync(existingChat);

        // Act
        await _contactService.AddContact(userId, contactUserId, isBlocked);

        // Assert
        _mockContactRepository.Verify(r => r.AddContactAsync(userId, contactUserId, isBlocked), Times.Once);
        _mockChatService.Verify(s => s.GetPrivateChatIfExists(userId, contactUserId), Times.Once);
        _mockChatService.Verify(s => s.CreatePrivateChatAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never); 
    }


    [Fact]
    public async Task GetContact_ContactExists_ReturnsContact()
    {
        // Arrange
        var userId = 1;
        var contactUserId = 2;
        var contactEntity = new ContactEntity { UserId = userId, ContactUserId = contactUserId };
        var contactDto = new ContactDto { UserId = userId, ContactUserId = contactUserId };
        _mockContactRepository.Setup(r => r.GetContactAsync(userId, contactUserId)).ReturnsAsync(contactEntity);
        _mockMapper.Setup(m => m.Map<ContactDto>(It.IsAny<ContactEntity>())).Returns(contactDto);

        // Act
        var result = await _contactService.GetContact(userId, contactUserId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.UserId);
        _mockContactRepository.Verify(r => r.GetContactAsync(userId, contactUserId), Times.Once);
        _mockMapper.Verify(m => m.Map<ContactDto>(contactEntity), Times.Once);
    }

    [Fact]
    public async Task GetContact_ContactDoesNotExist_ReturnsNull()
    {
        // Arrange
        var userId = 1;
        var contactUserId = 99;
        _mockContactRepository.Setup(r => r.GetContactAsync(userId, contactUserId)).ReturnsAsync((ContactEntity)null);

        _mockMapper.Setup(m => m.Map<ContactDto>(null)).Returns((ContactDto)null);

        // Act
        var result = await _contactService.GetContact(userId, contactUserId);

        // Assert
        Assert.Null(result);
        _mockContactRepository.Verify(r => r.GetContactAsync(userId, contactUserId), Times.Once);
        _mockMapper.Verify(m => m.Map<ContactDto>(It.Is<ContactEntity>(e => e == null)), Times.Once);
    }

    [Fact]
    public async Task IsContactBlocked_ReturnsCorrectStatus()
    {
        // Arrange
        var userId = 1;
        var contactUserId = 2;
        _mockContactRepository.Setup(r => r.IsContactBlockedAsync(userId, contactUserId)).ReturnsAsync(true);

        // Act
        var result = await _contactService.IsContactBlocked(userId, contactUserId);

        // Assert
        Assert.True(result);
        _mockContactRepository.Verify(r => r.IsContactBlockedAsync(userId, contactUserId), Times.Once);
    }

    [Fact]
    public async Task RemoveContact_RemovesContact()
    {
        // Arrange
        var userId = 1;
        var contactUserId = 2;
        _mockContactRepository.Setup(r => r.RemoveContactAsync(userId, contactUserId)).Returns(Task.CompletedTask);

        // Act
        await _contactService.RemoveContact(userId, contactUserId);

        // Assert
        _mockContactRepository.Verify(r => r.RemoveContactAsync(userId, contactUserId), Times.Once);
    }

    [Fact]
    public async Task GetAllContacts_ReturnsAllContacts()
    {
        // Arrange
        var userId = 1;
        var contactEntities = new List<ContactEntity>
        {
            new ContactEntity { UserId = userId, ContactUserId = 2, IsBlockedByUser = false, IsBlockedByContact = false },
            new ContactEntity { UserId = userId, ContactUserId = 3, IsBlockedByUser = false, IsBlockedByContact = false }
        };

        var contactDto1 = new ContactDto { UserId = userId, ContactUserId = 2, IsBlockedByUser = false, IsBlockedByContact = false };
        var contactDto2 = new ContactDto { UserId = userId, ContactUserId = 3, IsBlockedByUser = false, IsBlockedByContact = false };
        var expectedContactDtos = new List<ContactDto> { contactDto1, contactDto2 };

        _mockContactRepository.Setup(r => r.GetAllContactsAsync(userId)).ReturnsAsync(contactEntities);

        _mockMapper.Setup(m => m.Map<ContactDto>(contactEntities[0])).Returns(contactDto1);
        _mockMapper.Setup(m => m.Map<ContactDto>(contactEntities[1])).Returns(contactDto2);

        // Act
        var result = await _contactService.GetAllContacts(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedContactDtos.Count, result.Count());
        Assert.Contains(contactDto1, result);
        Assert.Contains(contactDto2, result);

        _mockContactRepository.Verify(r => r.GetAllContactsAsync(userId), Times.Once);

        _mockMapper.Verify(m => m.Map<ContactDto>(contactEntities[0]), Times.Once);
        _mockMapper.Verify(m => m.Map<ContactDto>(contactEntities[1]), Times.Once);
        _mockMapper.Verify(m => m.Map<ContactDto>(It.IsAny<ContactEntity>()), Times.Exactly(contactEntities.Count));
    }

    [Fact]
    public async Task UpdateContactBlockStatus_UpdatesStatus()
    {
        // Arrange
        var userId = 1;
        var contactUserId = 2;
        var isBlocked = true;
        _mockContactRepository.Setup(r => r.UpdateContactBlockStatusAsync(userId, contactUserId, isBlocked)).Returns(Task.CompletedTask);

        // Act
        await _contactService.UpdateContactBlockStatus(userId, contactUserId, isBlocked);

        // Assert
        _mockContactRepository.Verify(r => r.UpdateContactBlockStatusAsync(userId, contactUserId, isBlocked), Times.Once);
    }

    [Fact]
    public async Task GetBlockedContacts_ReturnsBlockedContacts()
    {
        // Arrange
        var userId = 1;
        var blockedContactEntities = new List<ContactEntity>
        {
            new ContactEntity { UserId = userId, ContactUserId = 4, IsBlockedByUser = false, IsBlockedByContact = false },
            new ContactEntity { UserId = userId, ContactUserId = 5, IsBlockedByUser = false, IsBlockedByContact = false }
        };
        var blockedContactDto1 = new ContactDto { UserId = userId, ContactUserId = 4, IsBlockedByUser = false, IsBlockedByContact = false };
        var blockedContactDto2 = new ContactDto { UserId = userId, ContactUserId = 5, IsBlockedByUser = false, IsBlockedByContact = false };
        var expectedBlockedContactDtos = new List<ContactDto> { blockedContactDto1, blockedContactDto2 };

        _mockContactRepository.Setup(r => r.GetBlockedContactsAsync(userId)).ReturnsAsync(blockedContactEntities);

        _mockMapper.Setup(m => m.Map<ContactDto>(blockedContactEntities[0])).Returns(blockedContactDto1);
        _mockMapper.Setup(m => m.Map<ContactDto>(blockedContactEntities[1])).Returns(blockedContactDto2);

        // Act
        var result = await _contactService.GetBlockedContacts(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedBlockedContactDtos.Count, result.Count());
        Assert.Contains(blockedContactDto1, result);
        Assert.Contains(blockedContactDto2, result);

        _mockContactRepository.Verify(r => r.GetBlockedContactsAsync(userId), Times.Once);

        _mockMapper.Verify(m => m.Map<ContactDto>(blockedContactEntities[0]), Times.Once);
        _mockMapper.Verify(m => m.Map<ContactDto>(blockedContactEntities[1]), Times.Once);
        _mockMapper.Verify(m => m.Map<ContactDto>(It.IsAny<ContactEntity>()), Times.Exactly(blockedContactEntities.Count));
    }

    [Fact]
    public async Task ConfirmFriendRequest_ConfirmsAndCreatesContactAndChat_ChatDoesNotExist()
    {
        // Arrange
        var userId = 1;
        var contactUserId = 2;
        var friendRequestEntity = new FriendRequestEntity { UserId = userId, ContactUserId = contactUserId };
        _mockContactRepository.Setup(r => r.GetFriendRequestAsync(userId, contactUserId)).ReturnsAsync(friendRequestEntity);
        _mockContactRepository.Setup(r => r.RemoveFriendRequestAsync(userId, contactUserId)).Returns(Task.CompletedTask);
        _mockContactRepository.Setup(r => r.AddContactAsync(userId, contactUserId, false)).Returns(Task.CompletedTask);
        _mockContactRepository.Setup(r => r.AddContactAsync(contactUserId, userId, false)).Returns(Task.CompletedTask);
        _mockChatService.Setup(s => s.GetPrivateChatIfExists(userId, contactUserId)).ReturnsAsync((ChatDto)null);
        _mockChatService.Setup(s => s.CreatePrivateChatAsync(userId, contactUserId)).Returns(Task.CompletedTask);

        // Act
        await _contactService.ConfirmFriendRequest(userId, contactUserId);

        // Assert
        _mockContactRepository.Verify(r => r.GetFriendRequestAsync(userId, contactUserId), Times.Once);
        _mockContactRepository.Verify(r => r.RemoveFriendRequestAsync(userId, contactUserId), Times.Once);
        _mockContactRepository.Verify(r => r.AddContactAsync(userId, contactUserId, false), Times.Once);
        _mockContactRepository.Verify(r => r.AddContactAsync(contactUserId, userId, false), Times.Once);
        _mockChatService.Verify(s => s.GetPrivateChatIfExists(userId, contactUserId), Times.Once);
        _mockChatService.Verify(s => s.CreatePrivateChatAsync(userId, contactUserId), Times.Once);
    }

    [Fact]
    public async Task ConfirmFriendRequest_ConfirmsAndCreatesContactAndChat_ChatExists()
    {
        // Arrange
        var userId = 1;
        var contactUserId = 2;
        var friendRequestEntity = new FriendRequestEntity { UserId = userId, ContactUserId = contactUserId };
        var existingChat = new ChatDto { Id = 10, IsGroup = false };
        _mockContactRepository.Setup(r => r.GetFriendRequestAsync(userId, contactUserId)).ReturnsAsync(friendRequestEntity);
        _mockContactRepository.Setup(r => r.RemoveFriendRequestAsync(userId, contactUserId)).Returns(Task.CompletedTask);
        _mockContactRepository.Setup(r => r.AddContactAsync(userId, contactUserId, false)).Returns(Task.CompletedTask);
        _mockContactRepository.Setup(r => r.AddContactAsync(contactUserId, userId, false)).Returns(Task.CompletedTask);
        _mockChatService.Setup(s => s.GetPrivateChatIfExists(userId, contactUserId)).ReturnsAsync(existingChat);

        // Act
        await _contactService.ConfirmFriendRequest(userId, contactUserId);

        // Assert
        _mockContactRepository.Verify(r => r.GetFriendRequestAsync(userId, contactUserId), Times.Once);
        _mockContactRepository.Verify(r => r.RemoveFriendRequestAsync(userId, contactUserId), Times.Once);
        _mockContactRepository.Verify(r => r.AddContactAsync(userId, contactUserId, false), Times.Once);
        _mockContactRepository.Verify(r => r.AddContactAsync(contactUserId, userId, false), Times.Once);
        _mockChatService.Verify(s => s.GetPrivateChatIfExists(userId, contactUserId), Times.Once);
        _mockChatService.Verify(s => s.CreatePrivateChatAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task ConfirmFriendRequest_RequestNotFound_ThrowsBusinessException()
    {
        // Arrange
        var userId = 1;
        var contactUserId = 2;
        _mockContactRepository.Setup(r => r.GetFriendRequestAsync(userId, contactUserId)).ReturnsAsync((FriendRequestEntity)null);

        // Act & Assert
        await Assert.ThrowsAsync<BusinessException>(() => _contactService.ConfirmFriendRequest(userId, contactUserId));
        _mockContactRepository.Verify(r => r.GetFriendRequestAsync(userId, contactUserId), Times.Once);
        _mockContactRepository.Verify(r => r.RemoveFriendRequestAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task RejectFriendRequest_RejectsRequest()
    {
        // Arrange
        var userId = 1;
        var contactUserId = 2;
        _mockContactRepository.Setup(r => r.RemoveFriendRequestAsync(userId, contactUserId)).Returns(Task.CompletedTask);

        // Act
        await _contactService.RejectFriendRequest(userId, contactUserId);

        // Assert
        _mockContactRepository.Verify(r => r.RemoveFriendRequestAsync(userId, contactUserId), Times.Once);
    }

    [Fact]
    public async Task SendFriendRequest_SendsRequest()
    {
        // Arrange
        var userId = 1;
        var contactUserId = 2;
        _mockContactRepository.Setup(r => r.SendFriendRequestAsync(userId, contactUserId)).Returns(Task.CompletedTask);

        // Act
        await _contactService.SendFriendRequest(userId, contactUserId);

        // Assert
        _mockContactRepository.Verify(r => r.SendFriendRequestAsync(userId, contactUserId), Times.Once);
    }

    [Fact]
    public async Task GetFriendRequests_ReturnsPendingRequests()
    {
        // Arrange
        var contactUserId = 1;
        var friendRequestEntities = new List<FriendRequestEntity> { new FriendRequestEntity(), new FriendRequestEntity() };
        _mockContactRepository.Setup(r => r.GetFriendRequestsAsync(contactUserId)).ReturnsAsync(friendRequestEntities);

        // Act
        var result = await _contactService.GetFriendRequests(contactUserId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(friendRequestEntities.Count, result.Count());
        _mockContactRepository.Verify(r => r.GetFriendRequestsAsync(contactUserId), Times.Once);
    }
}