using AutoMapper;
using Microsoft.Extensions.Logging;
using PixChat.Application.DTOs;
using PixChat.Application.Interfaces.Services;
using PixChat.Core.Entities;
using PixChat.Core.Exceptions;
using PixChat.Core.Interfaces.Repositories;

namespace PixChat.Application.Services;

public class ContactService : IContactService
{
    private readonly IContactRepository _contactRepository;
    private readonly IChatService _chatService;
    private readonly IMapper _mapper;
    private readonly ILogger<ContactService> _logger;

    public ContactService(
        ILogger<ContactService> logger,
        IContactRepository contactRepository,
        IChatService chatService,
        IMapper mapper
    )
    {
        _logger = logger;
        _contactRepository = contactRepository;
        _chatService = chatService;
        _mapper = mapper;
    }

    public async Task AddContact(int userId, int contactUserId, bool isBlocked)
    {
        try
        {
            await _contactRepository.AddContactAsync(userId, contactUserId, isBlocked);

            var chat = await _chatService.GetPrivateChatIfExists(userId, contactUserId);
            if (chat == null)
            {
                await _chatService.CreatePrivateChatAsync(userId, contactUserId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while adding contact {ContactUserId} for user {UserId}.",
                contactUserId, userId);
            throw;
        }
    }

    public async Task<ContactDto?> GetContact(int userId, int contactUserId)
    {
        try
        {
            var result = await _contactRepository.GetContactAsync(userId, contactUserId);
            return _mapper.Map<ContactDto?>(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching contact {ContactUserId} for user {UserId}.",
                contactUserId, userId);
            throw;
        }
    }

    public async Task<bool> IsContactBlocked(int userId, int contactUserId)
    {
        try
        {
            var result = await _contactRepository.IsContactBlockedAsync(userId, contactUserId);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while checking if contact {ContactUserId} is blocked by user {UserId}.",
                contactUserId, userId);
            throw;
        }
    }

    public async Task RemoveContact(int userId, int contactUserId)
    {
        try
        {
            await _contactRepository.RemoveContactAsync(userId, contactUserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while removing contact {ContactUserId} for user {UserId}.",
                contactUserId, userId);
            throw;
        }
    }

    public async Task<IEnumerable<ContactDto>> GetAllContacts(int userId)
    {
        try
        {
            var result = await _contactRepository.GetAllContactsAsync(userId);
            return result.Select(s => _mapper.Map<ContactDto>(s)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching all contacts for user {UserId}.", userId);
            throw;
        }
    }

    public async Task UpdateContactBlockStatus(int userId, int contactUserId, bool isBlocked)
    {
        try
        {
            await _contactRepository.UpdateContactBlockStatusAsync(userId, contactUserId, isBlocked);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating block status for contact {ContactUserId} by user {UserId}.",
                contactUserId, userId);
            throw;
        }
    }

    public async Task<IEnumerable<ContactDto>> GetBlockedContacts(int userId)
    {
        try
        {
            var result = await _contactRepository.GetBlockedContactsAsync(userId);
            return result.Select(s => _mapper.Map<ContactDto>(s)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching blocked contacts for user {UserId}.", userId);
            throw;
        }
    }

    public async Task ConfirmFriendRequest(int userId, int contactUserId)
    {
        try
        {
            var friendRequest = await _contactRepository.GetFriendRequestAsync(userId, contactUserId);
            if (friendRequest == null)
            {
                throw new BusinessException("Friend request not found");
            }

            await _contactRepository.RemoveFriendRequestAsync(userId, contactUserId);
            await _contactRepository.AddContactAsync(userId, contactUserId, false);
            await _contactRepository.AddContactAsync(contactUserId, userId, false);
            var chat = await _chatService.GetPrivateChatIfExists(userId, contactUserId);
            if (chat == null)
            {
                await _chatService.CreatePrivateChatAsync(userId, contactUserId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while confirming friend request between {UserId} and {ContactUserId}.",
                userId, contactUserId);
            throw;
        }
    }

    public async Task RejectFriendRequest(int userId, int contactUserId)
    {
        try
        {
            await _contactRepository.RemoveFriendRequestAsync(userId, contactUserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while rejecting friend request between {UserId} and {ContactUserId}.",
                userId, contactUserId);
            throw;
        }
    }

    public async Task SendFriendRequest(int userId, int contactUserId)
    {
        try
        {
            await _contactRepository.SendFriendRequestAsync(userId, contactUserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while sending friend request from {UserId} to {ContactUserId}.",
                userId, contactUserId);
            throw;
        }
    }

    public async Task<IEnumerable<FriendRequestEntity>> GetFriendRequests(int contactUserId)
    {
        try
        {
            var result = await _contactRepository.GetFriendRequestsAsync(contactUserId);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching friend requests for user {ContactUserId}.", contactUserId);
            throw;
        }
    }
}
