using PixChat.Application.DTOs;

namespace PixChat.Application.Interfaces.Services;

public interface IChatService
{
    Task<IEnumerable<ChatDto>> GetAllChatsAsync();
    Task<ChatDto?> GetChatByIdAsync(int id);
    Task CreateChatAsync(CreateChatDto dto);
    Task UpdateChatAsync(UpdateChatDto dto);
    Task DeleteChatAsync(int id);
    Task AddParticipantAsync(AddParticipantDto dto);
    Task RemoveParticipantAsync(int participantId);
    Task<ChatDto?> GetPrivateChatIfExists(int userId, int contactUserId);
    Task CreatePrivateChatAsync(int userId, int contactUserId);
    Task<IEnumerable<UserDto>> GetParticipantsByChatIdAsync(int chatId);
    Task<List<ChatDto>> GetUserChatsAsync(int userId);
}