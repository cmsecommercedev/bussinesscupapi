using System;
using Microsoft.Extensions.Logging;
using System.Net.Http;
namespace BussinessCupApi.CloudflareManager
{
    public class CloudflareD1LoggerProvider : ILoggerProvider
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _apiUrl;
        private readonly string _bearerToken;

        public CloudflareD1LoggerProvider(IHttpClientFactory httpClientFactory, string apiUrl, string bearerToken)
        {
            _httpClientFactory = httpClientFactory;
            _apiUrl = apiUrl;
            _bearerToken = bearerToken;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new CloudflareD1Logger(categoryName, _httpClientFactory, _apiUrl, _bearerToken);
        }

        public void Dispose()
        {
        }
    }


}
