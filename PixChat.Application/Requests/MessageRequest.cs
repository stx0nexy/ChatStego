namespace PixChat.Application.Requests;

public class MessageRequest
{
    public string Base64Image { get; set; }
    public string EncryptedKey { get; set; }
}