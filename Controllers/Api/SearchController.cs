using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using BussinessCupApi.Attributes;
using BussinessCupApi.Data;
using BussinessCupApi.Models.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BussinessCupApi.Controllers.Api
{
    [ApiKeyAuth]
    [Route("api/[controller]")]
    [ApiController]
    public class SearchController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private const int CACHE_DURATION_MINUTES = 100;

        public SearchController(ApplicationDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        [HttpGet]
        public async Task<ActionResult<List<SearchResultDto>>> Search([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 3)
                return BadRequest(new { error = "Arama terimi en az 3 karakter olmalıdır." });

            query = query.Trim().ToLower();
            string cacheKey = $"search_{query}";

            if (_cache.TryGetValue(cacheKey, out List<SearchResultDto> cachedResult))
            {
                return Ok(cachedResult);
            }

            // Oyuncular
            var players = await _context.Players
                .Where(p => (p.FirstName + " " + p.LastName).ToLower().Contains(query))
                .Select(p => new SearchResultDto
                {
                    Id = p.PlayerID,
                    Name = p.FirstName + " " + p.LastName,
                    LogoUrl = p.Icon,
                    Type = "player"
                })
                .ToListAsync();

            // Takımlar
            var teams = await _context.Teams
                .Where(t => t.Name.ToLower().Contains(query))
                .Select(t => new SearchResultDto
                {
                    Id = t.TeamID,
                    Name = t.Name,
                    LogoUrl = t.LogoUrl,
                    Type = "team"
                })
                .ToListAsync();

            // Ligler
            var leagues = await _context.Leagues
                .Where(l => l.Name.ToLower().Contains(query))
                .Select(l => new SearchResultDto
                {
                    Id = l.LeagueID,
                    Name = l.Name,
                    LogoUrl = l.LogoPath,
                    Type = "league"
                })
                .ToListAsync();

            var results = players.Concat(teams).Concat(leagues).ToList();

            // Cache’e ekle
            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));
            _cache.Set(cacheKey, results, cacheEntryOptions);

            return Ok(results);
        }
    }
}