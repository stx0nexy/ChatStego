namespace PixChat.Core.Entities;

public class OneTimeMessage
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string? SenderId { get; set; }
    public string ReceiverId { get; set; }
    public int ChatId { get; set; }
    public byte[] StegoImage { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool Received { get; set; } = false;
    public bool Read { get; set; } = false;
}