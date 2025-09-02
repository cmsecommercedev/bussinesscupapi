using Microsoft.AspNetCore.Authorization; // DateTime için
using Microsoft.AspNetCore.Hosting; // IWebHostEnvironment için
using Microsoft.AspNetCore.Http; // IFormFile için
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BussinessCupApi.Data;
using BussinessCupApi.Managers;
using BussinessCupApi.Models;
using BussinessCupApi.Models.Api;
using BussinessCupApi.ViewModels;
using System;
using System.Collections.Generic; // List için
using System.IO; // Path ve File işlemleri için
using System.Linq;
using System.Threading.Tasks;

namespace BussinessCupApi.Controllers
{
    [Authorize(Roles = "Admin")]

    public class MatchNewsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly CloudflareR2Manager _r2Manager;
        private readonly CustomUserManager _customUserManager;
        private readonly OpenAiManager _openAIManager;
        private readonly ILogger<MatchNewsController> _logger;

        // Dependency Injection ile gerekli servisleri alıyoruz
        public MatchNewsController(
            ApplicationDbContext context,
            CloudflareR2Manager r2Manager,
            CustomUserManager customUserManager,
            OpenAiManager openAIManager,
            ILogger<MatchNewsController> logger)
        {
            _context = context;
            _r2Manager = r2Manager;
            _customUserManager = customUserManager;
            _openAIManager = openAIManager;
            _logger = logger;
        }

                // GET: MatchNews veya MatchNews/Index
        // Hem listeyi hem de ekleme formunu gösterir
        public async Task<IActionResult> Index(string culture = "tr")
        {
            var user = await _customUserManager.GetUserAsync(User);

            var viewModel = await GetMatchNewsIndexViewModelAsync(culture);
            return View(viewModel);
        }

        private async Task<MatchNewsIndexViewModel> GetMatchNewsIndexViewModelAsync(string culture = "tr")
        {
            // Admin ise tüm şehirler ve haberler
            var matchNewsList = await _context.MatchNews
                .Include(m => m.Photos)
                .Include(m => m.Contents)
                .OrderByDescending(m => m.CreatedDate)
                .ToListAsync();

            var newsWithContent = matchNewsList
                .Select(m => new MatchNewsWithContentDto
                {
                    MatchNews = m,
                    Content = m.Contents.FirstOrDefault(c => c.Culture == culture)
                })
                .ToList();

            return new MatchNewsIndexViewModel
            {
                NewMatchNews = new MatchNewsInputModel(),
                MatchNewsList = newsWithContent,
                Culture = culture
            };
        }
        // AJAX ile haber listesi getirme
        [HttpGet]
        public async Task<IActionResult> GetMatchNewsList(string culture = "tr")
        {
            var matchNewsList = await _context.MatchNews
                .Include(m => m.Photos)
                .Include(m => m.Contents)
                .OrderByDescending(m => m.CreatedDate)
                .ToListAsync();

            var newsWithContent = matchNewsList
                .Select(m => new MatchNewsWithContentDto
                {
                    MatchNews = m,
                    Content = m.Contents.FirstOrDefault(c => c.Culture == culture)
                })
                .ToList();

            return PartialView("_MatchNewsListPartial", new MatchNewsIndexViewModel
            {
                MatchNewsList = newsWithContent,
                Culture = culture
            });
        }

        // ...existing code...
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MatchNewsIndexViewModel model, IFormFile MainPhoto, List<IFormFile> ImageFiles)
        {
            var input = model.NewMatchNews;

            if (ModelState.IsValid)
            {
                var matchNews = new MatchNews
                {
                    IsMainNews = input.IsMainNews,
                    CreatedDate = DateTime.UtcNow,
                    Published = true
                };

                // Ana fotoğraf yükleme
                if (MainPhoto != null && MainPhoto.Length > 0)
                {
                    var key = $"matchnewsimages/{Guid.NewGuid()}{Path.GetExtension(MainPhoto.FileName)}";
                    using var stream = MainPhoto.OpenReadStream();
                    await _r2Manager.UploadFileAsync(key, stream, MainPhoto.ContentType);
                    string relativePath = _r2Manager.GetFileUrl(key);
                    matchNews.MatchNewsMainPhoto = relativePath;
                }

                // Diğer fotoğraflar
                if (ImageFiles != null && ImageFiles.Count > 0)
                {
                    foreach (var file in ImageFiles)
                    {
                        if (file.Length > 0)
                        {
                            var key = $"matchnewsimages/{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                            using var stream = file.OpenReadStream();
                            await _r2Manager.UploadFileAsync(key, stream, file.ContentType);
                            string relativePath = _r2Manager.GetFileUrl(key);

                            var matchNewsPhoto = new MatchNewsPhoto
                            {
                                PhotoUrl = relativePath,
                                MatchNews = matchNews
                            };
                            matchNews.Photos.Add(matchNewsPhoto);
                        }
                    }
                }

                // Çok dilli içerik ekle (varsayılan culture: "tr")
                var content = new MatchNewsContent
                {
                    Culture = "tr",
                    Title = input.Title,
                    Subtitle = input.Subtitle,
                    DetailsTitle = input.DetailsTitle,
                    Details = input.Details
                };
                matchNews.Contents.Add(content);

                _context.Add(matchNews);
                await _context.SaveChangesAsync();

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = true, message = "Haber başarıyla eklendi." });
                }
                
                TempData["SuccessMessage"] = "Haber başarıyla eklendi.";
                return RedirectToAction(nameof(Index));
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = "Form validation hatası", errors = ModelState });
            }

            var viewModel = await GetMatchNewsIndexViewModelAsync();
            viewModel.NewMatchNews = input;
            return View("Index", viewModel);
        }
        // ...existing code...
        // GET: MatchNews/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                TempData["ErrorMessage"] = "Geçersiz haber ID'si.";
                return RedirectToAction(nameof(Index));
            }

            var matchNews = await _context.MatchNews
                .Include(m => m.Photos)
                .Include(m => m.Contents) // Tüm dillerdeki içerikleri de getir
                .FirstOrDefaultAsync(m => m.Id == id);

            if (matchNews == null)
            {
                TempData["ErrorMessage"] = "Haber bulunamadı.";
                return RedirectToAction(nameof(Index));
            }

            // View'a tüm içerikleri gönder
            return View(matchNews);
        }

        private bool MatchNewsExists(int id)
        {
            return _context.MatchNews.Any(e => e.Id == id);
        }

        // GET: MatchNews/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var matchNews = await _context.MatchNews
               .Include(m => m.Photos) // Fotoğrafları da yükle
               .FirstOrDefaultAsync(m => m.Id == id);
            if (matchNews == null) return NotFound();
            // Details view'ını oluşturmanız gerekecek.
            // return View(matchNews); // Details view'ını oluşturduktan sonra
            TempData["InfoMessage"] = "Detay sayfası henüz oluşturulmadı.";
            return RedirectToAction(nameof(Index)); // Şimdilik Index'e dön
        }

        // POST: MatchNews/TogglePublish/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TogglePublish(int id)
        {
            var matchNews = await _context.MatchNews.FindAsync(id);
            if (matchNews == null)
            {
                TempData["ErrorMessage"] = "Haber bulunamadı.";
                return RedirectToAction(nameof(Index));
            }

            matchNews.Published = !matchNews.Published; // Durumu tersine çevir
            _context.Update(matchNews);
            await _context.SaveChangesAsync();

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, message = $"Haber durumu başarıyla güncellendi: {(matchNews.Published ? "Yayında" : "Yayında Değil")}." });
            }

            TempData["SuccessMessage"] = $"Haber durumu başarıyla güncellendi: {(matchNews.Published ? "Yayında" : "Yayında Değil")}.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePhoto(int id)
        {
            try
            {
                var photo = await _context.MatchNewsPhotos.FindAsync(id);
                if (photo == null)
                {
                    return NotFound();
                }

                // Cloudflare R2'den dosyayı sil
                if (!string.IsNullOrEmpty(photo.PhotoUrl))
                {
                    var path = new Uri(photo.PhotoUrl).AbsolutePath;

                    await _r2Manager.DeleteFileAsync(path);
                }

                // Veritabanından kaydı sil
                _context.MatchNewsPhotos.Remove(photo);
                await _context.SaveChangesAsync();

                return Ok();
            }
            catch (Exception ex)
            {
                // Hata durumunda 500 dön
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet]
        public JsonResult GetTeamsByCity(int cityId)
        {
            var teams = _context.Teams
                .Where(t => t.CityID == cityId)
                .Select(t => new { t.TeamID, t.Name })
                .ToList();
            return Json(teams);
        }

        /// <summary>
        /// Maç haberini belirtilen dile çevirir
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TranslateMatchNews([FromBody] TranslateMatchNewsRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Text) || string.IsNullOrEmpty(request.TargetLanguage))
                {
                    return BadRequest(new { success = false, message = "Metin ve hedef dil gereklidir." });
                }

                var translatedText = await _openAIManager.TranslateMatchNewsAsync(
                    request.Text, 
                    request.TargetLanguage, 
                    request.SourceLanguage ?? "Türkçe"
                );

                return Json(new { success = true, translatedText = translatedText });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Çeviri işlemi başarısız");
                return StatusCode(500, new { success = false, message = "Çeviri işlemi sırasında bir hata oluştu." });
            }
        }

        /// <summary>
        /// Maç haberini birden fazla dile çevirir
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TranslateMatchNewsToMultipleLanguages([FromBody] TranslateMatchNewsMultipleRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Text) || request.TargetLanguages == null || !request.TargetLanguages.Any())
                {
                    return BadRequest(new { success = false, message = "Metin ve hedef diller gereklidir." });
                }

                var translations = await _openAIManager.TranslateMatchNewsToMultipleLanguagesAsync(
                    request.Text, 
                    request.TargetLanguages, 
                    request.SourceLanguage ?? "Türkçe"
                );

                return Json(new { success = true, translations = translations });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Çoklu dil çevirisi başarısız");
                return StatusCode(500, new { success = false, message = "Çeviri işlemi sırasında bir hata oluştu." });
            }
        }
        // TODO: Gerçek bir Delete Action'ı (istenirse) veya resim silme/yönetme eklenebilir.
    }
}
