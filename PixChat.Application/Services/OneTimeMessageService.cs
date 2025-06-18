using AutoMapper;
using Microsoft.Extensions.Logging;
using PixChat.Application.DTOs;
using PixChat.Application.Interfaces.Services;
using PixChat.Core.Entities;
using PixChat.Core.Interfaces.Repositories;

namespace PixChat.Application.Services;

public class OneTimeMessageService : IOneTimeMessageService
{
    private readonly IOneTimeMessageRepository _oneTimeMessageRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<OneTimeMessageService> _logger;

    public OneTimeMessageService(
        ILogger<OneTimeMessageService> logger,
        IOneTimeMessageRepository oneTimeMessageRepository,
        IMapper mapper
    )
    {
        _logger = logger;
        _oneTimeMessageRepository = oneTimeMessageRepository;
        _mapper = mapper;
    }

    public async Task<OneTimeMessageDto?> GetMessageByIdAsync(string id)
    {
        try
        {
            var result = await _oneTimeMessageRepository.GetByIdAsync(id);
            return _mapper.Map<OneTimeMessageDto?>(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching one-time message with ID: {MessageId}.", id);
            throw;
        }
    }

    public async Task<IEnumerable<OneTimeMessageDto>> GetMessagesByReceiverIdAsync(string receiverId)
    {
        try
        {
            var result = await _oneTimeMessageRepository.GetByReceiverIdAsync(receiverId);
            return result.Select(s => _mapper.Map<OneTimeMessageDto>(s)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching one-time messages for receiver: {ReceiverId}.", receiverId);
            throw;
        }
    }

    public async Task<string> SendMessageAsync( string? senderId, string receiverId,
        int chatId, byte[]? stegoImage, DateTime createdAt, bool received)
    {
        try
        {
            var messageAdd = new OneTimeMessage()
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                ChatId = chatId,
                StegoImage = stegoImage,
                CreatedAt = createdAt,
                Received = received
            };
            return await _oneTimeMessageRepository.AddAsync(messageAdd);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while sending one-time message for receiver: {ReceiverId}.", receiverId);
            throw;
        }
    }

    public async Task DeleteMessageAsync(string id)
    {
        try
        {
            await _oneTimeMessageRepository.DeleteAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting one-time message with ID: {MessageId}.", id);
            throw;
        }
    }
    
    public async Task MarkOneTimeMessageAsReceivedAsync(string messageId)
    {
        try
        {
            await _oneTimeMessageRepository.MarkOneTimeMessageAsReceivedAsync(messageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while marking one-time message with ID: {MessageId} as received.", messageId);
            throw;
        }
    }
}