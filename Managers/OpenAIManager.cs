using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace BussinessCupApi.Managers
{
    public class OpenAiManager
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<OpenAiManager> _logger;
        private readonly string _apiKey;

        public OpenAiManager(HttpClient httpClient, ILogger<OpenAiManager> logger, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            _apiKey = configuration["OpenAI:ApiKey"];
        }

        public async Task<string> GenerateMatchSummaryAsync(string homeTeam, string awayTeam, int homeScore, int awayScore, string goalDetails, string cardDetails)
        {
            var requestBody = new
            {
                model = "gpt-3.5-turbo",
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = $"Maç verilerine göre bir spor haberi yaz:\nEv sahibi takım: {homeTeam}\nDeplasman takımı: {awayTeam}\nSkor: {homeScore}-{awayScore}\nGoller: {goalDetails}\nKartlar: {cardDetails}\n\nDoğal, akıcı ve spor haberlerine uygun bir metin üret. Türkçe yaz."
                    }
                },
                temperature = 1,
                max_tokens = 1000
            };

            var requestJson = JsonSerializer.Serialize(requestBody);
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            requestMessage.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.SendAsync(requestMessage);
                response.EnsureSuccessStatusCode();

                var jsonResponse = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(jsonResponse);
                var content = doc.RootElement
                                 .GetProperty("choices")[0]
                                 .GetProperty("message")
                                 .GetProperty("content")
                                 .GetString();

                return content;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OpenAI isteği başarısız.");
                return "Spor haberi oluşturulamadı.";
            }
        }
    }
}
