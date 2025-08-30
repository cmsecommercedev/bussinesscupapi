// Controllers/LeagueRulesController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.Elfie.Serialization;
using Microsoft.EntityFrameworkCore;
using BussinessCupApi.Data;
using BussinessCupApi.Models;
using System.Linq;
using System.Threading.Tasks;

namespace BussinessCupApi.Controllers
{
    [Authorize(Roles = "Admin,CityAdmin")]
    public class LeagueRulesController : Controller
    {
        private readonly ApplicationDbContext _context;
        public LeagueRulesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ... existing code ...
        public async Task<IActionResult> Index(int leagueId, int seasonId)
        {
            var league = await _context.Leagues.FindAsync(leagueId);
            var season = await _context.Season.FindAsync(seasonId);
            if (league == null || season == null) return NotFound();

            var weekIds = await _context.Set<Week>()
                 .Where(w => w.LeagueID == leagueId && w.SeasonID == seasonId)
                 .Select(w => w.WeekID)
                 .ToListAsync();

            // O haftalardaki maçlardan takımları bul
            var homeTeamIds = await _context.Matches
                .Where(m => weekIds.Contains(m.WeekID))
                .Select(m => m.HomeTeamID)
                .ToListAsync();

            var awayTeamIds = await _context.Matches
                .Where(m => weekIds.Contains(m.WeekID))
                .Select(m => m.AwayTeamID)
                .ToListAsync();

            var allTeamIds = homeTeamIds.Concat(awayTeamIds).Distinct().ToList();
             

            var teams = await _context.Set<Team>()
                .Where(t => allTeamIds.Contains(t.TeamID))
                .ToListAsync();

            ViewBag.Teams = teams;
            ViewBag.LeagueId = leagueId;
            ViewBag.SeasonId = seasonId; 
            ViewBag.LeagueName = league.Name; // Lig adı
            ViewBag.SeasonName = season.Name; // Sezon adı (eğer Name yoksa uygun property'yi kullanın)

            return View();
        }
        // ... existing code ...

        public async Task<IActionResult> GetRules(int leagueId, int seasonId)
        {
            var rules = await (from lr in _context.LeagueRules
                               join t in _context.Teams on lr.TeamId equals t.TeamID
                               where lr.LeagueId == leagueId && lr.SeasonId == seasonId
                               select new
                               {
                                   lr.Id,
                                   lr.LeagueId,
                                   lr.SeasonId,
                                   lr.TeamId,
                                   t.Name,
                                   lr.RuleType,
                                   lr.Point,
                                   lr.Description,
                                   lr.CreatedAt
                               }).ToListAsync();

            return Json(rules);
        }

        [HttpPost]
        public async Task<IActionResult> AddRule([FromBody] LeagueRule rule)
        {
            if (!ModelState.IsValid || rule == null)
                return BadRequest();

            _context.LeagueRules.Add(rule);
            await _context.SaveChangesAsync();
            return Json(rule);
        }
        public class DeleteRuleRequest
        {
            public int Id { get; set; }
        }
        // Kural siler (JS ile tetiklenir)
        [HttpPost]
        public async Task<IActionResult> DeleteRule([FromBody] DeleteRuleRequest req)
        {
            var rule = await _context.LeagueRules.FindAsync(req.Id);
            if (rule == null) return NotFound();
            _context.LeagueRules.Remove(rule);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }
    }
}