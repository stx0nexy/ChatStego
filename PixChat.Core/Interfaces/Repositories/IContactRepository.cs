using PixChat.Core.Entities;

namespace PixChat.Core.Interfaces.Repositories;

public interface IContactRepository
{
    Task<ContactEntity?> GetContactAsync(int userId, int contactUserId);
    
    Task<bool> IsContactBlockedAsync(int userId, int contactUserId);
    
    Task AddContactAsync(int userId, int contactUserId, bool isBlocked);
    
    Task RemoveContactAsync(int userId, int contactUserId);
    
    Task<IEnumerable<ContactEntity>> GetAllContactsAsync(int userId);
    
    Task UpdateContactBlockStatusAsync(int userId, int contactUserId, bool isBlocked);
    
    Task<IEnumerable<ContactEntity>> GetBlockedContactsAsync(int userId);

    Task SendFriendRequestAsync(int userId, int contactUserId);

    Task<FriendRequestEntity> GetFriendRequestAsync(int userId, int contactUserId);

    Task<IEnumerable<FriendRequestEntity>> GetFriendRequestsAsync(int contactUserId);

    Task RemoveFriendRequestAsync(int userId, int contactUserId);
}