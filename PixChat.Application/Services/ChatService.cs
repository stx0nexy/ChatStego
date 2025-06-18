using AutoMapper;
using Microsoft.Extensions.Logging;
using PixChat.Application.DTOs;
using PixChat.Application.Interfaces.Services;
using PixChat.Core.Entities;
using PixChat.Core.Interfaces.Repositories;

namespace PixChat.Application.Services;

public class ChatService : IChatService
{
    private readonly IChatRepository _chatRepository;
    private readonly IMapper _mapper;
    private readonly IChatParticipantRepository _participantRepository;
    private readonly ILogger<ChatService> _logger;

    public ChatService(
        ILogger<ChatService> logger,
        IChatRepository chatRepository,
        IMapper mapper,
        IChatParticipantRepository participantRepository
    )
    {
        _logger = logger;
        _chatRepository = chatRepository;
        _mapper = mapper;
        _participantRepository = participantRepository;
    }
    
    public async Task<IEnumerable<ChatDto>> GetAllChatsAsync()
    {
        try
        {
            var chats = await _chatRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<ChatDto>>(chats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving all chats.");
            throw;
        }
    }

    public async Task<ChatDto?> GetChatByIdAsync(int id)
    {
        try
        {
            var chat = await _chatRepository.GetByIdAsync(id);
            return chat == null ? null : _mapper.Map<ChatDto>(chat);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving chat with ID {ChatId}.", id);
            throw;
        }
    }
    
    public async Task<ChatDto?> GetPrivateChatIfExists(int userId, int contactUserId)
    {
        try
        {
            var result = await _chatRepository.GetPrivateChatIfExistsAsync(userId, contactUserId);
            return _mapper.Map<ChatDto?>(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while checking for private chat between user {UserId} and contact {ContactUserId}.",
                userId, contactUserId);
            throw;
        }

    }

    public async Task CreatePrivateChatAsync(int userId, int contactUserId)
    {
        try
        {
            var chat = new ChatEntity
            {
                Name = $"{userId}-{contactUserId}",
                IsGroup = false,
                CreatorId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Participants = new List<ChatParticipantEntity>
                {
                    new ChatParticipantEntity { UserId = userId, IsAdmin = true, JoinedAt = DateTime.UtcNow },
                    new ChatParticipantEntity { UserId = contactUserId, IsAdmin = false, JoinedAt = DateTime.UtcNow }
                }
            };

            await _chatRepository.AddAsync(chat);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating private chat between user {UserId} and contact {ContactUserId}.",
                userId, contactUserId);
            throw;
        }
    }
    
    public async Task CreateChatAsync(CreateChatDto dto)
    {
        try
        {
            var chat = _mapper.Map<ChatEntity>(dto);
            chat.CreatedAt = DateTime.UtcNow;
            chat.UpdatedAt = DateTime.UtcNow;

            var result = await _chatRepository.AddAsync(chat);

            foreach (var participantId in dto.ParticipantIds)
            {
                await AddParticipantAsync(new AddParticipantDto
                {
                    ChatId = result,
                    UserId = participantId,
                    IsAdmin = participantId == dto.CreatorId
                });
            }

            await AddParticipantAsync(new AddParticipantDto
            {
                ChatId = result,
                UserId = dto.CreatorId,
                IsAdmin = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating chat from DTO {@ChatDto}.", dto);
            throw;
        }
    }

    public async Task UpdateChatAsync(UpdateChatDto dto)
    {
        try
        {
            var chat = await _chatRepository.GetByIdAsync(dto.Id);
            if (chat == null) throw new KeyNotFoundException($"Chat with ID {dto.Id} not found.");

            _mapper.Map(dto, chat);
            chat.UpdatedAt = DateTime.UtcNow;

            await _chatRepository.UpdateAsync(chat);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating chat with DTO {@ChatDto}.", dto);
            throw;
        }
    }

    public async Task DeleteChatAsync(int id)
    {
        try
        {
            await _chatRepository.DeleteAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting chat with ID {ChatId}.", id);
            throw;
        }
    }

    public async Task AddParticipantAsync(AddParticipantDto dto)
    {
        try
        {
            var participant = _mapper.Map<ChatParticipantEntity>(dto);
            participant.JoinedAt = DateTime.UtcNow;

            await _participantRepository.AddAsync(participant);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while adding participant from DTO {@ParticipantDto}.", dto);
            throw;
        }
    }

    public async Task RemoveParticipantAsync(int participantId)
    {
        try
        {
            await _participantRepository.DeleteAsync(participantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while removing participant with ID {ParticipantId}.", participantId);
            throw;
        }
    }
    
    public async Task<IEnumerable<UserDto>> GetParticipantsByChatIdAsync(int chatId)
    {
        try
        {
            var participants = await _chatRepository.GetParticipantsByChatIdAsync(chatId);

            if (!participants.Any())
            {
                _logger.LogWarning("No participants found for chat ID {ChatId}", chatId);
                return Enumerable.Empty<UserDto>();
            }

            var participantDtos = _mapper.Map<IEnumerable<UserDto>>(participants);

            return participantDtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving participants for chat ID {ChatId}", chatId);
            throw;
        }
    }
    
    public async Task<List<ChatDto>> GetUserChatsAsync(int userId)
    {
        try
        {
            var chatEntities = await _chatRepository.GetChatsByUserIdAsync(userId);
            return _mapper.Map<List<ChatDto>>(chatEntities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving chats for user {UserId}.", userId);
            throw;
        }
    }
}