using PixChat.Core.Entities;

namespace PixChat.Core.Interfaces.Repositories;

public interface IOneTimeMessageRepository
{
    Task<OneTimeMessage?> GetByIdAsync(string id);
    
    Task<IEnumerable<OneTimeMessage>> GetByReceiverIdAsync(string receiverId);
    
    Task<string> AddAsync(OneTimeMessage message);
    
    Task UpdateAsync(OneTimeMessage message);
    
    Task DeleteAsync(string id);
    Task MarkOneTimeMessageAsReceivedAsync(string messageId);
}