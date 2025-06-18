namespace PixChat.Core.Interfaces.Repositories;

public interface IUserKeyRepository
{
    Task<string?> GetPublicKeyAsync(int userId);

    Task SavePublicKeyAsync(int userId, string publicKey);
}