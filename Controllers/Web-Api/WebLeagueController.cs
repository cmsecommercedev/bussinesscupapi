using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using BussinessCupApi.Attributes;
using BussinessCupApi.Data;
using BussinessCupApi.Managers;
using BussinessCupApi.Models;
using BussinessCupApi.Models.UserPlayerTypes;
using BussinessCupApi.DTOs; // DTO'ları ekleyin
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BussinessCupApi.DTOs.Web;

namespace Controllers.Api
{
    [ApiKeyAuth]
    [Route("web-api/[controller]")]
    [ApiController]
    public class WebLeagueController : ControllerBase
    {
        // Cache key'lerini sabit olarak tanımlayalım
        private static class CacheKeys
        {
            private const string Prefix = "league_details_";
            public static string Leagues => $"{Prefix}league_details_";
        }

        private readonly ApplicationDbContext _context;
        private readonly ILogger<TeamDetailsController> _logger;
        private readonly IMemoryCache _cache;
        private readonly IDistributedCache _distributedCache;
        private readonly WebProviderManager _webProviderManager;
        private const int CACHE_DURATION_MINUTES = 1;

        public WebLeagueController(
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


        // 4. Şehre göre ligleri getir (DTO)
        [HttpGet("web/{cityId}/leagues")]
        public async Task<ActionResult<List<WebLeagueDto>>> GetLeaguesByCity(int cityId)
        {
            var leagues = await _webProviderManager.GetLeaguesByCityAsync(cityId);
            return Ok(leagues);
        }

        // 5. Lige göre haftaları getir (DTO)
        [HttpGet("web/league/{leagueId}/weeks")]
        public async Task<ActionResult<List<WebWeekDto>>> GetWeeksByLeague(int leagueId)
        {
            var weeks = await _webProviderManager.GetWeeksByLeagueAsync(leagueId);
            return Ok(weeks);
        }

        // 6. Lig ve haftaya göre haftanın maçlarını getir (DTO)
        [HttpGet("web/league/{leagueId}/week/{weekId}/matches")]
        public async Task<ActionResult<List<WebMatchDto>>> GetMatchesByLeagueAndWeek(int leagueId, int weekId)
        {
            var matches = await _webProviderManager.GetMatchesByLeagueAndWeekAsync(leagueId, weekId);
            return Ok(matches);
        }

        [HttpGet("web/news/main")]
        public async Task<ActionResult<List<WebMatchNewsDto>>> GetMainNews([FromQuery] bool onlyPublished = true)
        {
            var news = await _webProviderManager.GetMainNewsAsync(onlyPublished);
            return Ok(news);
        }

        // Takım ID'si ile takım detayını getir (DTO)
        [HttpGet("web/team/{teamId}")]
        public async Task<ActionResult<WebTeamDto>> GetTeamById(int teamId)
        {
            var team = await _webProviderManager.GetTeamByIdAsync(teamId); 
            return Ok(team);
        }

        // Haber ID'si ile haber detayını getir (DTO)
        [HttpGet("web/news/{id}")]
        public async Task<ActionResult<WebMatchNewsDto>> GetNewsById(int id)
        {
            var news = await _webProviderManager.GetNewsByIdAsync(id); 
            return Ok(news);
        }

        // Lig için günümüze en yakın haftanın maçlarını, lig ve hafta adı ile getir (DTO)
        [HttpGet("web/league/{leagueId}/actual-week-matches")]
        public async Task<ActionResult<WebActualWeekMatchesDto>> GetActualWeekMatches(int leagueId)
        {
            var result = await _webProviderManager.GetActualWeekMatchesAsync(leagueId); 
            return Ok(result);
        }

        // Takım için günümüze en yakın haftanın maçlarını getir (lig ve hafta adıyla)
        [HttpGet("web/team/{teamId}/all-team-matches")]
        public async Task<ActionResult<WebActualWeekMatchesDto>> GetActualWeekMatchesByTeam(int teamId)
        {
            var result = await _webProviderManager.GetAllMatchesByTeamAsync(teamId); 
            return Ok(result);
        }

        // Maç ID'si ile maç detayını getir (goller, kartlar, dizilişler, kadro)
        [HttpGet("web/match/{matchId}/details")]
        public async Task<ActionResult<WebMatchDetailDto>> GetMatchDetailsById(int matchId)
        {
            var matchDetail = await _webProviderManager.GetMatchDetailsByIdAsync(matchId);
            if (matchDetail == null)
                return NotFound();
            return Ok(matchDetail);
        }
    }
}
