using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BussinessCupApi.Data;
using BussinessCupApi.Models;
using BussinessCupApi.Models.Dtos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BussinessCupApi.Attributes;

namespace BussinessCupApi.Controllers.Api
{
    [ApiKeyAuth]
    [Route("api/[controller]")]
    [ApiController]
    public class MatchNewsController : ControllerBase
    {
        private static class CacheKeys
        {
            private const string Prefix = "match_news_";
            public static string ActualNews => $"{Prefix}actual";
        }

        private readonly ApplicationDbContext _context;
        private readonly ILogger<MatchNewsController> _logger;
        private readonly IMemoryCache _cache;
        private const int CACHE_DURATION_MINUTES = 5; // Haberler için biraz daha uzun cache süresi

        public MatchNewsController(
            ApplicationDbContext context,
            ILogger<MatchNewsController> logger,
            IMemoryCache cache)
        {
            _context = context;
            _logger = logger;
            _cache = cache;
        }

        [HttpGet("actual")]
        [ProducesResponseType(typeof(List<MatchNewsDto>), 200)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<MatchNewsDto>>> GetActualMatchNews()
        {
            string cacheKey = CacheKeys.ActualNews;

            try
            {
                // Önbellekten almayı dene
                if (_cache.TryGetValue(cacheKey, out ActionResult<List<MatchNewsDto>> cachedResult))
                {
                    _logger.LogInformation("Cache hit - Returning actual match news");
                    return cachedResult;
                }

                _logger.LogInformation("Cache miss - Getting actual match news");

                var matchNews = await _context.MatchNews
                    .Where(mn => mn.Published) // Sadece yayında olan haberleri getir
                    .Include(mn => mn.Photos) // İlişkili fotoğrafları da yükle
                    .OrderByDescending(mn => mn.CreatedDate) // Tarihe göre sırala (en yeni en üstte)
                    .Select(mn => new MatchNewsDto
                    {
                        Id = mn.Id,
                        Title = mn.Title,
                        Subtitle = mn.Subtitle,
                        MatchNewsMainPhoto = mn.MatchNewsMainPhoto,
                        DetailsTitle = mn.DetailsTitle,
                        Details = mn.Details,
                        CreatedDate = mn.CreatedDate,
                        Photos = mn.Photos.Select(p => new MatchNewsPhotoDto
                        {
                            Id = p.Id,
                            PhotoUrl = p.PhotoUrl
                        }).ToList()
                    })
                    .ToListAsync();

                var result = Ok(matchNews);

                // Önbelleğe ekle
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));
                _cache.Set(cacheKey, result, cacheEntryOptions);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Maç haberleri yüklenirken hata oluştu");
                return StatusCode(500, new { error = "Veriler yüklenirken bir sunucu hatası oluştu." });
            }
        }
    }
}