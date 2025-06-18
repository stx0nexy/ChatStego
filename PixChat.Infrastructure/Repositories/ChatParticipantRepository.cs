using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PixChat.Core.Entities;
using PixChat.Core.Interfaces;
using PixChat.Core.Interfaces.Repositories;
using PixChat.Infrastructure.Database;
using PixChat.Infrastructure.ExternalServices;

namespace PixChat.Infrastructure.Repositories;

public class ChatParticipantRepository : BaseDataService, IChatParticipantRepository
{
    public ChatParticipantRepository(
        IDbContextWrapper<ApplicationDbContext> dbContextWrapper,
        ILogger<ChatParticipantRepository> logger) : base(dbContextWrapper, logger)
    {
    }


    public async Task<IEnumerable<ChatParticipantEntity>> GetParticipantsByChatIdAsync(int chatId)
    {
        return await ExecuteSafeAsync(async () =>
        {
            return await Context.ChatParticipants
                .Where(p => p.ChatId == chatId)
                .ToListAsync();
        });
    }
    
    public async Task<ChatParticipantEntity?> GetParticipantByChatAndUserAsync(int chatId, int userId)
    {
        return await ExecuteSafeAsync(async () =>
        {
            return await Context.ChatParticipants
                .FirstOrDefaultAsync(cp => cp.ChatId == chatId && cp.UserId == userId);
        });
    }

    public async Task<ChatParticipantEntity?> GetByIdAsync(int participantId)
    {
        return await ExecuteSafeAsync(async () =>
        {
            return await Context.ChatParticipants
                .FirstOrDefaultAsync(p => p.Id == participantId);
        });
    }

    public async Task AddAsync(ChatParticipantEntity participant)
    {
        await ExecuteSafeAsync(async () =>
        {
            await Context.ChatParticipants.AddAsync(participant);
            await Context.SaveChangesAsync();
        });
    }

    public async Task DeleteAsync(int participantId)
    {
        await ExecuteSafeAsync(async () =>
        {
            var participant = await GetByIdAsync(participantId);
            if (participant != null)
            {
                Context.ChatParticipants.Remove(participant);
                await Context.SaveChangesAsync();
            }
        });
    }
    
    public async Task DeleteByChatAndUserAsync(int chatId, int userId)
    {
        await ExecuteSafeAsync(async () =>
        {
            var participantToRemove = await Context.ChatParticipants
                .FirstOrDefaultAsync(p => p.ChatId == chatId && p.UserId == userId);

            if (participantToRemove != null)
            {
                Context.ChatParticipants.Remove(participantToRemove);
                await Context.SaveChangesAsync();
                _logger.LogInformation("User {UserId} removed from chat {ChatId}.", userId, chatId);
            }
            else
            {
                _logger.LogWarning("User {UserId} not found in chat {ChatId} for removal.", userId, chatId);
            }
        });
    }

    public async Task UpdateAsync(ChatParticipantEntity participant)
    {
        await ExecuteSafeAsync(async () =>
        {
            Context.ChatParticipants.Update(participant);
            await Context.SaveChangesAsync();
        });
    }
}