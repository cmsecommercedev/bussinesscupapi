using BussinessCupApi.Data;
using BussinessCupApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using BussinessCupApi.Attributes;
using Microsoft.AspNetCore.Authorization;

namespace BussinessCupApi.Controllers
{
    [Authorize(Roles = "Admin")]

    public class AdvertiseController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdvertiseController> _logger;
        private readonly CloudflareR2Manager _r2Manager;

        public AdvertiseController(ApplicationDbContext context, ILogger<AdvertiseController> logger, CloudflareR2Manager r2Manager)
        {
            _context = context;
            _logger = logger;
            _r2Manager = r2Manager;

        }

        // GET: Advertise (Reklamları listeleme sayfası)
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            ViewBag.Cities = _context.City.OrderBy(c => c.Order).ToList();

            var advertisements = await _context.Advertisements.OrderByDescending(a => a.UploadDate).ToListAsync();
            return View(advertisements);
        }

        // GET: Advertise/Create (Bu action kaldırılabilir veya boş bir ViewModel döndürebilir,
        // çünkü form artık ana Index view'ında)
        // [HttpGet]
        // public IActionResult Create()
        // {
        //     return PartialView("_CreateAdvertise", new CreateAdvertiseViewModel());
        // }

        // POST: Advertise/Create (Yeni reklam oluşturma)
        [HttpPost]
        [ValidateAntiForgeryToken]
        // CreateAdvertiseViewModel yerine doğrudan parametreler veya güncellenmiş ViewModel kullanılabilir.
        // Şimdilik ViewModel kullandığımızı varsayalım.
        public async Task<IActionResult> Create(CreateAdvertiseViewModel model)
        {
            if (ModelState.IsValid) // Model validasyonu CreateAdvertiseViewModel'e göre çalışır
            {
                // ViewModel'de ImageFile olduğunu varsayıyoruz.
                if (model.ImageFile != null && model.ImageFile.Length > 0)
                {
                    // Dosya boyutu ve tür kontrolü (önceki kod gibi)
                    if (model.ImageFile.Length > 5 * 1024 * 1024)
                    {
                        ModelState.AddModelError("ImageFile", "Dosya boyutu 5MB'dan büyük olamaz.");
                        // Hata durumunda Index view'ını model ile geri döndürmek gerekebilir
                        // veya TempData kullanılıp RedirectToAction yapılabilir.
                        TempData["ErrorMessage"] = "Dosya boyutu 5MB'dan büyük olamaz.";
                        return RedirectToAction(nameof(Index)); // Veya View(model) (eğer Create view'ı varsa)
                    }
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                    var ext = Path.GetExtension(model.ImageFile.FileName).ToLowerInvariant();
                    if (string.IsNullOrEmpty(ext) || !allowedExtensions.Contains(ext))
                    {
                        ModelState.AddModelError("ImageFile", "Geçersiz dosya formatı. Sadece JPG, PNG, GIF desteklenmektedir.");
                        TempData["ErrorMessage"] = "Geçersiz dosya formatı. Sadece JPG, PNG, GIF desteklenmektedir.";
                        return RedirectToAction(nameof(Index));
                    }

                    string? uploadedFilePath = null;
                    try
                    {
                        var key = $"advertiseimages/{Guid.NewGuid()}{Path.GetExtension(model.ImageFile.FileName)}";

                        using var stream = model.ImageFile.OpenReadStream();
                        await _r2Manager.UploadFileAsync(key, stream, model.ImageFile.ContentType);


                        uploadedFilePath = _r2Manager.GetFileUrl(key);

                        // Advertise nesnesini oluştur
                        var advertise = new Advertise
                        {
                            Name = model.Name,
                            ImagePath = uploadedFilePath, // Base64 yerine dosya yolu
                            AltText = model.AltText,
                            LinkUrl = model.LinkUrl,
                            IsActive = model.IsActive,
                            Category = model.Category,
                            UploadDate = DateTime.UtcNow,
                            CityId = model.Category == "G" ? null : model.CityID
                        };

                        _context.Advertisements.Add(advertise);
                        await _context.SaveChangesAsync();
                        _logger.LogInformation("Yeni reklam başarıyla oluşturuldu: {AdName}, Resim: {ImagePath}", advertise.Name, advertise.ImagePath);

                        TempData["SuccessMessage"] = $"'{advertise.Name}' reklamı başarıyla oluşturuldu.";
                        return RedirectToAction(nameof(Index));
                    }
                    catch (Exception ex)
                    {
                        TempData["ErrorMessage"] = "Reklam kaydedilirken bir hata oluştu. Lütfen tekrar deneyin.";
                    }
                }
                else
                {
                    ModelState.AddModelError("ImageFile", "Lütfen bir resim dosyası seçin.");
                    TempData["ErrorMessage"] = "Lütfen bir resim dosyası seçin.";
                }
            }
            else
            {
                // ModelState geçerli değilse hataları birleştirip TempData'ya ekle
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                TempData["ErrorMessage"] = string.Join("<br>", errors);
            }


            // Hata durumunda Index sayfasına yönlendir (form orada olduğu için)
            // Eğer Create için ayrı bir View kullanılsaydı, o View'ı model ile döndürmek daha uygun olurdu.
            ViewBag.Cities = _context.City.OrderBy(c => c.Order).ToList();

            return RedirectToAction(nameof(Index));
        }

        // POST: Advertise/Delete/5 (Reklam silme)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var advertise = await _context.Advertisements.FindAsync(id);
            if (advertise == null)
            {
                _logger.LogWarning("Silinecek reklam bulunamadı. ID: {AdvertiseId}", id);
                TempData["ErrorMessage"] = "Reklam bulunamadı.";
                return RedirectToAction(nameof(Index));
            }

            string? imagePathToDelete = advertise.ImagePath; // Silmeden önce yolu al
            string deletedAdName = advertise.Name;

            try
            {
                _context.Advertisements.Remove(advertise);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"'{deletedAdName}' reklamı ve ilişkili resmi başarıyla silindi.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Reklam veritabanından silinirken hata oluştu. ID: {AdvertiseId}", id);
                TempData["ErrorMessage"] = "Reklam silinirken bir hata oluştu. Lütfen tekrar deneyin.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Advertise/DeleteImage/5 (Reklam resmini silme)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteImage(int id)
        {
            var advertise = await _context.Advertisements.FindAsync(id);
            if (advertise == null)
            {
                _logger.LogWarning("Resmi silinecek reklam bulunamadı. ID: {AdvertiseId}", id);
                TempData["ErrorMessage"] = "Reklam bulunamadı.";
                return RedirectToAction(nameof(Index));
            }

            string? imagePathToDelete = advertise.ImagePath;
            string adName = advertise.Name;

            try
            {
                // Resim yolunu temizle
                advertise.ImagePath = null;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Reklam resmi başarıyla veritabanından silindi: {AdName} (ID: {AdvertiseId})", adName, id);


                TempData["SuccessMessage"] = $"'{adName}' reklamının resmi başarıyla silindi.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Reklam resmi veritabanından silinirken hata oluştu. ID: {AdvertiseId}", id);
                TempData["ErrorMessage"] = "Reklam resmi silinirken bir hata oluştu. Lütfen tekrar deneyin.";
                return RedirectToAction(nameof(Index));
            }
        }
    }

}