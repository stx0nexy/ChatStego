using AutoMapper;
using Microsoft.Extensions.Logging;
using PixChat.Application.DTOs;
using PixChat.Application.Interfaces.Services;
using PixChat.Core.Entities;
using PixChat.Core.Interfaces.Repositories;

namespace PixChat.Application.Services;

public class ParticipantService: IParticipantService
{
    private readonly IChatParticipantRepository _chatParticipantRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<ParticipantService> _logger;

    public ParticipantService(
        ILogger<ParticipantService> logger,
        IChatParticipantRepository chatParticipantRepository,
        IMapper mapper
    )
    {
        _logger = logger;
        _chatParticipantRepository = chatParticipantRepository;
        _mapper = mapper;
    }
    
    public async Task<IEnumerable<ChatParticipantDto>> GetParticipantsByChatIdAsync(int chatId)
    {
        try
        {
            var participants = await _chatParticipantRepository.GetParticipantsByChatIdAsync(chatId);
            return _mapper.Map<IEnumerable<ChatParticipantDto>>(participants);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting participants by chat ID: {ChatId}.", chatId);
            throw;
        }
    }
    public async Task<ChatParticipantDto?> GetParticipantByChatAndUserAsync(int chatId, int userId)
    {
        try
        {
            var participant = await _chatParticipantRepository.GetParticipantByChatAndUserAsync(chatId, userId);
            return _mapper.Map<ChatParticipantDto?>(participant);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting participant by chat ID: {ChatId}.", chatId);
            throw;
        }
    }

    public async Task AddParticipantAsync(AddParticipantDto dto)
    {
        try
        {
            var participant = _mapper.Map<ChatParticipantEntity>(dto);
            participant.JoinedAt = DateTime.UtcNow;

            await _chatParticipantRepository.AddAsync(participant);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while adding participant to chat. DTO: {@Dto}", dto);
            throw;
        }
    }

    public async Task RemoveParticipantAsync(int participantId)
    {
        try
        {
            await _chatParticipantRepository.DeleteAsync(participantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while removing participant with ID: {ParticipantId}.", participantId);
            throw;
        }
    }
    
    public async Task RemoveByChatAndUserAsync(int chatId, int userId)
    {
        try
        {
            await _chatParticipantRepository.DeleteByChatAndUserAsync(chatId, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while removing participant with userID: {userId}.", userId);
            throw;
        }
    }

    public async Task UpdateParticipantAsync(UpdateParticipantDto dto)
    {
        try
        {
            var participant = await _chatParticipantRepository.GetByIdAsync(dto.Id);
            if (participant == null)
            {
                _logger.LogWarning("Participant with ID {ParticipantId} not found for update.", dto.Id);
                throw new KeyNotFoundException($"Participant with ID {dto.Id} not found.");
            }

            participant.IsAdmin = dto.IsAdmin;
            await _chatParticipantRepository.UpdateAsync(participant);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating participant with ID: {ParticipantId}. DTO: {@Dto}", dto.Id, dto);
            throw;
        }
    }
}