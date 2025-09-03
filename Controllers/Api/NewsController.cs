using BussinessCupApi.Data;
using BussinessCupApi.Models.Dtos;// NotificationManager için
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using BussinessCupApi.Attributes; // Kullanıcının kimliğini almak için (opsiyonel)

namespace BussinessCupApi.Controllers.Api // Namespace'i kontrol edin
{
    [ApiKeyAuth]
    [Route("api/[controller]")]
    [ApiController]
    public class NewsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FavouritesController> _logger;
        private readonly IMemoryCache _cache;

        public NewsController(ApplicationDbContext context, ILogger<FavouritesController> logger, IMemoryCache cache)
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
    }
}