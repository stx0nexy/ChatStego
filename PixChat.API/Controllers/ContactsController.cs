using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PixChat.Application.Interfaces.Services;
using PixChat.Application.Requests;

namespace PixChat.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContactsController : ControllerBase
{
    private readonly IContactService _contactService;
    private readonly IMapper _mapper;

    public ContactsController(IContactService contactService, IMapper mapper)
    {
        _contactService = contactService;
        _mapper = mapper;
    }

    [Authorize]
    [HttpGet("{userId}/contact/{contactUserId}")]
    public async Task<IActionResult> GetContact(int userId, int contactUserId)
    {
        var contact = await _contactService.GetContact(userId, contactUserId);
        if (contact == null)
            return NotFound();
            
        return Ok(contact);
    }

    [Authorize]
    [HttpPost("{userId}/contact")]
    public async Task<IActionResult> AddContact([FromBody] AddContactRequest request)
    {
        await _contactService.AddContact(request.UserId, request.ContactUserId, request.IsBlocked);
        return Ok();
    }

    [Authorize]
    [HttpDelete("{userId}/contact/{contactUserId}")]
    public async Task<IActionResult> RemoveContact(int userId, int contactUserId)
    {
        await _contactService.RemoveContact(userId, contactUserId);
        return Ok();
    }

    [Authorize]
    [HttpGet("{userId}/contacts")]
    public async Task<IActionResult> GetAllContacts(int userId)
    {
        var contacts = await _contactService.GetAllContacts(userId);
        return Ok(contacts);
    }

    [Authorize]
    [HttpPut("{userId}/contact/{contactUserId}/block")]
    public async Task<IActionResult> UpdateBlockStatus([FromBody] UpdateBlockStatusRequest request)
    {
        await _contactService.UpdateContactBlockStatus(request.UserId, request.ContactUserId, request.IsBlocked);
        return Ok();
    }
}