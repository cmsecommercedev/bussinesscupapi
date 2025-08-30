using BussinessCupApi.Data;
using BussinessCupApi.Models;
using BussinessCupApi.Models.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Caching.Distributed; // Opsiyonel
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using BussinessCupApi.Attributes;

namespace BussinessCupApi.Controllers.Api
{
    [ApiKeyAuth]
    [Route("api/[controller]")]
    [ApiController] 
    public class AdvertiseController : ControllerBase
    {
        // Cache key'lerini sabit olarak tanımlayalım
        private static class CacheKeys
        {
            public static string Advertisements => "advertisements_active";
        }

        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdvertiseController> _logger;
        private readonly IMemoryCache _cache;
        // private readonly IDistributedCache _distributedCache; // Eğer kullanıyorsanız
        private const int CACHE_DURATION_MINUTES = 100;

        public AdvertiseController(
            ApplicationDbContext context,
            ILogger<AdvertiseController> logger,
            IMemoryCache cache
            /* IDistributedCache distributedCache */)
        {
            _context = context;
            _logger = logger;
            _cache = cache;
            // _distributedCache = distributedCache;
        }

        // Aktif Reklamları Getir
        [HttpGet] // api/Advertise
        [ProducesResponseType(typeof(List<AdvertiseDto>), 200)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<AdvertiseDto>>> GetActiveAdvertisements()
        {
            string cacheKey = CacheKeys.Advertisements;

            try
            {
                if (_cache.TryGetValue(cacheKey, out ActionResult<List<AdvertiseDto>> cachedResult))
                {
                    _logger.LogInformation("Cache hit - Returning active advertisements");
                    return cachedResult;
                }

                _logger.LogInformation("Cache miss - Getting active advertisements from database");

                var advertisements = await _context.Advertisements // DbSet adının 'Advertises' olduğunu varsayıyoruz (modele göre)
                    .Where(ad => ad.IsActive) // Sadece aktif olanları al
                    .OrderByDescending(ad => ad.UploadDate) // En yeni yüklenene göre sırala (veya Id)
                    .Select(ad => new AdvertiseDto
                    {
                        Id = ad.Id,
                        Name = ad.Name,
                        LinkUrl = ad.LinkUrl,
                        IsActive = ad.IsActive,
                        AltText = ad.AltText,
                        Category = ad.Category,
                        ImageDataUrl = ad.ImagePath,
                        CityId = ad.CityId,
                        CityName = ad.City.Name
                        // UploadDate = ad.UploadDate // İsterseniz tarihi de ekleyin
                    })
                    .ToListAsync();

                var result = Ok(advertisements);

                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));
                _cache.Set(cacheKey, result, cacheEntryOptions);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Aktif reklamlar yüklenirken hata oluştu.");
                return StatusCode(500, new { error = "Reklamlar yüklenirken bir sunucu hatası oluştu." });
            }
        }
    }
}