using AutoMapper;
using Microsoft.Extensions.Logging;
using PixChat.Application.DTOs;
using PixChat.Application.Interfaces.Services;
using PixChat.Core.Entities;
using PixChat.Core.Interfaces.Repositories;

namespace PixChat.Application.Services;

public class OfflineMessageService : IOfflineMessageService
{
    private readonly IOfflineMessageRepository _offlineMessageRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<OfflineMessageService> _logger;

    public OfflineMessageService(
        ILogger<OfflineMessageService> logger,
        IOfflineMessageRepository offlineMessageRepository,
        IMapper mapper
    )
    {
        _logger = logger;
        _offlineMessageRepository = offlineMessageRepository;
        _mapper = mapper;
    }

    public async Task SaveMessageAsync(OfflineMessageDto message)
    {
        try
        {
            var messageEntity = _mapper.Map<OfflineMessageEntity>(message);
            await _offlineMessageRepository.SaveMessageAsync(messageEntity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while saving offline message for receiver: {ReceiverId}.",
                message.ReceiverId);
            throw;
        }
    }
    
    public async Task SaveMessageAsync(OfflineMessageFileDto fileMessage)
    {
        try
        {
            var messageEntity = _mapper.Map<OfflineMessageFileEntity>(fileMessage);
            await _offlineMessageRepository.SaveMessageAsync(messageEntity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while saving offline file message for receiver: {ReceiverId}.",
                fileMessage.ReceiverId);
            throw;
        }
    }

    public async Task<IEnumerable<OfflineMessageDto>> GetPendingMessagesAsync(string receiverId)
    {
        try
        {
            var result = await _offlineMessageRepository.GetPendingMessagesAsync(receiverId);
            return result.Select(s => _mapper.Map<OfflineMessageDto>(s)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching pending messages for receiver: {ReceiverId}.", receiverId);
            throw;
        }
    }
    
    public async Task<IEnumerable<OfflineMessageFileDto>> GetPendingFileMessagesAsync(string receiverId)
    {
        try
        {
            var result = await _offlineMessageRepository.GetPendingFileMessagesAsync(receiverId);
            return result.Select(s => _mapper.Map<OfflineMessageFileDto>(s)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching pending file messages for receiver: {ReceiverId}.", receiverId);
            throw;
        }
    }

    public async Task DeleteMessageAsync(string messageId)
    {
        try
        {
            await _offlineMessageRepository.DeleteMessageAsync(messageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting offline message with ID: {MessageId}.", messageId);
            throw;
        }
    }

    public async Task MarkMessageAsReceivedAsync(string messageId)
    {
        try
        {
            await _offlineMessageRepository.MarkMessageAsReceivedAsync(messageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while marking offline message with ID: {MessageId} as received.", messageId);
            throw;
        }
    }
}