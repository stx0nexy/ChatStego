using PixChat.Core.Entities;

namespace PixChat.Core.Interfaces.Repositories;

public interface IChatParticipantRepository
{
    Task<IEnumerable<ChatParticipantEntity>> GetParticipantsByChatIdAsync(int chatId);
    Task<ChatParticipantEntity?> GetByIdAsync(int participantId);
    Task<ChatParticipantEntity?> GetParticipantByChatAndUserAsync(int chatId, int userId);
    Task AddAsync(ChatParticipantEntity participant);
    Task DeleteAsync(int participantId);
    Task DeleteByChatAndUserAsync(int chatId, int userId);
    Task UpdateAsync(ChatParticipantEntity participant);
}