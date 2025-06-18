namespace PixChat.Application.DTOs;

public class OfflineMessageDto
{
    public string Id { get; set; }
    public string SenderId { get; set; }
    public string? ReceiverId { get; set; }
    public int ChatId { get; set; }
    public byte[] StegoImage { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool Received { get; set; } = false;
    public bool IsGroup { get; set; } = false;
}