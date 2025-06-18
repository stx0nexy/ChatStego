using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PixChat.Core.Entities;
using PixChat.Core.Interfaces;
using PixChat.Core.Interfaces.Repositories;
using PixChat.Infrastructure.Database;
using PixChat.Infrastructure.ExternalServices;

namespace PixChat.Infrastructure.Repositories;

public class ContactRepository : BaseDataService, IContactRepository
{
    public ContactRepository(
        IDbContextWrapper<ApplicationDbContext> dbContextWrapper,
        ILogger<ContactRepository> logger) : base(dbContextWrapper, logger)
    {
    }

    public async Task<ContactEntity?> GetContactAsync(int userId, int contactUserId)
    {
        return await ExecuteSafeAsync(async () =>
        {
            return (await Context.Contacts
                .FirstOrDefaultAsync(c => c.UserId == userId && c.ContactUserId == contactUserId)); 
        });
    }

    public async Task<bool> IsContactBlockedAsync(int userId, int contactUserId)
    {
        return await ExecuteSafeAsync(async () =>
        {
            var contact = await Context.Contacts
                .FirstOrDefaultAsync(c => c.UserId == userId && c.ContactUserId == contactUserId);
            
            return contact?.IsBlockedByUser ?? false;
        });
    }

    public async Task AddContactAsync(int userId, int contactUserId, bool isBlocked)
    {
        await ExecuteSafeAsync(async () =>
        {
            var contactEntity = new ContactEntity
            {
                UserId = userId,
                ContactUserId = contactUserId,
                IsBlockedByUser = isBlocked,
                IsBlockedByContact = isBlocked
            };

            await Context.Contacts.AddAsync(contactEntity);
            await Context.SaveChangesAsync();    
        });
        
    }

    public async Task RemoveContactAsync(int userId, int contactUserId)
    {
        await ExecuteSafeAsync(async () =>
        {
            var contact1 = await Context.Contacts
                .FirstOrDefaultAsync(c => c.UserId == userId && c.ContactUserId == contactUserId);

            if (contact1 != null)
            {
                Context.Contacts.Remove(contact1);
                _logger.LogInformation("Contact removed: User {UserId} removed Contact {ContactUserId}.", userId, contactUserId);
            }
            else
            {
                _logger.LogWarning("Contact not found for removal: User {UserId} trying to remove Contact {ContactUserId}.", userId, contactUserId);
            }

            var contact2 = await Context.Contacts
                .FirstOrDefaultAsync(c => c.UserId == contactUserId && c.ContactUserId == userId);

            if (contact2 != null)
            {
                Context.Contacts.Remove(contact2);
                _logger.LogInformation("Reciprocal contact removed: User {ContactUserId} removed Contact {UserId}.", contactUserId, userId);
            }
            else
            {
                _logger.LogWarning("Reciprocal contact not found for removal: User {ContactUserId} trying to remove Contact {UserId}.", contactUserId, userId);
            }

            await Context.SaveChangesAsync();
        });
    }

    public async Task<IEnumerable<ContactEntity>> GetAllContactsAsync(int userId)
    {
        return await ExecuteSafeAsync(async () =>
        {
            return await Context.Contacts
                .Where(c => c.UserId == userId)
                .ToListAsync();
        });
    }

    public async Task UpdateContactBlockStatusAsync(int userId, int contactUserId, bool isBlocked)
    {
        await ExecuteSafeAsync(async () =>
        {
            var contactForUser = await Context.Contacts
                .FirstOrDefaultAsync(c => c.UserId == userId && c.ContactUserId == contactUserId);

            var contactForContactUser = await Context.Contacts
                .FirstOrDefaultAsync(c => c.UserId == contactUserId && c.ContactUserId == userId);

            if (contactForUser != null)
            {
                contactForUser.IsBlockedByUser = isBlocked;
            }
            else
            {
                _logger.LogWarning("Contact entry for user {UserId} and contact {ContactUserId} not found during block status update.", userId, contactUserId);
            }

            if (contactForContactUser != null)
            {
                contactForContactUser.IsBlockedByContact = isBlocked;
            }
            else
            {
                _logger.LogWarning("Contact entry for user {ContactUserId} and contact {UserId} not found during block status update.", contactUserId, userId);
            }

            if (contactForUser != null || contactForContactUser != null)
            {
                await Context.SaveChangesAsync();
            }
            else
            {
                _logger.LogInformation("No contact entries found for update of block status between {UserId} and {ContactUserId}.", userId, contactUserId);
            }
        });
    }

    public async Task<IEnumerable<ContactEntity>> GetBlockedContactsAsync(int userId)
    {
        return await ExecuteSafeAsync(async () =>
        {
            return await Context.Contacts
                .Where(c => c.UserId == userId && c.IsBlockedByUser)
                .ToListAsync();    
        });
        
    }

    public async Task SendFriendRequestAsync(int userId, int contactUserId)
    {
        await ExecuteSafeAsync(async () =>
        {
            var friendRequestEntity = new FriendRequestEntity()
            {
                UserId = userId,
                ContactUserId = contactUserId,
                Status = false
            };

            await Context.FriendRequests.AddAsync(friendRequestEntity);
            await Context.SaveChangesAsync();
        });
    }

    public async Task<FriendRequestEntity> GetFriendRequestAsync(int userId, int contactUserId)
    {
        return await ExecuteSafeAsync(async () =>
        {
            return (await Context.FriendRequests
                .FirstOrDefaultAsync(c => c.UserId == userId && c.ContactUserId == contactUserId))!;    
        });
        
    }

    public async Task<IEnumerable<FriendRequestEntity>> GetFriendRequestsAsync(int contactUserId)
    {
        return await ExecuteSafeAsync(async () =>
        {
            return (await Context.FriendRequests
                .Where(c => c.ContactUserId == contactUserId).ToListAsync()); 
        });
    }
    
    public async Task RemoveFriendRequestAsync(int userId, int contactUserId)
    {
        await ExecuteSafeAsync(async () =>
        {
            var friendRequest = await Context.FriendRequests
                .FirstOrDefaultAsync(fr => fr.UserId == userId && fr.ContactUserId == contactUserId);

            if (friendRequest != null)
            {
                Context.FriendRequests.Remove(friendRequest);
                await Context.SaveChangesAsync();
            }
            else
            {
                _logger.LogWarning("Friend request from user {UserId} to contact {ContactUserId} not found for removal.",
                    userId, contactUserId);
            }
        });
    }
    
}