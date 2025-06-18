using PixChat.Application.DTOs;
using PixChat.Core.Entities;

namespace PixChat.Application.Interfaces.Services;

public interface IOfflineMessageService
{
    Task SaveMessageAsync(OfflineMessageDto message);
    Task SaveMessageAsync(OfflineMessageFileDto fileMessage);

    Task<IEnumerable<OfflineMessageDto>> GetPendingMessagesAsync(string receiverId);
    Task<IEnumerable<OfflineMessageFileDto>> GetPendingFileMessagesAsync(string receiverId);
    Task DeleteMessageAsync(string messageId);
    Task MarkMessageAsReceivedAsync(string messageId);
}