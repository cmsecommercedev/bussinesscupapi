using BussinessCupApi.Data;
using BussinessCupApi.Models.Dtos;// NotificationManager için
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using BussinessCupApi.Attributes; // Kullanıcının kimliğini almak için (opsiyonel)
using System;
using BussinessCupApi.Models;

namespace BussinessCupApi.Controllers.Api // Namespace'i kontrol edin
{
    [ApiKeyAuth]
    [Route("api/[controller]")]
    [ApiController]
    public class ContextController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ContextController> _logger;
        private readonly IMemoryCache _cache;

        public ContextController(ApplicationDbContext context, ILogger<ContextController> logger, IMemoryCache cache)
        {
            _context = context;
            _logger = logger;
            _cache = cache;
        }

        // GET: /api/news/list?culture=tr
        [HttpGet("list")]
        public async Task<ActionResult<IEnumerable<MatchNewsDto>>> GetMatchNewsList([FromQuery] string culture = "tr")
        {
            // Haberleri içerik ve fotoğrafları ile birlikte çek
            var items = await _context.MatchNews
                .AsNoTracking()
                .Where(m => m.Published)
                .Include(m => m.Photos)
                .Include(m => m.Contents)
                .OrderByDescending(m => m.CreatedDate)
                .Select(m => new MatchNewsDto
                {
                    Id = m.Id,
                    MatchNewsMainPhoto = m.MatchNewsMainPhoto ?? string.Empty,
                    CreatedDate = m.CreatedDate,
                    // Culture'a göre tekil içerik alanları
                    Title = m.Contents.Where(c => c.Culture == culture).Select(c => c.Title).FirstOrDefault(),
                    Subtitle = m.Contents.Where(c => c.Culture == culture).Select(c => c.Subtitle).FirstOrDefault(),
                    DetailsTitle = m.Contents.Where(c => c.Culture == culture).Select(c => c.DetailsTitle).FirstOrDefault(),
                    Details = m.Contents.Where(c => c.Culture == culture).Select(c => c.Details).FirstOrDefault(),
                    // Fotoğraflar
                    Photos = m.Photos
                        .Select(p => new MatchNewsPhotoDto
                        {
                            Id = p.Id,
                            PhotoUrl = p.PhotoUrl
                        })
                        .ToList()
                })
                .ToListAsync();

            return Ok(items);
        }

        // GET: /api/context/static?key=someKey
        [HttpGet("static")]
        public async Task<ActionResult> GetStatic([FromQuery] string? key = null)
        {
            var query = _context.StaticKeyValues.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(key))
            {
                var item = await query
                    .Where(s => s.Key == key)
                    .Select(s => new { s.Key, s.Value, s.UpdatedAt })
                    .FirstOrDefaultAsync();

                if (item == null) return NotFound();
                return Ok(item);
            }

            var items = await query
                .OrderByDescending(s => s.UpdatedAt)
                .Select(s => new { s.Key, s.Value, s.UpdatedAt })
                .ToListAsync();

            return Ok(items);
        }

        // GET: /api/context/photos?category=2024
        [HttpGet("photos")]
        public async Task<ActionResult> GetPhotos([FromQuery] string? category = null)
        {
            var query = _context.PhotoGalleries.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(p => p.Category == category);
            }

            var items = await query
                .OrderByDescending(p => p.UploadedAt)
                .Select(p => new
                {
                    p.Id,
                    p.Category,
                    p.FileName,
                    p.FilePath,
                    p.UploadedAt
                })
                .ToListAsync();

            return Ok(items);
        }

        [HttpGet("stories")]
        public async Task<ActionResult<List<StoryDto>>> GetStories([FromQuery] string? type = null)
        {
            string? normType = type?.Trim().ToLower();
            if (!string.IsNullOrEmpty(normType) && normType != "image" && normType != "video")
            {
                return BadRequest(new { message = "type 'image' veya 'video' olmalıdır." });
            }

            var last24Hours = DateTime.UtcNow.AddHours(-24);

            IQueryable<Story> query = _context.Stories
                .AsNoTracking()
                .Where(s => s.Published && s.UpdatedAt >= last24Hours) // Son 24 saat filtresi
                .Include(s => s.Contents);

            if (!string.IsNullOrEmpty(normType))
            {
                query = query.Where(s => s.Contents.Any(c =>
                    !string.IsNullOrEmpty(c.ContentType) &&
                    c.ContentType.StartsWith(normType)));
            }

            query = query.OrderByDescending(s => s.UpdatedAt);

            var items = await query
                .Select(s => new StoryDto
                {
                    Id = s.Id,
                    Title = s.Title,
                    StoryImage = s.StoryImage,
                    Published = s.Published,
                    UpdatedAt = s.UpdatedAt,
                    Contents = s.Contents
                        .Where(c => string.IsNullOrEmpty(normType) ||
                            (!string.IsNullOrEmpty(c.ContentType) && c.ContentType.StartsWith(normType)))
                        .OrderBy(c => c.DisplayOrder)
                        .Select(c => new StoryContentDto
                        {
                            Id = c.Id,
                            MediaUrl = c.MediaUrl,
                            ContentType = c.ContentType,
                            DisplayOrder = c.DisplayOrder
                        })
                        .ToList(),
                    Type = s.Contents.Any(c => c.ContentType.StartsWith("video"))
                                ? "video"
                                : "image"
                })
                .ToListAsync();

            return Ok(items);
        }


        // GET: /api/context/richstatic?category=flags&culture=tr
        [HttpGet("richstatic")]
        public async Task<ActionResult> GetRichStatic([FromQuery] string? category = null, [FromQuery] string? culture = null, [FromQuery] bool? published = true)
        {
            var query = _context.RichStaticContents.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(x => x.CategoryCode == category);
            }
            if (!string.IsNullOrWhiteSpace(culture))
            {
                query = query.Where(x => x.Culture == culture);
            }
            if (published.HasValue)
            {
                query = query.Where(x => x.Published == published.Value);
            }

            var items = await query
                .OrderByDescending(x => x.UpdatedAt)
                .Select(x => new
                {
                    x.Id,
                    x.CategoryCode,
                    x.Culture,
                    x.MediaUrl,
                    x.ProfileImageUrl,
                    x.EmbedVideoUrl,
                    x.Text,
                    x.AltText,
                    x.Published,
                    x.UpdatedAt
                })
                .ToListAsync();

            return Ok(items);
        }

        [HttpGet("app-settings")]
        public async Task<ActionResult<Settings>> GetAppSettings()
        { 
            try
            { 
                var settings = await _context.Settings
                    .OrderByDescending(s => s.LastUpdated)
                    .FirstOrDefaultAsync();

                if (settings == null)
                {
                    _logger.LogWarning("Ayarlar bulunamadı");
                    return NotFound(new { error = "Ayarlar bulunamadı" });
                }

                return Ok(settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Uygulama ayarları yüklenirken hata oluştu");
                return StatusCode(500, new { error = "Uygulama ayarları yüklenirken bir hata oluştu" });
            }
        }
    }
}