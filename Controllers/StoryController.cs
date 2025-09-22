using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BussinessCupApi.Data;
using BussinessCupApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BussinessCupApi.Controllers
{
	[Authorize(Roles = "Admin")]
	public class StoryController : Controller
	{
		private readonly ApplicationDbContext _context;
		private readonly CloudflareR2Manager _r2Manager;

		public StoryController(ApplicationDbContext context, CloudflareR2Manager r2Manager)
		{
			_context = context;
			_r2Manager = r2Manager;
		}

		[HttpGet]
		public async Task<IActionResult> Index()
		{
			var stories = await _context.Stories
				.AsNoTracking()
				.Include(s => s.Contents)
				.OrderByDescending(s => s.UpdatedAt)
				.ToListAsync();
			return View(stories);
		}

		[HttpGet]
		public IActionResult Create()
		{
			return View(new Story());
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create([FromForm] string title, [FromForm] bool published, List<IFormFile> files)
		{
			if (string.IsNullOrWhiteSpace(title))
			{
				ModelState.AddModelError("Title", "Başlık zorunludur.");
			}

			if (!ModelState.IsValid)
			{
				return View(new Story { Title = title ?? string.Empty, Published = published });
			}

			var story = new Story
			{
				Title = title.Trim(),
				Published = published,
				CreatedAt = DateTime.UtcNow,
				UpdatedAt = DateTime.UtcNow
			};

			_context.Stories.Add(story);
			await _context.SaveChangesAsync();

			int order = 0;
			if (files != null)
			{
				foreach (var file in files.Where(f => f != null && f.Length > 0))
				{
					var ext = Path.GetExtension(file.FileName);
					var key = $"stories/{story.Id}/{Guid.NewGuid()}{ext}";
					using var stream = file.OpenReadStream();
					await _r2Manager.UploadFileAsync(key, stream, file.ContentType);
					var url = _r2Manager.GetFileUrl(key);

					_context.StoryContents.Add(new StoryContent
					{
						StoryId = story.Id,
						MediaUrl = url,
						ContentType = file.ContentType,
						DisplayOrder = order++,
						CreatedAt = DateTime.UtcNow
					});
				}
			}

			await _context.SaveChangesAsync();
			TempData["SuccessMessage"] = "Story oluşturuldu.";
			return RedirectToAction(nameof(Index));
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Delete(int id)
		{
			var story = await _context.Stories.Include(s => s.Contents).FirstOrDefaultAsync(s => s.Id == id);
			if (story != null)
			{
				_context.StoryContents.RemoveRange(story.Contents);
				_context.Stories.Remove(story);
				await _context.SaveChangesAsync();
				TempData["SuccessMessage"] = "Story silindi.";
			}
			return RedirectToAction(nameof(Index));
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> AddFiles(int id, List<IFormFile> files)
		{
			if (files == null || files.Count == 0)
			{
				TempData["SuccessMessage"] = "Eklenecek dosya seçilmedi.";
				return RedirectToAction(nameof(Index));
			}

			var story = await _context.Stories
				.Include(s => s.Contents)
				.FirstOrDefaultAsync(s => s.Id == id);

			if (story == null)
			{
				return NotFound();
			}

			int order = (story.Contents?.OrderByDescending(c => c.DisplayOrder).FirstOrDefault()?.DisplayOrder ?? -1) + 1;

			foreach (var file in files.Where(f => f != null && f.Length > 0))
			{
				var ext = Path.GetExtension(file.FileName);
				var key = $"stories/{story.Id}/{Guid.NewGuid()}{ext}";
				using var stream = file.OpenReadStream();
				await _r2Manager.UploadFileAsync(key, stream, file.ContentType);
				var url = _r2Manager.GetFileUrl(key);

				_context.StoryContents.Add(new StoryContent
				{
					StoryId = story.Id,
					MediaUrl = url,
					ContentType = file.ContentType,
					DisplayOrder = order++,
					CreatedAt = DateTime.UtcNow
				});
			}

			story.UpdatedAt = DateTime.UtcNow;
			await _context.SaveChangesAsync();
			TempData["SuccessMessage"] = "Dosyalar eklendi.";
			return RedirectToAction(nameof(Index));
		}
	}
}


