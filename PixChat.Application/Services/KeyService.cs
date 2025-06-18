using AutoMapper;
using Microsoft.Extensions.Logging;
using PixChat.Application.Interfaces.Services;
using PixChat.Core.Interfaces.Repositories;

namespace PixChat.Application.Services;

public class KeyService : IKeyService
{
    private readonly IUserKeyRepository _userKeyRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<KeyService> _logger;

    public KeyService(
        ILogger<KeyService> logger,
        IUserKeyRepository userKeyRepository,
        IMapper mapper
    )
    {
        _logger = logger;
        _userKeyRepository = userKeyRepository;
        _mapper = mapper;
    }
    
    public async Task<string?> GetPublicKeyAsync(int userId)
    {
        try
        {
            var result = await _userKeyRepository.GetPublicKeyAsync(userId);
            if (result == null)
            {
                _logger.LogInformation("Public key not found for user {UserId}.", userId);
                return null;

            }
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving public key for user {UserId}.", userId);
            throw;
        }
    }

    public async Task SavePublicKeyAsync(int userId, string publicKey)
    {
        try
        {
            await _userKeyRepository.SavePublicKeyAsync(userId, publicKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while saving key for user {UserId}.", userId);
            throw;
        }
    }
}