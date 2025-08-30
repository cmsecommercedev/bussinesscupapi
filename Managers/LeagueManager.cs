using Microsoft.EntityFrameworkCore;
using BussinessCupApi.Data;
using BussinessCupApi.Models;
using BussinessCupApi.ViewModels;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic; // List<T> için eklendi

namespace BussinessCupApi.Managers
{
    public class LeagueManager
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<LeagueManager> _logger;

        public LeagueManager(ApplicationDbContext context, ILogger<LeagueManager> logger)
        {
            _context = context;
            _logger = logger;
        }

        public class LeagueStandingsResult
        {
            public LeagueStandingsViewModel ViewModel { get; set; }
            public string ErrorMessage { get; set; }
            public bool IsAjaxRequest { get; set; } // AJAX durumunu iletmek için
            public bool LeagueNotFound { get; set; } // Lig bulunamadı durumunu iletmek için
        }


        public async Task<LeagueStandingsResult> GetLeagueStandingsAsync(int leagueId, int seasonId, int? groupId, bool isAjaxRequest)
        {
            try
            {
                var leagueGroups = await _context.Group
                    .Where(g => g.LeagueID == leagueId && g.SeasonID == seasonId)
                    .OrderBy(g => g.GroupName)
                    .Select(g => new { g.GroupID, g.GroupName })
                    .ToListAsync();

                int? effectiveGroupId = groupId;
                if (!effectiveGroupId.HasValue && leagueGroups.Any())
                {
                    effectiveGroupId = leagueGroups.First().GroupID;
                }

                var leagueData = await _context.Leagues
                    .Where(l => l.LeagueID == leagueId)
                    .Select(l => new
                    {
                        l.LeagueID,
                        l.Name,
                        Weeks = l.Weeks
                            .Where(w => w.SeasonID == seasonId && w.WeekStatus == "League")
                            .Select(w => new
                            {
                                w.WeekID,
                                w.SeasonID,
                                Matches = w.Matches
                                    .Where(m => m.IsPlayed &&
                                                (effectiveGroupId.HasValue ? m.GroupID == effectiveGroupId.Value : m.GroupID == null))
                                    .Select(m => new
                                    {
                                        m.MatchID,
                                        m.HomeTeamID,
                                        m.AwayTeamID,
                                        m.HomeScore,
                                        m.AwayScore,
                                        m.MatchDate,
                                        m.GroupID,
                                        HomeTeam = new { m.HomeTeam.TeamID, m.HomeTeam.Name },
                                        AwayTeam = new { m.AwayTeam.TeamID, m.AwayTeam.Name }
                                    })
                                    .ToList()
                            })
                            .ToList()
                    })
                    .FirstOrDefaultAsync();

                if (leagueData == null)
                {
                    return new LeagueStandingsResult { LeagueNotFound = true, IsAjaxRequest = isAjaxRequest, ErrorMessage = "Lig bulunamadı." };
                }

                var matches = leagueData.Weeks.SelectMany(w => w.Matches).ToList();
                var teamIds = matches.SelectMany(m => new[] { m.HomeTeamID, m.AwayTeamID }).Distinct().ToList();
                var teams = await _context.Teams
                    .Where(t => teamIds.Contains(t.TeamID))
                    .Select(t => new { t.TeamID, t.Name,t.LogoUrl })
                    .ToDictionaryAsync(t => t.TeamID, t => t);

                // 1. LeagueRule verilerini çek
                var leagueRules = await _context.Set<LeagueRule>()
                    .Where(r => r.LeagueId == leagueId && r.SeasonId == seasonId && teamIds.Contains(r.TeamId))
                    .ToListAsync();

                var currentGroupName = effectiveGroupId.HasValue
                    ? leagueGroups.FirstOrDefault(g => g.GroupID == effectiveGroupId.Value)?.GroupName
                    : null;

                string seasonName = "";
                if (!isAjaxRequest) // Sadece tam sayfa yüklemesinde sezon adını çek
                {
                    seasonName = await _context.Season
                       .Where(s => s.SeasonID == seasonId)
                       .Select(s => s.Name)
                       .FirstOrDefaultAsync() ?? "Bilinmeyen Sezon";
                }

                // 2. Standings hesaplamasında ceza puanını ve açıklamasını ekle
                var standings = teamIds.Select(teamId =>
                {
                    var teamMatches = matches.Where(m => m.HomeTeamID == teamId || m.AwayTeamID == teamId);
                    var team = teams.GetValueOrDefault(teamId);
                    if (team == null) return null;

                    // Takımın ceza kuralı var mı?
                    var teamRules = leagueRules.Where(r => r.TeamId == teamId).ToList();
                    var penaltyPoints = teamRules
                        .Where(r => r.RuleType == RuleType.PointDeduction && r.Point.HasValue)
                        .Sum(r => r.Point.Value);

                    // Diğer ceza açıklamaları
                    var penaltyDescriptions = string.Join(" | ", teamRules
                        .Where(r => r.RuleType != RuleType.PointDeduction && !string.IsNullOrEmpty(r.Description))
                        .Select(r => r.Description));

                    // Normal puan hesaplaması
                    int won = teamMatches.Count(m =>
                        (m.HomeTeamID == teamId && m.HomeScore.HasValue && m.AwayScore.HasValue && m.HomeScore > m.AwayScore) ||
                        (m.AwayTeamID == teamId && m.HomeScore.HasValue && m.AwayScore.HasValue && m.AwayScore > m.HomeScore));
                    int drawn = teamMatches.Count(m => m.HomeScore.HasValue && m.AwayScore.HasValue && m.HomeScore == m.AwayScore);

                    int points = won * 3 + drawn * 1 - penaltyPoints; // Ceza puanını düş

                    return new TeamStandingViewModel
                    {
                        TeamID = teamId,
                        TeamName = team.Name,
                        TeamIcon = team.LogoUrl,
                        Group = currentGroupName,
                        Played = teamMatches.Count(),
                        Won = won,
                        Drawn = drawn,
                        Lost = teamMatches.Count() - won - drawn,
                        GoalsFor = teamMatches.Sum(m => m.HomeTeamID == teamId ? m.HomeScore ?? 0 : m.AwayScore ?? 0),
                        GoalsAgainst = teamMatches.Sum(m => m.HomeTeamID == teamId ? m.AwayScore ?? 0 : m.HomeScore ?? 0),
                        LastFiveMatches = teamMatches
                            .OrderByDescending(m => m.MatchDate)
                            .Take(5)
                            .Select(m =>
                            {
                                if (!m.HomeScore.HasValue || !m.AwayScore.HasValue) return "?";
                                if (m.HomeScore == m.AwayScore) return "D";
                                if (m.HomeTeamID == teamId) return m.HomeScore > m.AwayScore ? "W" : "L";
                                return m.AwayScore > m.HomeScore ? "W" : "L";
                            }).ToList(),
                        PenaltyPoints = penaltyPoints, // yeni alan
                        Points=points,
                        PenaltyDescription = penaltyDescriptions // yeni alan
                    };
                })
                .Where(s => s != null)
                .OrderByDescending(t => t.Points)
                .ThenByDescending(t => t.GoalDifference)
                .ThenByDescending(t => t.GoalsFor)
                .ToList();

                var viewModel = new LeagueStandingsViewModel
                {
                    LeagueID = leagueData.LeagueID,
                    LeagueName = leagueData.Name,
                    SeasonId = seasonId,
                    SeasonName = seasonName,
                    Standings = standings,
                    Groups = leagueGroups.Select(g => new GroupInfoViewModel { GroupID = g.GroupID, GroupName = g.GroupName }).ToList(),
                    CurrentGroupId = effectiveGroupId
                };

                return new LeagueStandingsResult { ViewModel = viewModel, IsAjaxRequest = isAjaxRequest };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lig puan durumu hesaplanırken hata oluştu. LeagueID: {LeagueID}, SeasonID: {SeasonID}, GroupID: {GroupID}", leagueId, seasonId, groupId);
                return new LeagueStandingsResult { ErrorMessage = "Puan durumu hesaplanırken bir hata oluştu: " + ex.Message, IsAjaxRequest = isAjaxRequest };
            }
        }
    }
}