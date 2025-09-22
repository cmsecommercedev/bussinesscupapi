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

namespace BussinessCupApi.Controllers
{
	[Authorize(Roles = "Admin")]
	public class StaticContentController : Controller
	{
		private readonly ApplicationDbContext _context;
		private readonly CloudflareR2Manager _r2Manager;

		public StaticContentController(ApplicationDbContext context, CloudflareR2Manager r2Manager)
		{
			_context = context;
			_r2Manager = r2Manager;
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
				.AsNoTracking()
				.OrderByDescending(x => x.UpdatedAt)
				.ToListAsync();

			ViewBag.Items = items;
			return View(new RichStaticContent());
		}

				[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> RichStatic(RichStaticContent model)
		{ 
			if (!ModelState.IsValid)
			{
				ViewBag.Items = await _context.RichStaticContents
					.AsNoTracking()
					.OrderByDescending(x => x.UpdatedAt)
					.ToListAsync();
				return View(model);
			}

			// Görsel yüklendiyse R2'ye yükle
			if (model.ImageFile != null && model.ImageFile.Length > 0)
			{
				var ext = Path.GetExtension(model.ImageFile.FileName);
				var safeCat = string.IsNullOrWhiteSpace(model.CategoryCode) ? "misc" : model.CategoryCode.Trim().ToLower();
				var key = $"richstatic/{safeCat}/{Guid.NewGuid()}{ext}";
				using var stream = model.ImageFile.OpenReadStream();
				await _r2Manager.UploadFileAsync(key, stream, model.ImageFile.ContentType);
				model.ImageUrl = _r2Manager.GetFileUrl(key);
			}

			// Her zaman yeni kayıt oluştur
			model.CreatedAt = DateTime.UtcNow;
			model.UpdatedAt = DateTime.UtcNow;
			_context.RichStaticContents.Add(model);

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