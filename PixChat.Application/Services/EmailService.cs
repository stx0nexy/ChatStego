using Mailjet.Client;
using Mailjet.Client.Resources;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using PixChat.Application.Interfaces.Services;

namespace PixChat.Application.Services;

public class EmailService : IEmailService
{
    private readonly string _apiKey;
    private readonly string _apiSecret;
    private readonly string _fromEmail;
    private readonly string _fromName;

    public EmailService(IConfiguration configuration)
    {
        _apiKey = configuration["MailJet:ApiKey"];
        _apiSecret = configuration["MailJet:ApiSecret"];
        _fromEmail = configuration["MailJet:FromEmail"];
        _fromName = configuration["MailJet:FromName"];
    }

    public async Task SendEmailAsync(string email, string subject, string message)
    {
        var client = new MailjetClient(_apiKey, _apiSecret);

        var request = new MailjetRequest
            {
                Resource = Send.Resource
            }
            .Property(Send.FromEmail, _fromEmail)
            .Property(Send.FromName, _fromName)
            .Property(Send.Subject, subject)
            .Property(Send.TextPart, message)
            .Property(Send.HtmlPart, $"<h3>{message}</h3>")
            .Property(Send.Recipients, new JArray {
                new JObject {
                    { "Email", email }
                }
            });

        MailjetResponse response = await client.PostAsync(request);
        
        if (response.IsSuccessStatusCode)
        {
            Console.WriteLine($"Email sent to {email} with subject: {subject}");
        }
        else
        {
            Console.WriteLine($"Failed to send email. Status: {response.StatusCode}, ErrorInfo: {response.GetErrorMessage()}");
            Console.WriteLine($"Response content: {response.Content}");
        }
    }
}

