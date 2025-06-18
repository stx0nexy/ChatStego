using PixChat.Application.DTOs;

namespace PixChat.Application.Interfaces.Services;

public interface IOneTimeMessageService
{
    Task<OneTimeMessageDto?> GetMessageByIdAsync(string id);
    
    Task<IEnumerable<OneTimeMessageDto>> GetMessagesByReceiverIdAsync(string receiverId);
    
    Task<string> SendMessageAsync( string? senderId, string receiverId,
        int chatId, byte[] stegoImage,  DateTime createdAt, bool received);
    
    Task DeleteMessageAsync(string id);
    Task MarkOneTimeMessageAsReceivedAsync(string messageId);
}