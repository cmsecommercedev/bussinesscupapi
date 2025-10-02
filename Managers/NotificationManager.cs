using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using BussinessCupApi.ViewModels;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;

namespace BussinessCupApi.Managers
{
    public class NotificationManager
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;
        private readonly FirebaseMessaging _firebaseMessaging;
        private readonly OpenAiManager _aiManager;

        public NotificationManager(IConfiguration configuration, ILogger<NotificationManager> logger, OpenAiManager aiManager)
        {
            _configuration = configuration;
            _logger = logger;
            _aiManager = aiManager;

            if (FirebaseApp.DefaultInstance == null)
            {
                // Dosya yolu: appsettings.json'dan veya sabit olarak belirtebilirsin
                var firebaseJsonPath = Path.Combine(AppContext.BaseDirectory, "firebase_secret.json");
                if (!File.Exists(firebaseJsonPath))
                    throw new FileNotFoundException("Firebase config dosyası bulunamadı.", firebaseJsonPath);

                var json = File.ReadAllText(firebaseJsonPath);
                FirebaseApp.Create(new AppOptions()
                {
                    Credential = GoogleCredential.FromJson(json)
                });
            }

            _firebaseMessaging = FirebaseMessaging.DefaultInstance;
        }

        public async Task<(bool success, string message)> SendNotificationToAllUsers(NotificationViewModel model,string topicnoculture)
        {
            try
            {
                // Languages: tr (original), ru, ro, en
                var languages = new List<(string langCode, string displayName)>
                {
                    ("tr", "Turkish"),
                    ("ru", "Russian"),
                    ("ro", "Romanian"),
                    ("en", "English")
                };

                var responses = new List<string>();

                foreach (var (langCode, displayName) in languages)
                {
                    string title = model.TitleTr;
                    string body = model.MessageTr;

                    if (langCode != "tr")
                    {
                        // Translate title and body from Turkish to target language
                        title = await _aiManager.TranslateFromTurkishAsync(model.TitleTr ?? string.Empty, displayName);
                        body = await _aiManager.TranslateFromTurkishAsync(model.MessageTr ?? string.Empty, displayName);
                    }

                    var topic = $"{topicnoculture}_{langCode}";

                    var message = new Message()
                    {
                        Notification = new Notification()
                        {
                            Title = title,
                            Body = body
                        },
                        Topic = topic
                    };

                    var resp = await _firebaseMessaging.SendAsync(message);
                    responses.Add($"{topic}:{resp}");
                }

                return (true, string.Join("; ", responses));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Bildirim gönderilirken hata oluştu");
                return (false, "Bildirim gönderilirken bir hata oluştu.");
            }
        }

        public async Task<(bool success, string message)> SendNotificationToUser(string deviceToken, NotificationViewModel model)
        {
            try
            {
                var message = new Message()
                {
                    Notification = new Notification()
                    {
                        Title = model.TitleTr,
                        Body = model.MessageTr
                    },
                    Token = deviceToken
                };

                var response = await _firebaseMessaging.SendAsync(message);
                return (true, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kişiye özel bildirim gönderilirken hata oluştu");
                return (false, "Kişiye özel bildirim gönderilirken bir hata oluştu.");
            }
        }

        /// <summary>
        /// Firebase'de bir topic (grup) oluşturur. Firebase'de topic'ler otomatik oluşur, bu metot sadece isim kontrolü için kullanılabilir.
        /// </summary>
        public Task<(bool success, string message)> CreateGroup(string groupName)
        {
            // Firebase'de topic'ler otomatik olarak ilk abone ile oluşur, ekstra bir API çağrısı gerekmez.
            // Ancak uygulama tarafında grup ismini kaydetmek isterseniz burada ek işlemler yapabilirsiniz.
            if (string.IsNullOrWhiteSpace(groupName))
                return Task.FromResult((false, "Grup adı boş olamaz."));

            // Gerekirse burada veritabanına da kaydedebilirsiniz.
            return Task.FromResult((true, $"Grup '{groupName}' oluşturulmaya hazır (Firebase topic olarak)."));
        }

        public async Task<(bool success, string message)> SendNotificationToGroup(string groupName, NotificationViewModel model)
        {
            try
            {
                var message = new Message()
                {
                    Notification = new Notification()
                    {
                        Title = model.TitleTr,
                        Body = model.MessageTr
                    },
                    Topic = groupName
                };

                var response = await _firebaseMessaging.SendAsync(message);
                return (true, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gruba özel bildirim gönderilirken hata oluştu");
                return (false, "Gruba özel bildirim gönderilirken bir hata oluştu.");
            }
        }

        /// <summary>
        /// Bir veya birden fazla cihazı (token) belirtilen topic'e abone eder.
        /// </summary>
        public async Task<(bool success, string message)> SubscribeToTopicAsync(IReadOnlyList<string> deviceTokens, string topic)
        {
            try
            {
                var response = await _firebaseMessaging.SubscribeToTopicAsync(deviceTokens, topic);
                if (response.FailureCount > 0)
                {
                    var errors = response.Errors.Select(e => $"[{e.Index}] {e.Reason}");
                    return (false, $"Bazı tokenlar abone edilemedi: {string.Join(", ", errors)}");
                }
                return (true, $"{response.SuccessCount} cihaz '{topic}' konusuna abone edildi.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Topic'e abone edilirken hata oluştu");
                return (false, "Topic'e abone edilirken bir hata oluştu.");
            }
        }

        /// <summary>
        /// Bir veya birden fazla cihazı (token) belirtilen topic'ten çıkarır.
        /// </summary>
        public async Task<(bool success, string message)> UnsubscribeFromTopicAsync(IReadOnlyList<string> deviceTokens, string topic)
        {
            try
            {
                var response = await _firebaseMessaging.UnsubscribeFromTopicAsync(deviceTokens, topic);
                if (response.FailureCount > 0)
                {
                    var errors = response.Errors.Select(e => $"[{e.Index}] {e.Reason}");
                    return (false, $"Bazı tokenlar topic'ten çıkarılamadı: {string.Join(", ", errors)}");
                }
                return (true, $"{response.SuccessCount} cihaz '{topic}' konusundan çıkarıldı.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Topic'ten çıkarılırken hata oluştu");
                return (false, "Topic'ten çıkarılırken bir hata oluştu.");
            }
        }
    }
}