using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PixChat.Core.Entities;
using PixChat.Core.Interfaces;
using PixChat.Core.Interfaces.Repositories;
using PixChat.Infrastructure.Database;
using PixChat.Infrastructure.ExternalServices;

namespace PixChat.Infrastructure.Repositories;

public class OfflineMessageRepository : BaseDataService, IOfflineMessageRepository
{
    public OfflineMessageRepository(
        IDbContextWrapper<ApplicationDbContext> dbContextWrapper,
        ILogger<OfflineMessageRepository> logger) : base(dbContextWrapper, logger)
    {
    }

     public async Task SaveMessageAsync(OfflineMessageEntity message)
    {
        await ExecuteSafeAsync(async () =>
        {
            _logger.LogInformation($"Saving offline message for receiver: {message.ReceiverId}");

            await Context.OfflineMessages.AddAsync(message);
            await Context.SaveChangesAsync();

            _logger.LogInformation($"Successfully saved offline message with ID: {message.Id}");
        });
    }

    public async Task SaveMessageAsync(OfflineMessageFileEntity fileMessage)
    {
        await ExecuteSafeAsync(async () =>
        {
            _logger.LogInformation($"Saving offline file message for receiver: {fileMessage.ReceiverId}");

            await Context.OfflineMessageFiles.AddAsync(fileMessage);
            await Context.SaveChangesAsync();

            _logger.LogInformation($"Successfully saved offline file message with ID: {fileMessage.Id}");
        });
    }

    public async Task<IEnumerable<OfflineMessageEntity>> GetPendingMessagesAsync(string receiverId)
    {
        return await ExecuteSafeAsync(async () =>
        {
            _logger.LogInformation($"Fetching pending messages for receiver: {receiverId}");

            var messages = await Context.OfflineMessages
                .Where(m => m.ReceiverId == receiverId)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();

            _logger.LogInformation($"Found {messages.Count} pending messages for receiver: {receiverId}");

            return messages;
        });
    }

    public async Task<IEnumerable<OfflineMessageFileEntity>> GetPendingFileMessagesAsync(string receiverId)
    {
        return await ExecuteSafeAsync(async () =>
        {
            _logger.LogInformation($"Fetching pending file messages for receiver: {receiverId}");

            var fileMessages = await Context.OfflineMessageFiles
                .Where(m => m.ReceiverId == receiverId)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();

            _logger.LogInformation($"Found {fileMessages.Count} pending file messages for receiver: {receiverId}");

            return fileMessages;
        });
    }

    public async Task DeleteMessageAsync(string messageId)
    {
        await ExecuteSafeAsync(async () =>
        {
            try
            {
                _logger.LogInformation($"Deleting offline message with ID: {messageId}");

                var deletedMessages = await Context.OfflineMessages
                    .Where(m => m.Id == messageId)
                    .ExecuteDeleteAsync();

                if (deletedMessages > 0)
                {
                    _logger.LogInformation($"Successfully deleted offline message with ID: {messageId}");
                    return;
                }

                var deletedFiles = await Context.OfflineMessageFiles
                    .Where(m => m.Id == messageId)
                    .ExecuteDeleteAsync();

                if (deletedFiles > 0)
                {
                    _logger.LogInformation($"Successfully deleted offline file message with ID: {messageId}");
                }
                else
                {
                    _logger.LogWarning($"Message with ID: {messageId} not found or already deleted in both tables");
                }
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, $"Concurrency conflict while deleting message with ID: {messageId}");

                Context.ChangeTracker.Clear();

                var existsInMessages = await Context.OfflineMessages.AnyAsync(m => m.Id == messageId);
                var existsInFiles = await Context.OfflineMessageFiles.AnyAsync(m => m.Id == messageId);

                if (!existsInMessages && !existsInFiles)
                {
                    _logger.LogInformation($"Message with ID: {messageId} was already deleted");
                    return;
                }

                try
                {
                    if (existsInMessages)
                    {
                        var message = await Context.OfflineMessages.FindAsync(messageId);
                        if (message != null)
                        {
                            Context.OfflineMessages.Remove(message);
                            await Context.SaveChangesAsync();
                            _logger.LogInformation($"Successfully deleted message with ID: {messageId} after resolving concurrency conflict");
                        }
                    }
                    else if (existsInFiles)
                    {
                        var fileMessage = await Context.OfflineMessageFiles.FindAsync(messageId);
                        if (fileMessage != null)
                        {
                            Context.OfflineMessageFiles.Remove(fileMessage);
                            await Context.SaveChangesAsync();
                            _logger.LogInformation($"Successfully deleted file message with ID: {messageId} after resolving concurrency conflict");
                        }
                    }
                }
                catch (Exception retryEx)
                {
                    _logger.LogError(retryEx, $"Failed to delete message with ID: {messageId} after retry");
                    throw;
                }
            }
        });
    }

    public async Task MarkMessageAsReceivedAsync(string messageId)
    {
        await ExecuteSafeAsync(async () =>
        {
            var message = await Context.OfflineMessages.FindAsync(messageId);
            if (message != null)
            {
                message.Received = true;
                await Context.SaveChangesAsync();
                _logger.LogInformation($"Marked message with ID: {messageId} as received");
                return;
            }

            var fileMessage = await Context.OfflineMessageFiles.FindAsync(messageId);
            if (fileMessage != null)
            {
                fileMessage.Received = true;
                await Context.SaveChangesAsync();
                _logger.LogInformation($"Marked file message with ID: {messageId} as received");
            }
            else
            {
                _logger.LogWarning($"Message with ID: {messageId} not found in either table");
            }
        });
    }
}