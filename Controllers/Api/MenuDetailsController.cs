using BussinessCupApi.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;
using BussinessCupApi.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using BussinessCupApi.Attributes;
using BussinessCupApi.Models;

namespace Controllers.Api
{
    //[ApiKeyAuth]
    [Route("api/[controller]")]
    [ApiController]
    public class MenuDetailsController : ControllerBase
    {
        // Cache key'lerini sabit olarak tanımlayalım
        private static class CacheKeys
        {
            private const string Prefix = "league_details_";
            public static string Leagues => $"{Prefix}leagues";
            public static string LeagueMatches(int leagueId) => $"{Prefix}league_{leagueId}_matches";
            public static string MatchSquads(int matchId) => $"{Prefix}match_{matchId}_squads";
            public static string TeamPlayers(int teamId) => $"{Prefix}team_{teamId}_players";
            // ... diğer cache key'leri
        }

        private readonly ApplicationDbContext _context;
        private readonly ILogger<MenuDetailsController> _logger;
        private readonly IMemoryCache _cache;
        private readonly IDistributedCache _distributedCache;
        private const int CACHE_DURATION_MINUTES = 100;

        public MenuDetailsController(
            ApplicationDbContext context,
            ILogger<MenuDetailsController> logger,
            IMemoryCache cache,
            IDistributedCache distributedCache)
        {
            _context = context;
            _logger = logger;
            _cache = cache;
            _distributedCache = distributedCache;
        }

        [HttpGet("all-leagues/{cityId}")]
        public async Task<IActionResult> GetAllLeagues(int cityId)
        {
            try
            {
                var cacheKey = $"{CacheKeys.Leagues}_all_{cityId}";
                if (_cache.TryGetValue(cacheKey, out List<MenuDetailsLeagueDto> leagues))
                {
                    return Ok(leagues);
                }

                var allLeagues = await _context.Leagues
                    .Where(x => x.CityID == cityId)
                    .Select(l => new MenuDetailsLeagueDto
                    {
                        LeagueId = l.LeagueID,
                        LeagueName = l.Name,
                        LeagueIcon = l.LogoPath
                    })
                    .ToListAsync();

                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));

                _cache.Set(cacheKey, allLeagues, cacheOptions);

                return Ok(allLeagues);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ligler getirilirken hata oluştu");
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("all-teams/{cityId}")]
        public async Task<IActionResult> GetAllTeams(int cityId, [FromQuery] string? macId = null)
        {
            try
            {
                List<int> favouriteTeamIds = new List<int>();
                if (!string.IsNullOrWhiteSpace(macId))
                { 
                    favouriteTeamIds = await _context.FavouriteTeams
                        .Where(f => f.MacID == macId)
                        .Select(f => f.TeamID)
                        .ToListAsync();
                }

                var allTeams = await _context.Teams
                    .Where(x => x.CityID == cityId)
                    .OrderBy(t => t.Name)
                    .Select(t => new MenuDetailsTeamDto
                    {
                        TeamId = t.TeamID,
                        TeamName = t.Name,
                        TeamIcon = t.LogoUrl,
                        Manager = t.Manager,
                        TeamIsFree=t.TeamIsFree,
                        IsFavourite = favouriteTeamIds.Contains(t.TeamID)
                    })
                    .ToListAsync();

                return Ok(allTeams);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Takımlar getirilirken hata oluştu");
                return StatusCode(500, ex.Message);
            }
        }
    }
}
