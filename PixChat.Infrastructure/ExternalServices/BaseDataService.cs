using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PixChat.Core.Interfaces;
using PixChat.Infrastructure.Database;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PixChat.Infrastructure.ExternalServices;

public abstract class BaseDataService
{
    private readonly IDbContextWrapper<ApplicationDbContext> _dbContextWrapper;
    public readonly ILogger<BaseDataService> _logger;

    protected BaseDataService(
        IDbContextWrapper<ApplicationDbContext> dbContextWrapper,
        ILogger<BaseDataService> logger)
    {
        _dbContextWrapper = dbContextWrapper;
        _logger = logger;
    }
    
    protected ApplicationDbContext Context => _dbContextWrapper.DbContext;

    protected Task ExecuteSafeAsync(Func<Task> action, CancellationToken cancellationToken = default) =>
        ExecuteSafeAsync(token => action(), cancellationToken);

    protected Task<TResult> ExecuteSafeAsync<TResult>(Func<Task<TResult>> action, CancellationToken cancellationToken = default) =>
        ExecuteSafeAsync(token => action(), cancellationToken);
    
    private async Task ExecuteSafeAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken = default)
    {
        if (Context.Database.CurrentTransaction != null)
        {
            await action(cancellationToken);
            return;
        }

        IDbContextTransaction transaction = null;
        try
        {
            transaction = await _dbContextWrapper.BeginTransactionAsync(cancellationToken);
            await action(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            if (transaction != null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }
            _logger.LogError(ex, "Transaction is rollbacked");
            throw;
        }
        finally
        {
            transaction?.Dispose();
        }
    }

    private async Task<TResult> ExecuteSafeAsync<TResult>(Func<CancellationToken, Task<TResult>> action, CancellationToken cancellationToken = default)
    {
        if (Context.Database.CurrentTransaction != null)
        {
            return await action(cancellationToken);
        }

        IDbContextTransaction transaction = null;
        try
        {
            transaction = await _dbContextWrapper.BeginTransactionAsync(cancellationToken);
            var result = await action(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch (Exception ex)
        {
            if (transaction != null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }
            _logger.LogError(ex, "Transaction is rollbacked");
            throw;
        }
        finally
        {
            transaction?.Dispose();
        }
    }
}