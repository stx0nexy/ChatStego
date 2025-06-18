using PixChat.Application.DTOs;
using PixChat.Core.Entities;

namespace PixChat.Application.Interfaces.Services;

public interface IContactService
{
    Task<ContactDto> GetContact(int userId, int contactUserId);
    
    Task<bool> IsContactBlocked(int userId, int contactUserId);
    
    Task AddContact(int userId, int contactUserId, bool isBlocked);
    
    Task RemoveContact(int userId, int contactUserId);
    
    Task<IEnumerable<ContactDto>> GetAllContacts(int userId);
    
    Task UpdateContactBlockStatus(int userId, int contactUserId, bool isBlocked);
    
    Task<IEnumerable<ContactDto>> GetBlockedContacts(int userId);

    Task ConfirmFriendRequest(int userId, int contactUserId);

    Task RejectFriendRequest(int userId, int contactUserId);

    Task SendFriendRequest(int userId, int contactUserId);

    Task<IEnumerable<FriendRequestEntity>> GetFriendRequests(int contactUserId);
}