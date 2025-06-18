using Microsoft.Extensions.Caching.Memory;
using PixChat.Application.Interfaces.Services;

namespace PixChat.Application.Services;

public class TwoFactorService : ITwoFactorService
{
    private readonly IEmailService _emailService;
    private readonly IMemoryCache _cache;

    public TwoFactorService(IEmailService emailService, IMemoryCache cache)
    {
        _emailService = emailService;
        _cache = cache;
    }

    public string GenerateCode()
    {
        var random = new Random();
        return random.Next(100000, 999999).ToString();
    }

    public async Task SendTwoFactorCodeAsync(string email)
    {
        var code = GenerateCode();
        var expires = DateTime.UtcNow.AddMinutes(10);
        _cache.Set(email, (code, expires), TimeSpan.FromMinutes(10));

        await _emailService.SendEmailAsync(email, "Your 2FA Code", $"Your code is: {code}");
    }

    public bool VerifyCode(string email, string code)
    {
        if (!_cache.TryGetValue(email, out (string Code, DateTime Expires) entry))
            return false;

        if (entry.Expires < DateTime.UtcNow)
        {
            _cache.Remove(email);
            return false;
        }

        return string.Equals(entry.Code.Trim(), code.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}