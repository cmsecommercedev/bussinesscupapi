using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using BussinessCupApi.Attributes;
using BussinessCupApi.Data;
using BussinessCupApi.DTOs; // DTO'ları ekleyin
using BussinessCupApi.DTOs.Web;
using BussinessCupApi.Managers;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Controllers.Api
{
    [ApiKeyAuth]
    [Route("web-api/[controller]")]
    [ApiController]
    public class WebCityController : ControllerBase
    {
        private static class CacheKeys
        {
            private const string Prefix = "city_details_";
            public static string Leagues => $"{Prefix}city_details";
        }

        private readonly ApplicationDbContext _context;
        private readonly ILogger<TeamDetailsController> _logger;
        private readonly IMemoryCache _cache;
        private readonly IDistributedCache _distributedCache;
        private const int CACHE_DURATION_MINUTES = 1;
        private readonly WebProviderManager _webProviderManager;

        public WebCityController(
            ApplicationDbContext context,
            ILogger<TeamDetailsController> logger,
            IMemoryCache cache,
            IDistributedCache distributedCache,
            WebProviderManager webProviderManager)
        {
            _context = context;
            _logger = logger;
            _cache = cache;
            _distributedCache = distributedCache;
            _webProviderManager = webProviderManager;
        }

        // 1. Tüm şehirleri getir (DTO)
        [HttpGet("web/city/all")]
        public async Task<ActionResult<List<WebCityDto>>> GetAllCities()
        {
            var cities = await _webProviderManager.GetAllCitiesAsync();
            return Ok(cities);
        }

        // 2. Bir şehrin haberlerini getir (DTO)
        [HttpGet("web/{cityId}/news")]
        public async Task<ActionResult<List<WebMatchNewsDto>>> GetCityNews(int cityId, [FromQuery] bool onlyPublished = true)
        {
            var news = await _webProviderManager.GetCityNewsAsync(cityId, onlyPublished);
            return Ok(news);
        }

        // 3. Bir şehrin takımlarını getir (DTO)
        [HttpGet("web/{cityId}/teams")]
        public async Task<ActionResult<List<WebTeamDto>>> GetCityTeams(int cityId)
        {
            var teams = await _webProviderManager.GetCityTeamsAsync(cityId);
            return Ok(teams);
        }
        [HttpGet("web/team/{teamId}/roster")]
        public async Task<ActionResult<List<WebPlayerDto>>> GetTeamRoster(int teamId)
        {
            var roster = await _webProviderManager.GetTeamRosterAsync(teamId);
            return Ok(roster);
        }
    }
}