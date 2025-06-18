using PixChat.Core.Entities;

namespace PixChat.Core.Interfaces.Repositories;

public interface IChatRepository
{
    Task<IEnumerable<ChatEntity>> GetAllAsync();
    Task<ChatEntity?> GetByIdAsync(int id);
    Task<int> AddAsync(ChatEntity entity);
    Task UpdateAsync(ChatEntity entity);
    Task DeleteAsync(int id);
    Task<ChatEntity?> GetPrivateChatIfExistsAsync(int userId, int contactUserId);
    Task<IEnumerable<UserEntity>> GetParticipantsByChatIdAsync(int chatId);
    Task<List<ChatEntity>> GetChatsByUserIdAsync(int userId);
}