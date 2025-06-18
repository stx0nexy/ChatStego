namespace PixChat.Application.Interfaces.Services;

public interface ITwoFactorService
{
    string GenerateCode();
    Task SendTwoFactorCodeAsync(string email);
    bool VerifyCode(string email, string code);
}