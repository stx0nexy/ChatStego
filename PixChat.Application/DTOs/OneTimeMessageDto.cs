namespace PixChat.Application.DTOs;

public class OneTimeMessageDto
{
    public string Id { get; set; }
    public string? SenderId { get; set; }
    public string ReceiverId { get; set; }
    public int ChatId { get; set; }
    public byte[] StegoImage { get; set; }
    public string EncryptionKey { get; set; }
    public int MessageLength { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool Received { get; set; } = false;
    public bool Read { get; set; } = false;
}