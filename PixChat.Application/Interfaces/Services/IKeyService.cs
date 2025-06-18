namespace PixChat.Application.Interfaces.Services;

public interface IKeyService
{
    Task<string?> GetPublicKeyAsync(int userId);

    Task SavePublicKeyAsync(int userId, string publicKey);
}