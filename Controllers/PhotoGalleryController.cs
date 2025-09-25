using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BussinessCupApi.Data;
using BussinessCupApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BussinessCupApi.Controllers
{
	[Authorize(Roles = "Admin")]
	public class PhotoGalleryController : Controller
	{
		private readonly ApplicationDbContext _db;
		private readonly IWebHostEnvironment _env;
		private readonly CloudflareR2Manager _r2Manager;

		private static readonly string[] AllowedYears = new[]
		{
			"2019","2020","2021","2022","2023","2024","2025"
		};

		public PhotoGalleryController(ApplicationDbContext db, IWebHostEnvironment env, CloudflareR2Manager r2Manager)
		{
			_db = db;
			_env = env;
			_r2Manager = r2Manager;
		}

		// Ana Sayfa (Kartlar için)
		[AllowAnonymous]
		public IActionResult Index()
		{
			ViewBag.AllowedYears = AllowedYears;
			return View();
		}

		// Seçilen kategoriye ait fotoları döner
		[HttpGet]
		public async Task<IActionResult> GetPhotos(string year)
		{
			if (string.IsNullOrWhiteSpace(year) || !AllowedYears.Contains(year))
			{
				return BadRequest("Geçersiz kategori.");
			}

			var photos = await _db.PhotoGalleries
				.Where(p => p.Category == year)
				.OrderByDescending(p => p.UploadedAt)
				.Select(p => new
				{
					url = p.FilePath,
					path = p.FilePath // silme için gönderiyoruz
				})
				.ToListAsync();

			return Json(photos);
		}

		// Fotoğraf ekleme (AJAX)
		[HttpPost]
		public async Task<IActionResult> Create(string year, List<IFormFile> photos)
		{
			if (string.IsNullOrWhiteSpace(year) || !AllowedYears.Contains(year))
			{
				return BadRequest(new { success = false, message = "Yıl geçersiz." });
			}

			if (photos == null || photos.Count == 0)
			{
				return BadRequest(new { success = false, message = "En az bir fotoğraf yükleyin." });
			}

			var uploadedFiles = new List<string>();

			foreach (var photo in photos)
			{
				if (photo.Length == 0) continue;

				var safeFileName = Path.GetFileName(photo.FileName);
				var uniqueName = $"{Guid.NewGuid():N}{Path.GetExtension(safeFileName)}";
				var key = $"photogallery/{year}/{uniqueName}";

				using (var stream = photo.OpenReadStream())
				{
					await _r2Manager.UploadFileAsync(key, stream, photo.ContentType);
				}

				var publicUrl = _r2Manager.GetFileUrl(key);

				_db.PhotoGalleries.Add(new PhotoGallery
				{
					Category = year,
					FileName = safeFileName,
					FilePath = publicUrl,
					UploadedAt = DateTime.UtcNow
				});

				uploadedFiles.Add(publicUrl);
			}

			await _db.SaveChangesAsync();

			return Json(new { success = true, urls = uploadedFiles });
		}


		// Fotoğraf silme (AJAX)
		[HttpPost]
		public async Task<IActionResult> Delete(string path, string year)
		{
			if (string.IsNullOrWhiteSpace(year) || !AllowedYears.Contains(year))
			{
				return BadRequest(new { success = false, message = "Geçersiz kategori." });
			}

			var photo = await _db.PhotoGalleries.FirstOrDefaultAsync(p => p.FilePath == path && p.Category == year);
			if (photo == null)
			{
				return NotFound(new { success = false, message = "Fotoğraf bulunamadı." });
			}

			try
			{
				// Cloudflare R2'den de sil
				var key = $"photogallery/{year}/{photo.FileName}";
				await _r2Manager.DeleteFileAsync(key);

				_db.PhotoGalleries.Remove(photo);
				await _db.SaveChangesAsync();

				return Json(new { success = true });
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { success = false, message = "Silme sırasında hata oluştu.", error = ex.Message });
			}
		}
	}
}
