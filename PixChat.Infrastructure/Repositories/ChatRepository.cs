using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PixChat.Core.Entities;
using PixChat.Core.Interfaces;
using PixChat.Core.Interfaces.Repositories;
using PixChat.Infrastructure.Database;
using PixChat.Infrastructure.ExternalServices;

namespace PixChat.Infrastructure.Repositories;

public class ChatRepository: BaseDataService, IChatRepository
{
    public ChatRepository(
    IDbContextWrapper<ApplicationDbContext> dbContextWrapper,
        ILogger<ChatRepository> logger) : base(dbContextWrapper, logger)
    {
    }
    public async Task<ChatEntity?> GetPrivateChatIfExistsAsync(int userId, int contactUserId)
    {
        return await ExecuteSafeAsync(async () =>
        {
            return await Context.Chats
                .Where(c => !c.IsGroup &&
                            c.Participants.Any(p => p.UserId == userId) &&
                            c.Participants.Any(p => p.UserId == contactUserId))
                .FirstOrDefaultAsync();
        });
    }
    
    public async Task<IEnumerable<ChatEntity>> GetAllAsync()
    {
        return await ExecuteSafeAsync(async () =>
        {
            return await Context.Chats
                .Include(c => c.Participants)
                .ToListAsync();
        });
    }

    public async Task<ChatEntity?> GetByIdAsync(int id)
    {
        return await ExecuteSafeAsync(async () =>
        {
            return await Context.Chats
                .Include(c => c.Participants)
                .FirstOrDefaultAsync(c => c.Id == id);
        });
    }

    public async Task<int> AddAsync(ChatEntity entity)
    {
        return await ExecuteSafeAsync(async () =>
        {
            await Context.Chats.AddAsync(entity);
            await Context.SaveChangesAsync();
            return entity.Id;
        });
    }

    public async Task UpdateAsync(ChatEntity entity)
    {
        await ExecuteSafeAsync(async () =>
        {
            Context.Chats.Update(entity);
            await Context.SaveChangesAsync();
        });
    }

    public async Task DeleteAsync(int id)
    {
        await ExecuteSafeAsync(async () =>
        {
            var entity = await Context.Chats.FindAsync(id);
            if (entity != null)
            {
                Context.Chats.Remove(entity);
                await Context.SaveChangesAsync();
            }
        });
    }
    
    public async Task<IEnumerable<UserEntity>> GetParticipantsByChatIdAsync(int chatId)
    {
        return await ExecuteSafeAsync(async () =>
        {
            var participants = await Context.ChatParticipants
                .Where(cp => cp.ChatId == chatId)
                .Include(cp => cp.User)
                .Select(cp => cp.User)
                .ToListAsync();

            return participants;
        });
    }
    
    public async Task<List<ChatEntity>> GetChatsByUserIdAsync(int userId)
    {
        return await ExecuteSafeAsync(async () =>
        {
            return await Context.Set<ChatEntity>()
                .Where(chat => chat.Participants.Any(p => p.UserId == userId))
                .ToListAsync();
        });
    }

}