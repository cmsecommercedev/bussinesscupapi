using System;
using System.Linq;
using System.Threading.Tasks;
using BussinessCupApi.Data;
using BussinessCupApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;
using Microsoft.AspNetCore.Http;
using BussinessCupApi.Managers;

namespace BussinessCupApi.Controllers
{
	[Authorize(Roles = "Admin")]
	public class StaticContentController : Controller
	{
		private readonly ApplicationDbContext _context;
		private readonly CloudflareR2Manager _r2Manager;
		private readonly OpenAiManager _openAIManager;

		public StaticContentController(ApplicationDbContext context, CloudflareR2Manager r2Manager, OpenAiManager openAIManager)
		{
			_context = context;
			_r2Manager = r2Manager;
			_openAIManager = openAIManager;
		}

		[HttpGet]
		public async Task<IActionResult> Index()
		{
			var items = await _context.StaticKeyValues
				.AsNoTracking()
				.OrderBy(x => x.Key)
				.ToListAsync();

			return View(items);
		}

		[HttpGet]
		public async Task<IActionResult> Edit(string key)
		{
			if (string.IsNullOrWhiteSpace(key)) return RedirectToAction(nameof(Index));
			var item = await _context.StaticKeyValues.AsNoTracking().FirstOrDefaultAsync(x => x.Key == key);
			if (item == null) return NotFound();
			return View(item);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(string key, StaticKeyValue model)
		{
			if (string.IsNullOrWhiteSpace(key)) return RedirectToAction(nameof(Index));

			var entity = await _context.StaticKeyValues.FirstOrDefaultAsync(x => x.Key == key);
			if (entity == null) return NotFound(); // yeni key oluşturulmaz

			// sadece value güncellenir
			entity.Value = model?.Value ?? string.Empty;
			entity.UpdatedAt = DateTime.UtcNow;

			await _context.SaveChangesAsync();
			TempData["SuccessMessage"] = $"'{key}' içeriği güncellendi.";
			return RedirectToAction(nameof(Index));
		}

		// RICH STATIC CONTENT

		[HttpGet]
		public async Task<IActionResult> RichStatic()
		{
			var items = await _context.RichStaticContents
				.Include(x => x.Season)
				.Include(x => x.Category)
				.AsNoTracking()
				.OrderByDescending(x => x.UpdatedAt)
				.ToListAsync();

			var seasons = await _context.Season
				.AsNoTracking()
				.OrderByDescending(x => x.SeasonID)
				.ToListAsync();

			var categories = await _context.RichContentCategories
				.AsNoTracking()
				.Where(x => x.IsActive)
				.OrderBy(x => x.Name)
				.ToListAsync();

			ViewBag.Items = items;
			ViewBag.Seasons = seasons;
			ViewBag.Categories = categories;
			return View(new RichStaticContent());
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> RichStatic(RichStaticContent model)
		{
			if (!ModelState.IsValid)
			{
				ViewBag.Items = await _context.RichStaticContents
					.Include(x => x.Season)
					.Include(x => x.Category)
					.AsNoTracking()
					.OrderByDescending(x => x.UpdatedAt)
					.ToListAsync();
				
				var seasons = await _context.Season
					.AsNoTracking()
					.OrderByDescending(x => x.SeasonID)
					.ToListAsync();
				ViewBag.Seasons = seasons;

				var categories = await _context.RichContentCategories
					.AsNoTracking()
					.Where(x => x.IsActive)
					.OrderBy(x => x.Name)
					.ToListAsync();
				ViewBag.Categories = categories;
				
				return View(model);
			}

			// Get category code for file naming
			var categoryCode = "misc";
			if (model.CategoryId.HasValue)
			{
				var category = await _context.RichContentCategories
					.AsNoTracking()
					.FirstOrDefaultAsync(x => x.Id == model.CategoryId.Value);
				if (category != null && !string.IsNullOrWhiteSpace(category.Code))
				{
					categoryCode = category.Code.Trim().ToLower();
				}
			}

			// Görsel yüklendiyse R2'ye yükle
			if (model.MediaFile != null && model.MediaFile.Length > 0)
			{
				var ext = Path.GetExtension(model.MediaFile.FileName);
				var key = $"richstatic/{categoryCode}/{Guid.NewGuid()}{ext}";
				using var stream = model.MediaFile.OpenReadStream();
				await _r2Manager.UploadFileAsync(key, stream, model.MediaFile.ContentType);
				model.MediaUrl = _r2Manager.GetFileUrl(key);
			}

			// Profil görseli yüklendiyse R2'ye yükle
			if (model.ProfileImageFile != null && model.ProfileImageFile.Length > 0)
			{
				var ext = Path.GetExtension(model.ProfileImageFile.FileName);
				var key = $"richstatic/profile/{categoryCode}/{Guid.NewGuid()}{ext}";
				using var stream = model.ProfileImageFile.OpenReadStream();
				await _r2Manager.UploadFileAsync(key, stream, model.ProfileImageFile.ContentType);
				model.ProfileImageUrl = _r2Manager.GetFileUrl(key);
			}

			// Türkçe kayıt oluştur
			model.CreatedAt = DateTime.UtcNow;
			model.UpdatedAt = DateTime.UtcNow;
			model.Culture = "tr";
			_context.RichStaticContents.Add(model);

            // Diğer diller için çeviri ve kayıt (sadece text doluysa)
            // Diğer diller için çeviri ve kayıt (boşsa direkt boş string kaydedilecek)
            var text = model.Text ?? string.Empty;

            string enText = "";
            string ruText = "";
            string roText = "";

            if (!string.IsNullOrWhiteSpace(text))
            {
                enText = await _openAIManager.TranslateFromTurkishAsync(text, "English");
                ruText = await _openAIManager.TranslateFromTurkishAsync(text, "Russian");
                roText = await _openAIManager.TranslateFromTurkishAsync(text, "Romanian");
            }

            // İngilizce kayıt
            var enModel = new RichStaticContent
            {
                CategoryId = model.CategoryId,
                SeasonId = model.SeasonId,
                MediaUrl = model.MediaUrl,
                ProfileImageUrl = model.ProfileImageUrl,
                CreatedAt = model.CreatedAt,
                UpdatedAt = model.UpdatedAt,
                Culture = "en",
                Text = enText, // boş olabilir
                EmbedVideoUrl = model.EmbedVideoUrl,
                AltText = model.AltText,
                Published = model.Published
            };
            _context.RichStaticContents.Add(enModel);

            // Rusça kayıt
            var ruModel = new RichStaticContent
            {
                CategoryId = model.CategoryId,
                SeasonId = model.SeasonId,
                MediaUrl = model.MediaUrl,
                ProfileImageUrl = model.ProfileImageUrl,
                CreatedAt = model.CreatedAt,
                UpdatedAt = model.UpdatedAt,
                Culture = "ru",
                Text = ruText,
                EmbedVideoUrl = model.EmbedVideoUrl,
                AltText = model.AltText,
                Published = model.Published
            };
            _context.RichStaticContents.Add(ruModel);

            // Romence kayıt
            var roModel = new RichStaticContent
            {
                CategoryId = model.CategoryId,
                SeasonId = model.SeasonId,
                MediaUrl = model.MediaUrl,
                ProfileImageUrl = model.ProfileImageUrl,
                CreatedAt = model.CreatedAt,
                UpdatedAt = model.UpdatedAt,
                Culture = "ro",
                Text = roText,
                EmbedVideoUrl = model.EmbedVideoUrl,
                AltText = model.AltText,
                Published = model.Published
            };
            _context.RichStaticContents.Add(roModel);



            await _context.SaveChangesAsync();
			TempData["SuccessMessage"] = "Rich static içerik kaydedildi.";
			return RedirectToAction(nameof(RichStatic));
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteRichStatic(int id)
		{
			var entity = await _context.RichStaticContents.FirstOrDefaultAsync(x => x.Id == id);
			if (entity != null)
			{
				_context.RichStaticContents.Remove(entity);
				await _context.SaveChangesAsync();
				TempData["SuccessMessage"] = "Kayıt silindi.";
			}
			return RedirectToAction(nameof(RichStatic));
		}
	}
}