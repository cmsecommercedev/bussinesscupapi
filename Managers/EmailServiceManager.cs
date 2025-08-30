using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

public class EmailServiceManager
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public EmailServiceManager(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string toName, string resetLink)
    {
        var senderName = _configuration["Brevo:SenderName"];
        var senderEmail = _configuration["Brevo:SenderEmail"];
        var apiKey = _configuration["Brevo:ApiKey"];
        var mailsubject = _configuration["Brevo:Subject"];

        var payload = new
        {
            sender = new
            {
                name = senderName,
                email = senderEmail
            },
            to = new[]
            {
                new { email = toEmail, name = toName }
            },
            subject = mailsubject,
            htmlContent = $"<p>Merhaba {toName},</p><p>Şifrenizi sıfırlamak için <a href='{resetLink}'>buraya tıklayın</a>.</p>"
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email");
        request.Headers.Add("api-key", apiKey);
        request.Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            throw new Exception($"Mail gönderilemedi: {response.StatusCode}, {content}");
        }
    }
}
