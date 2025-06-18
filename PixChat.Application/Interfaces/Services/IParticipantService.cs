using PixChat.Application.DTOs;

namespace PixChat.Application.Interfaces.Services;

public interface IParticipantService
{
    Task<IEnumerable<ChatParticipantDto>> GetParticipantsByChatIdAsync(int chatId);
    Task<ChatParticipantDto?> GetParticipantByChatAndUserAsync(int chatId, int userId);
    Task AddParticipantAsync(AddParticipantDto dto);
    Task RemoveParticipantAsync(int participantId);
    Task RemoveByChatAndUserAsync(int chatId, int userId);
    Task UpdateParticipantAsync(UpdateParticipantDto dto);
}