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
        private readonly ILogger<MatchNewsController> _logger;
        private readonly OpenAiManager _openAIManager;

        // Dependency Injection ile gerekli servisleri alıyoruz
        public MatchNewsController(
            ApplicationDbContext context,
            CloudflareR2Manager r2Manager,
            CustomUserManager customUserManager,
            ILogger<MatchNewsController> logger,
            OpenAiManager openAIManager)
        {
            _context = context;
            _r2Manager = r2Manager;
            _customUserManager = customUserManager;
            _logger = logger;
            _openAIManager = openAIManager;
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
                    IsMainNews = true,
                    Category = input.Category,
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

                // Çok dilli içerik ekle (tr, en, ro, ru)
                var trContent = new MatchNewsContent
                {
                    Culture = "tr",
                    Title = input.Title_tr,
                    Subtitle = input.Subtitle_tr,
                    DetailsTitle = input.DetailsTitle_tr,
                    Details = input.Details_tr
                };
                matchNews.Contents.Add(trContent);

                var enContent = new MatchNewsContent
                {
                    Culture = "en",
                    Title = input.Title_en,
                    Subtitle = input.Subtitle_en,
                    DetailsTitle = input.DetailsTitle_en,
                    Details = input.Details_en
                };
                matchNews.Contents.Add(enContent);

                var roContent = new MatchNewsContent
                {
                    Culture = "ro",
                    Title = input.Title_ro,
                    Subtitle = input.Subtitle_ro,
                    DetailsTitle = input.DetailsTitle_ro,
                    Details = input.Details_ro
                };
                matchNews.Contents.Add(roContent);

                var ruContent = new MatchNewsContent
                {
                    Culture = "ru",
                    Title = input.Title_ru,
                    Subtitle = input.Subtitle_ru,
                    DetailsTitle = input.DetailsTitle_ru,
                    Details = input.Details_ru
                };
                matchNews.Contents.Add(ruContent);

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

        // POST: MatchNews/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, IFormFile MainPhoto, List<IFormFile> ImageFiles)
        {
            var matchNews = await _context.MatchNews
                .Include(m => m.Photos)
                .Include(m => m.Contents)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (matchNews == null)
            {
                TempData["ErrorMessage"] = "Haber bulunamadı.";
                return RedirectToAction(nameof(Index));
            }

            // Category güncelle
            if (Request.Form.TryGetValue("Category", out var catVal))
            {
                if (Enum.TryParse<NewsCategory>(catVal.ToString(), out var cat))
                {
                    matchNews.Category = cat;
                }
            }

            // Ana fotoğraf güncelle
            if (MainPhoto != null && MainPhoto.Length > 0)
            {
                var key = $"matchnewsimages/{Guid.NewGuid()}{Path.GetExtension(MainPhoto.FileName)}";
                using var stream = MainPhoto.OpenReadStream();
                await _r2Manager.UploadFileAsync(key, stream, MainPhoto.ContentType);
                string relativePath = _r2Manager.GetFileUrl(key);
                matchNews.MatchNewsMainPhoto = relativePath;
            }

            // Yeni ek fotoğrafları ekle
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

            // Çok dilliler: helper
            void UpsertContent(string culture, string title, string subtitle, string detailsTitle, string details)
            {
                var existing = matchNews.Contents.FirstOrDefault(c => c.Culture == culture);
                if (existing == null)
                {
                    existing = new MatchNewsContent { Culture = culture, MatchNews = matchNews };
                    matchNews.Contents.Add(existing);
                }
                existing.Title = title;
                existing.Subtitle = subtitle;
                existing.DetailsTitle = detailsTitle;
                existing.Details = details;
            }

            UpsertContent("tr",
                Request.Form["Title_tr"],
                Request.Form["Subtitle_tr"],
                Request.Form["DetailsTitle_tr"],
                Request.Form["Details_tr"]);

            UpsertContent("en",
                Request.Form["Title_en"],
                Request.Form["Subtitle_en"],
                Request.Form["DetailsTitle_en"],
                Request.Form["Details_en"]);

            UpsertContent("ro",
                Request.Form["Title_ro"],
                Request.Form["Subtitle_ro"],
                Request.Form["DetailsTitle_ro"],
                Request.Form["Details_ro"]);

            UpsertContent("ru",
                Request.Form["Title_ru"],
                Request.Form["Subtitle_ru"],
                Request.Form["DetailsTitle_ru"],
                Request.Form["Details_ru"]);

            _context.Update(matchNews);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Haber güncellendi.";
            return RedirectToAction(nameof(Edit), new { id = matchNews.Id });
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

        // Alan bazlı çeviri (TR -> hedef dil)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TranslateField([FromBody] TranslateMatchNewsRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Text) || string.IsNullOrWhiteSpace(request.TargetLanguage))
                {
                    return BadRequest(new { success = false, message = "Metin ve hedef dil gereklidir." });
                }

                var translatedText = await _openAIManager.TranslateFromTurkishAsync(
                    request.Text, 
                    request.SourceLanguage ?? "Türkçe"
                );

                return Json(new { success = true, translatedText });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Alan çevirisi başarısız");
                return StatusCode(500, new { success = false, message = "Çeviri sırasında bir hata oluştu." });
            }
        }
        
        
        // TODO: Gerçek bir Delete Action'ı (istenirse) veya resim silme/yönetme eklenebilir.
    }
}
