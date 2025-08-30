using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using BussinessCupApi.Attributes;
using BussinessCupApi.Data;
using BussinessCupApi.DTOs; // DTO'ları ekleyin
using BussinessCupApi.DTOs.Web;
using BussinessCupApi.Extensions;
using BussinessCupApi.Managers;
using BussinessCupApi.Models;
using System.Collections.Generic;
using System.Threading.Tasks; 

namespace Controllers.Api
{
   // [ApiKeyAuth]
    [Route("web-api/[controller]")]
    [ApiController]
    public class WebMatchController : ControllerBase
    {
        private static class CacheKeys
        {
            private const string Prefix = "city_matches_"; 
        }

        private readonly ApplicationDbContext _context;
        private readonly ILogger<TeamDetailsController> _logger;
        private readonly IMemoryCache _cache;
        private readonly IDistributedCache _distributedCache;
        private const int CACHE_DURATION_MINUTES = 1;
        private readonly WebProviderManager _webProviderManager;
        private readonly LeagueManager _leaguemanager;

        public WebMatchController(
            ApplicationDbContext context,
            ILogger<TeamDetailsController> logger,
            IMemoryCache cache,
            IDistributedCache distributedCache,
            WebProviderManager webProviderManager,
            LeagueManager leagueManager)
        {
            _context = context;
            _logger = logger;
            _cache = cache;
            _distributedCache = distributedCache;
            _webProviderManager = webProviderManager;
            _leaguemanager = leagueManager;
        } 

        // Şehirdeki son 4 maçı getirir
        [HttpGet("last-matches/{cityId}")]
        public async Task<IActionResult> GetLastMatchesByCity(int cityId)
        {
            string cacheKey = $"city_last_matches_{cityId}";
            if (!_cache.TryGetValue(cacheKey, out List<WebMatchDto> matches))
            {
                matches = _context.Matches
                    .Where(m => m.League.CityID == cityId)
                    .OrderByDescending(m => m.MatchDate)
                    .Take(4)
                    .Select(m => new WebMatchDto
                    {
                        MatchID = m.MatchID,
                        LeagueID = m.LeagueID,
                        WeekID = m.WeekID,
                        GroupID = m.GroupID,
                        HomeTeamID = m.HomeTeamID,
                        AwayTeamID = m.AwayTeamID,
                        MatchDate = m.MatchDate,
                        HomeScore = m.HomeScore,
                        AwayScore = m.AwayScore,
                        Status = m.Status.GetDisplayName(),
                        HomeTeam = new WebTeamDto
                        {
                            TeamID = m.HomeTeam.TeamID,
                            Name = m.HomeTeam.Name,
                            CityID = m.HomeTeam.CityID,
                            LogoUrl = m.HomeTeam.LogoUrl,
                            Manager = m.HomeTeam.Manager
                        },
                        AwayTeam = new WebTeamDto
                        {
                            TeamID = m.AwayTeam.TeamID,
                            Name = m.AwayTeam.Name,
                            CityID = m.AwayTeam.CityID,
                            LogoUrl = m.AwayTeam.LogoUrl,
                            Manager = m.AwayTeam.Manager
                        }
                    })
                    .ToList();
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));
                _cache.Set(cacheKey, matches, cacheEntryOptions);
            }
            return Ok(matches);
        }

        // Şehirdeki son haftanın ideal 11'ini getirir
        [HttpGet("last-week-best11/{cityId}")]
        public async Task<IActionResult> GetLastWeekBest11ByCity(int cityId)
        {
            string cacheKey = $"city_last_week_best11_{cityId}";
            if (!_cache.TryGetValue(cacheKey, out List<WebPlayerDto> best11))
            {
                var lastWeek = _context.Weeks
                    .Where(w => w.League.CityID == cityId)
                    .OrderByDescending(w => w.WeekID)
                    .FirstOrDefault();
                if (lastWeek == null)
                    return NotFound("Hafta bulunamadı");

                var weekBestTeam = _context.WeekBestTeams
                    .Where(wbt => wbt.WeekID == lastWeek.WeekID)
                    .FirstOrDefault();
                if (weekBestTeam == null)
                    return NotFound("Haftanın en iyi 11'i bulunamadı");

                best11 = _context.WeekBestTeamPlayers
                    .Where(wbtp => wbtp.WeekBestTeamID == weekBestTeam.WeekBestTeamID)
                    .OrderBy(wbtp => wbtp.OrderNumber)
                    .Select(wbtp => new WebPlayerDto
                    {
                        PlayerID = wbtp.PlayerID,
                        FirstName = wbtp.Player.FirstName,
                        LastName = wbtp.Player.LastName,
                        Position = wbtp.Player.Position,
                        Number = wbtp.Player.Number,
                        DateOfBirth = wbtp.Player.DateOfBirth,
                        Nationality = wbtp.Player.Nationality,
                        Icon = wbtp.Player.Icon
                    })
                    .ToList();

                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));
                _cache.Set(cacheKey, best11, cacheEntryOptions);
            }
            return Ok(best11);
        }

        // Şehirdeki son lig puan durumunu getirir
        [HttpGet("last-league-standings/{cityId}")]
        public async Task<IActionResult> GetLastLeagueStandingsByCity(int cityId)
        {
            string cacheKey = $"city_last_league_standings_{cityId}";
            if (!_cache.TryGetValue(cacheKey, out object result))
            {
                // Şehirdeki aktif ligi bul
                var lastLeague = _context.Leagues
                    .Where(l => l.CityID == cityId)
                    .OrderByDescending(l => l.LeagueID)
                    .FirstOrDefault();

                if (lastLeague == null)
                    return NotFound("Lig bulunamadı");

                // Son sezonu bul
                var lastSeason = _context.Season
                    .Where(s => s.LeagueID==lastLeague.LeagueID)
                    .OrderByDescending(s => s.SeasonID)
                    .FirstOrDefault();

                if (lastSeason == null)
                    return NotFound("Sezon bulunamadı");


                result = await _leaguemanager.GetLeagueStandingsAsync(lastLeague.LeagueID, lastSeason.SeasonID, null, false);
                 

                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));
                _cache.Set(cacheKey, result, cacheEntryOptions);
            }
            return Ok(result);
        }
    }
}
