using PixChat.Core.Entities;

namespace PixChat.Core.Interfaces.Repositories;

public interface IOfflineMessageRepository
{
    Task SaveMessageAsync(OfflineMessageEntity message);
    Task SaveMessageAsync(OfflineMessageFileEntity fileMessage);

    Task<IEnumerable<OfflineMessageEntity>> GetPendingMessagesAsync(string receiverId);
    Task<IEnumerable<OfflineMessageFileEntity>> GetPendingFileMessagesAsync(string receiverId);
    Task DeleteMessageAsync(string messageId);
    Task MarkMessageAsReceivedAsync(string messageId);
}