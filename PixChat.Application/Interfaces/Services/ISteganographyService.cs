namespace PixChat.Application.Interfaces.Services;

public interface ISteganographyService
{
    byte[] EmbedMessage(byte[] image, string message, string key);

    (byte[] message, DateTime timestamp, string encryptedAESKey, byte[] aesIV) ExtractFullMessage(byte[] image, string key);

    byte[] GetRandomImage();
}