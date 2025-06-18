using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PixChat.Core.Entities;
using PixChat.Core.Interfaces;
using PixChat.Core.Interfaces.Repositories;
using PixChat.Infrastructure.Database;
using PixChat.Infrastructure.ExternalServices;

namespace PixChat.Infrastructure.Repositories;

public class OneTimeMessageRepository : BaseDataService, IOneTimeMessageRepository
{
    public OneTimeMessageRepository(
        IDbContextWrapper<ApplicationDbContext> dbContextWrapper,
        ILogger<OneTimeMessageRepository> logger) : base(dbContextWrapper, logger)
    {
    }

    public async Task<OneTimeMessage?> GetByIdAsync(string id)
    {
        return await ExecuteSafeAsync(async () =>
        {
            return await Context.OneTimeMessages
                .FirstOrDefaultAsync(m => m.Id == id);
        });
    }

    public async Task<IEnumerable<OneTimeMessage>> GetByReceiverIdAsync(string receiverId)
    {
        return await ExecuteSafeAsync(async () =>
        {
            return await Context.OneTimeMessages
                .Where(m => m.ReceiverId == receiverId)
                .ToListAsync();
        });
    }

    public async Task<string> AddAsync(OneTimeMessage message)
    {
        return await ExecuteSafeAsync(async () =>
        {
            await Context.OneTimeMessages.AddAsync(message);
            await Context.SaveChangesAsync();
            return message.Id;
        });
    }

    public async Task UpdateAsync(OneTimeMessage message)
    {
        await ExecuteSafeAsync(async () =>
        {
            Context.OneTimeMessages.Update(message);
            await Context.SaveChangesAsync();
        });
    }

    public async Task DeleteAsync(string id)
    {
        await ExecuteSafeAsync(async () =>
        {
            var message = await Context.OneTimeMessages.FindAsync(id);
            if (message != null)
            {
                Context.OneTimeMessages.Remove(message);
                await Context.SaveChangesAsync();
            }
            else
            {
                _logger.LogWarning("OneTimeMessage with ID {MessageId} not found for deletion.", id);
            }
        });
    }
    
    public async Task MarkOneTimeMessageAsReceivedAsync(string messageId)
    {
        await ExecuteSafeAsync(async () =>
        {
            var message = await Context.OneTimeMessages.FindAsync(messageId);
            if (message != null)
            {
                message.Received = true;
                await Context.SaveChangesAsync();
                _logger.LogInformation($"Marked OneTimeMessage with ID: {messageId} as received.");
            }
            else
            {
                _logger.LogWarning("OneTimeMessage with ID {MessageId} not found to mark as received.", messageId);
            }
        });
    }
}
