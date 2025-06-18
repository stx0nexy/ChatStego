using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PixChat.Application.DTOs;
using PixChat.Application.Interfaces.Services;

namespace PixChat.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ParticipantController : ControllerBase
{
    private readonly IParticipantService _participantService;
    private readonly ILogger<ParticipantController> _logger;

    public ParticipantController(IParticipantService participantService, ILogger<ParticipantController> logger)
    {
        _participantService = participantService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetParticipants(int chatId)
    {
        try
        {
            var participants = await _participantService.GetParticipantsByChatIdAsync(chatId);
            if (participants == null || !((List<ChatParticipantDto>)participants).Any())
            {
                return NotFound($"No participants found for chat ID {chatId} or chat does not exist.");
            }
            return Ok(participants);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting participants for chat {ChatId}.", chatId);
            return StatusCode(500, "Internal server error.");
        }
    }
    
    [HttpGet("chat/{chatId}/user/{userId}")]
    public async Task<ActionResult<ChatParticipantDto>> GetChatParticipant(int chatId, int userId)
    {
        try
        {
            var participant = await _participantService.GetParticipantByChatAndUserAsync(chatId, userId);
            if (participant == null)
            {
                return NotFound($"Participant not found for chat ID {chatId} and user ID {userId}.");
            }
            return Ok(participant);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting participant for chat {ChatId} and user {UserId}.", chatId, userId);
            return StatusCode(500, "Internal server error.");
        }
    }
    
    [HttpPost]
    public async Task<IActionResult> AddParticipant(int chatId, [FromBody] AddParticipantDto dto)
    {
        if (chatId != dto.ChatId)
        {
            return BadRequest("Chat ID in URL does not match chat ID in request body.");
        }
        try
        {
            await _participantService.AddParticipantAsync(dto);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding participant to chat {ChatId}.", chatId);
            return StatusCode(500, "Internal server error.");
        }
    }
    
    [HttpPut("{participantId}")]
    public async Task<IActionResult> UpdateParticipant(int chatId, int participantId, [FromBody] UpdateParticipantDto dto)
    {
        if (participantId != dto.Id || chatId != dto.ChatId)
        {
            return BadRequest("Participant ID or Chat ID in URL does not match IDs in request body.");
        }

        try
        {
            await _participantService.UpdateParticipantAsync(dto);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Participant {ParticipantId} not found for chat {ChatId} during update.", participantId, chatId);
            return NotFound($"Participant with ID {participantId} not found in chat {chatId}.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating participant {ParticipantId} for chat {ChatId}.", participantId, chatId);
            return StatusCode(500, "Internal server error.");
        }
    }
    
    [HttpDelete("{participantId}")]
    public async Task<IActionResult> RemoveParticipantById(int chatId, int participantId)
    {
        try
        {
            await _participantService.RemoveParticipantAsync(participantId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing participant {ParticipantId} from chat {ChatId}.", participantId, chatId);
            return StatusCode(500, "Internal server error.");
        }
    }
    
    [HttpDelete("user/{userId}")]
    public async Task<IActionResult> RemoveParticipantByChatAndUser(int chatId, int userId)
    {
        try
        {
            await _participantService.RemoveByChatAndUserAsync(chatId, userId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing user {UserId} from chat {ChatId}.", userId, chatId);
            return StatusCode(500, "Internal server error.");
        }
    }
}