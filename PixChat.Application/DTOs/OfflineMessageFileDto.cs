namespace PixChat.Application.DTOs;

public class OfflineMessageFileDto
{
    public string Id { get; set; }
    public string SenderId { get; set; }
    public string? ReceiverId { get; set; }
    public int ChatId { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool Received { get; set; } = false;
    public bool IsGroup { get; set; } = false;
    public bool IsFile { get; set; } = false;
    public string FileData { get; set; }
    public string? FileName { get; set; }
    public string? FileType { get; set; }
    public string? EncryptedAESKey { get; set; }
    public string? AESIV { get; set; }
}