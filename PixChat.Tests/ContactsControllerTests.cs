using Moq;
using Microsoft.AspNetCore.Mvc;
using PixChat.API.Controllers;
using PixChat.Application.DTOs;
using PixChat.Application.Interfaces.Services;
using PixChat.Application.Requests;

namespace PixChat.Tests;

public class ContactsControllerTests
{
    private readonly Mock<IContactService> _mockContactService;
    private readonly Mock<AutoMapper.IMapper> _mockMapper;
    private readonly ContactsController _contactsController;

    public ContactsControllerTests()
    {
        _mockContactService = new Mock<IContactService>();
        _mockMapper = new Mock<AutoMapper.IMapper>();
        _contactsController = new ContactsController(_mockContactService.Object, _mockMapper.Object);
    }

    [Fact]
    public async Task GetContact_ContactExists_ReturnsContact()
    {
        // Arrange
        var userId = 1;
        var contactUserId = 2;
        var contactDto = new ContactDto { UserId = userId, ContactUserId = contactUserId };
        _mockContactService.Setup(s => s.GetContact(userId, contactUserId)).ReturnsAsync(contactDto);

        // Act
        var result = await _contactsController.GetContact(userId, contactUserId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedContact = Assert.IsType<ContactDto>(okResult.Value);
        Assert.Equal(userId, returnedContact.UserId);
        Assert.Equal(contactUserId, returnedContact.ContactUserId);
        _mockContactService.Verify(s => s.GetContact(userId, contactUserId), Times.Once);
    }

    [Fact]
    public async Task GetContact_ContactDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var userId = 1;
        var contactUserId = 99;
        _mockContactService.Setup(s => s.GetContact(userId, contactUserId)).ReturnsAsync((ContactDto)null);

        // Act
        var result = await _contactsController.GetContact(userId, contactUserId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
        _mockContactService.Verify(s => s.GetContact(userId, contactUserId), Times.Once);
    }

    [Fact]
    public async Task AddContact_ValidRequest_ReturnsOk()
    {
        // Arrange
        var request = new AddContactRequest { UserId = 1, ContactUserId = 2, IsBlocked = false };
        _mockContactService.Setup(s => s.AddContact(request.UserId, request.ContactUserId, request.IsBlocked)).Returns(Task.CompletedTask);

        // Act
        var result = await _contactsController.AddContact(request);

        // Assert
        Assert.IsType<OkResult>(result);
        _mockContactService.Verify(s => s.AddContact(request.UserId, request.ContactUserId, request.IsBlocked), Times.Once);
    }

    [Fact]
    public async Task RemoveContact_ContactExists_ReturnsOk()
    {
        // Arrange
        var userId = 1;
        var contactUserId = 2;
        _mockContactService.Setup(s => s.RemoveContact(userId, contactUserId)).Returns(Task.CompletedTask);

        // Act
        var result = await _contactsController.RemoveContact(userId, contactUserId);

        // Assert
        Assert.IsType<OkResult>(result);
        _mockContactService.Verify(s => s.RemoveContact(userId, contactUserId), Times.Once);
    }

    [Fact]
    public async Task RemoveContact_ContactDoesNotExist_ReturnsOk()
    {
        // Arrange
        var userId = 1;
        var contactUserId = 99;
        _mockContactService.Setup(s => s.RemoveContact(userId, contactUserId)).Returns(Task.CompletedTask);

        // Act
        var result = await _contactsController.RemoveContact(userId, contactUserId);

        // Assert
        Assert.IsType<OkResult>(result);
        _mockContactService.Verify(s => s.RemoveContact(userId, contactUserId), Times.Once);
    }


    [Fact]
    public async Task GetAllContacts_ReturnsAllContacts()
    {
        // Arrange
        var userId = 1;
        var contacts = new List<ContactDto> { new ContactDto { Id = 1, UserId = userId, ContactUserId = 2 } };
        _mockContactService.Setup(s => s.GetAllContacts(userId)).ReturnsAsync(contacts);

        // Act
        var result = await _contactsController.GetAllContacts(userId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedContacts = Assert.IsAssignableFrom<IEnumerable<ContactDto>>(okResult.Value);
        Assert.Single(returnedContacts);
        _mockContactService.Verify(s => s.GetAllContacts(userId), Times.Once);
    }

    [Fact]
    public async Task UpdateBlockStatus_ValidRequest_ReturnsOk()
    {
        // Arrange
        var request = new UpdateBlockStatusRequest { UserId = 1, ContactUserId = 2, IsBlocked = true };
        _mockContactService.Setup(s => s.UpdateContactBlockStatus(request.UserId, request.ContactUserId, request.IsBlocked)).Returns(Task.CompletedTask);

        // Act
        var result = await _contactsController.UpdateBlockStatus(request);

        // Assert
        Assert.IsType<OkResult>(result);
        _mockContactService.Verify(s => s.UpdateContactBlockStatus(request.UserId, request.ContactUserId, request.IsBlocked), Times.Once);
    }
}