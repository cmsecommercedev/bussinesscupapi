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

        /// <summary>
        /// Maç haberi metnini belirtilen dile çevirir
        /// </summary>
        /// <param name="matchNewsText">Çevrilecek maç haberi metni</param>
        /// <param name="targetLanguage">Hedef dil (örn: "English", "Deutsch", "Français", "Español")</param>
        /// <param name="sourceLanguage">Kaynak dil (varsayılan: "Türkçe")</param>
        /// <returns>Çevrilmiş metin</returns>
        public async Task<string> TranslateMatchNewsAsync(string matchNewsText, string targetLanguage, string sourceLanguage = "Türkçe")
        {
            var requestBody = new
            {
                model = "gpt-3.5-turbo",
                messages = new[]
                {
                    new
                    {
                        role = "system",
                        content = $"Sen profesyonel bir spor muhabiri ve çevirmen. {sourceLanguage} dilinden {targetLanguage} diline spor haberlerini çeviriyorsun. Çeviride spor terminolojisini doğru kullan, doğal ve akıcı bir dil kullan, orijinal metnin anlamını ve duygusunu koru."
                    },
                    new
                    {
                        role = "user",
                        content = $"Aşağıdaki maç haberini {targetLanguage} diline çevir:\n\n{matchNewsText}\n\nÇeviriyi sadece {targetLanguage} dilinde yap, başka açıklama ekleme."
                    }
                },
                temperature = 0.7,
                max_tokens = 1500
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
                _logger.LogError(ex, $"OpenAI çeviri isteği başarısız. Hedef dil: {targetLanguage}");
                return $"Çeviri yapılamadı. Hata: {ex.Message}";
            }
        }

        // Türkçe kaynak alınarak hedef dile çeviri (basit, futbol temalı ton)
        public async Task<string> TranslateFromTurkishAsync(string text, string targetLanguage)
        {
            var requestBody = new
            {
                model = "gpt-3.5-turbo",
                messages = new object[]
                {
                    new { role = "system", content = $"Sen profesyonel bir spor çevirmenisin. Verilen metni Türkçe'den {targetLanguage} diline çevir. Futbol temalı, doğal ve akıcı bir dil kullan; anlamı koru, abartı ekleme, haberleşme dili tutarlı kalsın." },
                    new { role = "user", content = text }
                },
                temperature = 0.7,
                max_tokens = 1200
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

                return content ?? string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OpenAI basit çeviri isteği başarısız.");
                return "";
            }
        }

        /// <summary>
        /// Maç haberi metnini birden fazla dile çevirir
        /// </summary>
        /// <param name="matchNewsText">Çevrilecek maç haberi metni</param>
        /// <param name="targetLanguages">Hedef diller listesi</param>
        /// <param name="sourceLanguage">Kaynak dil (varsayılan: "Türkçe")</param>
        /// <returns>Dil-çeviri çiftleri</returns>
        public async Task<Dictionary<string, string>> TranslateMatchNewsToMultipleLanguagesAsync(string matchNewsText, List<string> targetLanguages, string sourceLanguage = "Türkçe")
        {
            var translations = new Dictionary<string, string>();
            
            foreach (var language in targetLanguages)
            {
                try
                {
                    var translation = await TranslateMatchNewsAsync(matchNewsText, language, sourceLanguage);
                    translations[language] = translation;
                    
                    // Rate limiting için kısa bekleme
                    await Task.Delay(100);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Dil {language} için çeviri başarısız");
                    translations[language] = $"Çeviri hatası: {ex.Message}";
                }
            }

            return translations;
        }
    }
}
