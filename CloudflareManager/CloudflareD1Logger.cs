using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace BussinessCupApi.CloudflareManager
{


    public class CloudflareD1Logger : ILogger
    {
        private readonly string _categoryName;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _apiUrl;
        private readonly string _bearerToken;

        public CloudflareD1Logger(string categoryName, IHttpClientFactory httpClientFactory, string apiUrl, string bearerToken)
        {
            _categoryName = categoryName;
            _httpClientFactory = httpClientFactory;
            _apiUrl = apiUrl;
            _bearerToken = bearerToken;
        }

        public IDisposable BeginScope<TState>(TState state) => null;

        public bool IsEnabled(LogLevel logLevel)
        {
            // Burada istediğin minimum log seviyesini ayarlayabilirsin.
            return logLevel >= LogLevel.Information;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId,
            TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            var message = formatter(state, exception);

            // Async metot çağırımı için
            _ = LogAsync(logLevel, message, exception);
        }

        private async Task LogAsync(LogLevel logLevel, string message, Exception exception)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();

                var logObj = new
                {
                    ActivityLoggerDetails = message,
                    ActivityLoggerDate = DateTime.UtcNow.ToString("o"),
                    ActivityLoggerException = exception?.ToString() ?? "",
                    ActivityLoggerSource = _categoryName,
                };

                var json = JsonSerializer.Serialize(logObj);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Post, _apiUrl);
                request.Content = content;
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _bearerToken);

                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
            }
            catch
            {
                // Loglama sırasında hata oluşursa burada yakalayabilirsin
                // Genellikle loglamada hata bastırılır, uygulamayı etkilememesi için.
            }
        }
    }


}
