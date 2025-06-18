using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PixChat.Application.DTOs;
using PixChat.Application.Interfaces.Services;

namespace PixChat.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly IMapper _mapper;
    private readonly ILogger<ChatController> _logger;

    public ChatController(IChatService chatService, IMapper mapper, ILogger<ChatController> logger)
    {
        _chatService = chatService;
        _mapper = mapper;
        _logger = logger;
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<IEnumerable<ChatDto>>> GetAllChats()
    {
        try
        {
            var chats = await _chatService.GetAllChatsAsync();
            return Ok(chats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching all chats.");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<ChatDto>> GetChatById(int id)
    {
        try
        {
            var chat = await _chatService.GetChatByIdAsync(id);
            if (chat == null)
            {
                return NotFound();
            }
            return Ok(chat);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error occurred while fetching chat with ID {id}");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("private/{userId}/{contactUserId}")]
    [Authorize]
    public async Task<ActionResult<ChatDto?>> GetPrivateChatIfExists(int userId, int contactUserId)
    {
        try
        {
            var chat = await _chatService.GetPrivateChatIfExists(userId, contactUserId);
            if (chat == null)
            {
                return NotFound();
            }
            return Ok(chat);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching private chat.");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateChat([FromBody] CreateChatDto dto)
    {
        try
        {
            await _chatService.CreateChatAsync(dto);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating chat.");
            return StatusCode(500, "Internal server error");
        }
    }

    [Authorize]
    [HttpPut]
    public async Task<IActionResult> UpdateChat([FromBody] UpdateChatDto dto)
    {
        try
        {
            await _chatService.UpdateChatAsync(dto);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating chat.");
            return StatusCode(500, "Internal server error");
        }
    }

    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteChat(int id)
    {
        try
        {
            await _chatService.DeleteChatAsync(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting chat.");
            return StatusCode(500, "Internal server error");
        }
    }

    [Authorize]
    [HttpPost("add-participant")]
    public async Task<IActionResult> AddParticipant([FromBody] AddParticipantDto dto)
    {
        try
        {
            await _chatService.AddParticipantAsync(dto);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while adding participant.");
            return StatusCode(500, "Internal server error");
        }
    }

    [Authorize]
    [HttpDelete("remove-participant/{participantId}")]
    public async Task<IActionResult> RemoveParticipant(int participantId)
    {
        try
        {
            await _chatService.RemoveParticipantAsync(participantId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while removing participant.");
            return StatusCode(500, "Internal server error");
        }
    }

    [Authorize]
    [HttpGet("participants/{chatId}")]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetParticipantsByChatId(int chatId)
    {
        try
        {
            var participants = await _chatService.GetParticipantsByChatIdAsync(chatId);
            return Ok(participants);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching participants.");
            return StatusCode(500, "Internal server error");
        }
    }
    
    [Authorize]
    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserChats(int userId)
    {
        var chats = await _chatService.GetUserChatsAsync(userId);
        return Ok(chats);
    }
}