using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using BussinessCupApi.ViewModels;
using BussinessCupApi.Attributes;
using BussinessCupApi.Data;
using BussinessCupApi.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using BussinessCupApi.ViewModels.MatchSquad;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net.Http;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using static BussinessCupApi.Models.Match;
using BussinessCupApi.Models.UserPlayerTypes;
using System.Numerics;
using BussinessCupApi.Extensions;
using BussinessCupApi.Managers;
using Microsoft.AspNetCore.Authorization;
using BussinessCupApi.DTOs;

namespace BussinessCupApi.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AdminController> _logger;
        private readonly LeagueManager _leagueManager; // LeagueManager eklendi
        private readonly CloudflareR2Manager _r2Manager;
        private readonly NotificationManager _notificationManager;
        private readonly OpenAiManager _openAIManager; 




        public AdminController(ApplicationDbContext context, IConfiguration configuration, ILogger<AdminController> logger,
            LeagueManager leagueManager, CloudflareR2Manager r2Manager,
            NotificationManager notificationManager, OpenAiManager openAiManager) // LeagueManager constructor'a eklendi
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
            _leagueManager = leagueManager; // Atama yapıldı
            _r2Manager = r2Manager;
            _notificationManager = notificationManager;
            _openAIManager = openAiManager; // OpenAiManager eklend
        }

        public async Task<IActionResult> Dashboard()
        {
            try
            {
                var leagues = await _context.Leagues
                    .Select(l => new
                    {
                        l.LeagueID,
                        l.Name,
                        l.LeagueType,
                        l.StartDate,
                        l.CityID,
                        Seasons = l.Seasons.Select(s => new
                        {
                            s.SeasonID,
                            s.Name,
                            s.IsActive
                        }).OrderByDescending(s => s.IsActive).ToList(),
                        Weeks = l.Weeks.Select(w => new
                        {
                            w.WeekID,
                            w.WeekNumber,
                            w.WeekStatus,
                            w.WeekName,
                            w.SeasonID,
                            Matches = w.Matches.Select(m => new
                            {
                                m.MatchID,
                                m.MatchDate,
                                m.HomeScore,
                                m.AwayScore,
                                m.IsPlayed,
                                HomeTeam = new { m.HomeTeam.TeamID, m.HomeTeam.Name },
                                AwayTeam = new { m.AwayTeam.TeamID, m.AwayTeam.Name }
                            })
                            .OrderBy(m => m.MatchDate)
                            .ToList()
                        })
                        .OrderBy(w => w.WeekNumber)
                        .ToList()
                    })
                    .OrderByDescending(l => l.LeagueID)
                    .ToListAsync();

                var cities = await _context.City.OrderBy(c => c.Name).ToListAsync();

                var viewModel = new DashboardViewModel
                {
                    Leagues = leagues.Select(l => new DashboardLeagueViewModel
                    {
                        LeagueID = l.LeagueID,
                        Name = l.Name,
                        LeagueType = l.LeagueType,
                        CityID = l.CityID,
                        Seasons = l.Seasons.Select(s => new DashboardSeasonViewModel
                        {
                            SeasonID = s.SeasonID,
                            Name = s.Name,
                            IsActive = s.IsActive
                        }).ToList(),
                        Weeks = l.Weeks.Select(w => new DashboardWeekViewModel
                        {
                            WeekID = w.WeekID,
                            WeekNumber = w.WeekNumber,
                            WeekName = w.WeekName,
                            WeekStatus = w.WeekStatus,
                            SeasonID = w.SeasonID,
                            Matches = w.Matches.Select(m => new DashboardMatchViewModel
                            {
                                MatchID = m.MatchID,
                                MatchDate = m.MatchDate,
                                HomeScore = m.HomeScore,
                                AwayScore = m.AwayScore,
                                IsPlayed = m.IsPlayed,
                                HomeTeam = new TeamBasicViewModel
                                {
                                    TeamID = m.HomeTeam.TeamID,
                                    Name = m.HomeTeam.Name
                                },
                                AwayTeam = new TeamBasicViewModel
                                {
                                    TeamID = m.AwayTeam.TeamID,
                                    Name = m.AwayTeam.Name
                                }
                            }).ToList()
                        }).ToList()
                    }).ToList(),
                    Cities = cities
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Dashboard yüklenirken hata oluştu");
                TempData["Error"] = "Veriler yüklenirken bir hata oluştu: " + ex.Message;
                return View(new DashboardViewModel { Leagues = new List<DashboardLeagueViewModel>() });
            }
        }


        public async Task<IActionResult> MatchDetails(int id)
        {
            try
            {
                var match = await _context.Matches
                    .Select(m => new Match
                    {
                        MatchID = m.MatchID,
                        LeagueID = m.LeagueID,
                        HomeTeamID = m.HomeTeamID,
                        AwayTeamID = m.AwayTeamID,
                        MatchDate = m.MatchDate,
                        HomeScore = m.HomeScore,
                        AwayScore = m.AwayScore,
                        IsPlayed = m.IsPlayed,
                        ManOfTheMatchID = m.ManOfTheMatchID,
                        HomeTeam = new Team
                        {
                            TeamID = m.HomeTeam.TeamID,
                            Name = m.HomeTeam.Name
                        },
                        AwayTeam = new Team
                        {
                            TeamID = m.AwayTeam.TeamID,
                            Name = m.AwayTeam.Name
                        },
                        Goals = m.Goals.Select(g => new Goal
                        {
                            GoalID = g.GoalID,
                            MatchID = g.MatchID,
                            TeamID = g.TeamID,
                            PlayerID = g.PlayerID,
                            AssistPlayerID = g.AssistPlayerID,
                            Minute = g.Minute,
                            IsPenalty = g.IsPenalty,
                            IsOwnGoal = g.IsOwnGoal,
                            Player = new Player
                            {
                                PlayerID = g.Player.PlayerID,
                                FirstName = g.Player.FirstName,
                                LastName = g.Player.LastName
                            },
                            AssistPlayer = g.AssistPlayer != null ? new Player
                            {
                                PlayerID = g.AssistPlayer.PlayerID,
                                FirstName = g.AssistPlayer.FirstName,
                                LastName = g.AssistPlayer.LastName
                            } : null
                        }).ToList(),
                        Cards = m.Cards.Select(c => new Card
                        {
                            CardID = c.CardID,
                            MatchID = c.MatchID,
                            PlayerID = c.PlayerID,
                            CardType = c.CardType,
                            Minute = c.Minute,
                            Player = new Player
                            {
                                PlayerID = c.Player.PlayerID,
                                FirstName = c.Player.FirstName,
                                LastName = c.Player.LastName
                            }
                        }).ToList(),
                        ManOfTheMatch = m.ManOfTheMatch != null ? new Player
                        {
                            PlayerID = m.ManOfTheMatch.PlayerID,
                            FirstName = m.ManOfTheMatch.FirstName,
                            LastName = m.ManOfTheMatch.LastName
                        } : null
                    })
                    .FirstOrDefaultAsync(m => m.MatchID == id);

                if (match == null)
                {
                    TempData["Error"] = "Maç bulunamadı.";
                    return RedirectToAction("Dashboard");
                }

                var homePlayers = await _context.Players
                    .Where(p => p.TeamID == match.HomeTeamID)
                    .Select(p => new Player
                    {
                        PlayerID = p.PlayerID,
                        FirstName = p.FirstName ?? "",
                        LastName = p.LastName ?? "",
                        Position = p.Position ?? "",
                        TeamID = p.TeamID
                    })
                    .OrderBy(p => p.Position)
                    .ToListAsync();

                var awayPlayers = await _context.Players
                    .Where(p => p.TeamID == match.AwayTeamID)
                    .Select(p => new Player
                    {
                        PlayerID = p.PlayerID,
                        FirstName = p.FirstName ?? "",
                        LastName = p.LastName ?? "",
                        Position = p.Position ?? "",
                        TeamID = p.TeamID
                    })
                    .OrderBy(p => p.Position)
                    .ToListAsync();

                var viewModel = new MatchDetailsViewModel
                {
                    Match = match,
                    HomePlayers = homePlayers,
                    AwayPlayers = awayPlayers
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Maç detayları yüklenirken bir hata oluştu: " + ex.Message;
                return RedirectToAction("Dashboard");
            }
        }



        public async Task<IActionResult> GetWeeks(int leagueId, int seasonId)
        {
            try
            {
                var weeks = await _context.Weeks
                    .Where(w => w.LeagueID == leagueId && w.SeasonID == seasonId)
                    .OrderBy(w => w.WeekNumber)
                    .Select(w => new SelectListItem
                    {
                        Value = w.WeekID.ToString(),
                        Text = $"{w.WeekName} ({w.StartDate:dd.MM.yyyy} - {w.EndDate:dd.MM.yyyy})"
                    })
                    .ToListAsync();

                return Json(weeks);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Haftalar yüklenirken bir hata oluştu: " + ex.Message);
            }
        }

        public async Task<IActionResult> CreateWeek(int leagueId, int seasonId)
        {
            try
            {
                var league = await _context.Leagues
                    .Include(l => l.Weeks)
                    .FirstOrDefaultAsync(l => l.LeagueID == leagueId);

                if (league == null)
                {
                    TempData["Error"] = "Lig bulunamadı.";
                    return RedirectToAction("Dashboard");
                }

                var nextWeekNumber = league.Weeks.Any()
                    ? league.Weeks.Max(w => w.WeekNumber) + 1
                    : 1;

                var viewModel = new WeekViewModel
                {
                    LeagueID = leagueId,
                    WeekNumber = nextWeekNumber,
                    StartDate = DateTime.Today,
                    EndDate = DateTime.Today.AddDays(7),
                    SeasonID = seasonId

                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Hafta ekleme sayfası yüklenirken bir hata oluştu: " + ex.Message;
                return RedirectToAction("Dashboard");
            }
        }

        [HttpPost]

        public async Task<IActionResult> CreateWeek(WeekViewModel model)
        {
            try
            {
                var week = new Week
                {
                    LeagueID = model.LeagueID,
                    WeekNumber = model.WeekNumber,
                    WeekStatus = model.WeekStatus,
                    WeekName = model.WeekName,
                    SeasonID = model.SeasonID,
                    StartDate = model.StartDate,
                    EndDate = model.EndDate
                };

                _context.Weeks.Add(week);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Hafta başarıyla oluşturuldu.";
                return RedirectToAction("Dashboard");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Hafta oluşturulurken hata oluştu");
                return Json(new { success = false, message = "Hafta oluşturulurken bir hata oluştu: " + ex.Message });
            }
        }


        public IActionResult CreateMatch(int leagueId, int weekId)
        {
            var model = new MatchViewModel
            {
                LeagueID = leagueId,
                WeekID = weekId,
                Teams = GetTeams(leagueId),
                MatchDate = DateTime.Now,
                Groups = new SelectList(
                    _context.Group
                        .Where(g => g.LeagueID == leagueId)
                        .OrderBy(g => g.GroupName)
                        .ToList(),
                    "GroupID",
                    "GroupName"
                )
            };

            return PartialView(model);
        }


        [HttpPost]
        public async Task<IActionResult> CreateMatch(MatchViewModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var match = new Match
                    {
                        LeagueID = model.LeagueID,
                        WeekID = model.WeekID,
                        HomeTeamID = model.HomeTeamID,
                        AwayTeamID = model.AwayTeamID,
                        MatchDate = model.MatchDate,
                        IsPlayed = false,
                        GroupID = model.GroupID
                    };

                    _context.Matches.Add(match);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Maç başarıyla eklendi.";
                    return RedirectToAction("Dashboard");
                }

                model.Weeks = await _context.Weeks
                    .Where(w => w.LeagueID == model.LeagueID)
                    .OrderBy(w => w.WeekNumber)
                    .Select(w => new SelectListItem
                    {
                        Value = w.WeekID.ToString(),
                        Text = $"{w.WeekNumber}. Hafta ({w.StartDate:dd.MM.yyyy} - {w.EndDate:dd.MM.yyyy})"
                    })
                    .ToListAsync();

                model.Groups = new SelectList(
                    _context.Group
                        .Where(g => g.LeagueID == model.LeagueID)
                        .OrderBy(g => g.GroupName)
                        .ToList(),
                    "GroupID",
                    "GroupName"
                );

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Maç eklenirken bir hata oluştu: " + ex.Message;
                return RedirectToAction("Dashboard");
            }
        }


        public async Task<IActionResult> CreateTeam()
        {
            // Şehirleri yükle
            var cities = await _context.City
                                    .OrderBy(c => c.Name)
                                    .Select(c => new SelectListItem
                                    {
                                        Value = c.CityID.ToString(),
                                        Text = c.Name
                                    })
                                    .ToListAsync();

            var viewModel = new TeamViewModel
            {
                // ViewBag yerine ViewModel'a şehir listesini ekleyelim
                AvailableCities = cities
            };

            return View(viewModel); // Güncellenmiş ViewModel'ı View'a gönder
        }

        // ... existing code ...

        [HttpPost]
        public async Task<IActionResult> CreateTeam(TeamViewModel model)
        {
            try
            {
                // City alanı dropdown'dan geldiği için manuel validasyonu kaldırabiliriz.
                // Ancak CityID zorunluysa, ViewModel'da Required attribute eklenmeli.
                // ModelState.Remove("City");

                if (ModelState.IsValid)
                {
                    string? logoFilePath = null; // Logo dosya yolunu tutacak değişken (nullable)

                    if (model.Logo != null && model.Logo.Length > 0)
                    {
                        // Logo dosyasını sunucuya kaydetme
                        //string uploadsFolder = Path.Combine(_env.WebRootPath, "images", "team_logos"); // Kaydedilecek klasör
                        //if (!Directory.Exists(uploadsFolder)) // Klasör yoksa oluştur
                        //{
                        //    Directory.CreateDirectory(uploadsFolder);
                        //}

                        // Benzersiz dosya adı oluşturma (çakışmaları önlemek için)
                        //string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(model.Logo.FileName);
                        //string filePath = Path.Combine(uploadsFolder, uniqueFileName); // Tam dosya yolu

                        // Dosyayı diske yazma
                        //using (var fileStream = new FileStream(filePath, FileMode.Create))
                        //{
                        //    await model.Logo.CopyToAsync(fileStream);
                        //}
                        //// Veritabanına kaydedilecek göreceli yol (relative path)
                        //logoFilePath = "/images/team_logos/" + uniqueFileName;


                        var key = $"teamimages/{Guid.NewGuid()}{Path.GetExtension(model.Logo.FileName)}";

                        using var stream = model.Logo.OpenReadStream();
                        await _r2Manager.UploadFileAsync(key, stream, model.Logo.ContentType);


                        logoFilePath = _r2Manager.GetFileUrl(key);

                    }

                    // Yeni Team nesnesi oluşturma
                    var team = new Team
                    {
                        Name = model.Name,
                        CityID = model.CityID, // CityID atandı
                        // City = model.City, // Şimdilik bu satırı yorumlayabilir veya kaldırabiliriz, CityID kullanıldığı için
                        Stadium = model.Stadium,
                        LogoUrl = logoFilePath, // Base64 yerine dosya yolunu kaydet
                        Manager = model.Manager,
                        TeamPassword = model.TeamPassword
                    };

                    // Veritabanına ekleme ve kaydetme
                    _context.Teams.Add(team);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Takım başarıyla oluşturuldu.";
                    return RedirectToAction("Teams"); // Takım listesi sayfasına yönlendir
                }

                // ModelState geçerli değilse hataları logla
                _logger.LogWarning("CreateTeam ModelState geçersiz.");
                foreach (var state in ModelState)
                {
                    foreach (var error in state.Value.Errors)
                    {
                        _logger.LogWarning($"Alan: {state.Key}, Hata: {error.ErrorMessage}");
                    }
                }

                // Şehir listesini tekrar yükle (hata durumunda formu tekrar gösterirken gerekli)
                model.AvailableCities = await _context.City
                                            .OrderBy(c => c.Name)
                                            .Select(c => new SelectListItem
                                            {
                                                Value = c.CityID.ToString(),
                                                Text = c.Name
                                            })
                .ToListAsync();


                // Formu hatalarla birlikte tekrar göster
                return View(model);
            }
            catch (Exception ex)
            {
                // Hata oluşursa logla ve kullanıcıya bilgi ver
                _logger.LogError(ex, "Takım oluşturulurken hata oluştu.");
                TempData["Error"] = "Takım oluşturulurken bir hata oluştu: " + ex.Message;

                model.AvailableCities = await _context.City
                                   .OrderBy(c => c.Name)
                                   .Select(c => new SelectListItem
                                   {
                                       Value = c.CityID.ToString(),
                                       Text = c.Name
                                   })
                .ToListAsync();

                TempData["Error"] = "Oyuncu düzenleme sayfası yüklenirken bir hata oluştu: " + ex.Message;
                return RedirectToAction("Teams"); // Veya Dashboard
            }
        }


        public async Task<IActionResult> EditPlayer(int playerId)
        {
            try
            {
                var player = await _context.Players.FindAsync(playerId);
                if (player == null)
                {
                    TempData["Error"] = "Oyuncu bulunamadı.";
                    // Takım detayları varsa oraya, yoksa genel bir listeye yönlendirilebilir.
                    // Örneğin, eğer bir önceki sayfadan gelindiyse history.back() çalışır.
                    // Direkt erişimlerde Teams listesine yönlendirmek daha mantıklı olabilir.
                    return RedirectToAction("Teams"); // Veya Dashboard
                }

                var user = await _context.Users.FindAsync(player.UserId);

                var teams = await _context.Teams
                    .Select(t => new SelectListItem
                    {
                        Value = t.TeamID.ToString(),
                        Text = t.Name
                    })
                    .ToListAsync();

                var viewModel = new PlayerViewModel
                {
                    PlayerID = player.PlayerID,
                    FirstName = player.FirstName,
                    LastName = player.LastName,
                    Position = player.Position ?? "",
                    Number = player.Number ?? 0,
                    // Tarih formatını kontrol et, eğer veritabanı DateOnly ise ToDateTime() gerekebilir
                    DateOfBirth = player.DateOfBirth.HasValue ? player.DateOfBirth.Value : DateTime.MinValue,
                    Nationality = player.Nationality,
                    IsCaptain = user != null && user.UserType == UserType.Captain, 
                    ExistingIcon = player.Icon, // Mevcut icon YOLUNU view'a gönder
                    TeamID = player.TeamID, // Doğru property'yi kullan
                    Teams = teams,
                    // Gerekirse Teams listesini de burada doldurun
                    // Teams = GetTeams()
                };

                // View'a giderken ViewData veya ViewBag ile takım adını göndermek isteyebilirsiniz
                // var team = await _context.Teams.FindAsync(viewModel.TeamID);
                // ViewBag.TeamName = team?.Name ?? "Takım Bilgisi Yok";


                // EditPlayer.cshtml dosyasını kullanacağız
                return View("EditPlayer", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Oyuncu düzenleme sayfası yüklenirken hata oluştu. PlayerID: {PlayerID}", playerId);
                TempData["Error"] = "Oyuncu düzenleme sayfası yüklenirken bir hata oluştu: " + ex.Message;
                return RedirectToAction("Teams"); // Veya Dashboard
            }
        }


        [HttpPost]
        public async Task<IActionResult> EditPlayer(PlayerViewModel model)
        {
            // PlayerID'yi kontrol et
            if (model.PlayerID <= 0)
            {
                ModelState.AddModelError("", "Geçersiz oyuncu ID'si.");
            }


            // Gerekirse diğer validasyonları kaldırabilir veya kontrol edebilirsiniz
            // ModelState.Remove("IconFile"); // IconFile zorunlu değilse
            // ModelState.Remove("ExistingIcon"); // Bu alan sadece gösterim için


            if (ModelState.IsValid)
            {
                try
                {
                    var player = await _context.Players.FindAsync(model.PlayerID);

                    if (model.IsCaptain)
                    {
                        var userExists = await _context.Users.AnyAsync(u => u.Id == player.UserId);
                        if (!userExists)
                        {
                            TempData["Error"] = "Bu oyuncu için bir kullanıcı yok, kaptan olamaz";
                            // Hata durumunda formu tekrar göstermeden önce gerekli listeleri doldur
                            // model.Teams = GetTeams();
                            return View("EditPlayer", model); // Hata durumunda EditPlayer view'ını göster
                        }
                    }

                    if (player == null)
                    {
                        TempData["Error"] = "Oyuncu bulunamadı.";
                        return RedirectToAction("Teams"); // Veya Dashboard
                    }

                    // Eski dosya yolunu sakla (silmek için)
                    string? oldIconPath = player.Icon;

                    // Icon işleme - Yeni dosya yüklendi mi?
                    if (model.IconFile != null && model.IconFile.Length > 0)
                    {
                        var key = $"playerimages/{Guid.NewGuid()}{Path.GetExtension(model.IconFile.FileName)}";

                        using var stream = model.IconFile.OpenReadStream();
                        await _r2Manager.UploadFileAsync(key, stream, model.IconFile.ContentType);


                        player.Icon = _r2Manager.GetFileUrl(key);
                    }
                    // Eğer yeni dosya yüklenmediyse, player.Icon'u değiştirme


                    // Diğer alanları güncelle
                    player.FirstName = model.FirstName;
                    player.LastName = model.LastName;
                    player.Position = model.Position;
                    player.Number = model.Number;
                    player.DateOfBirth = model.DateOfBirth;
                    player.Nationality = model.Nationality; 
                    player.TeamID = model.TeamID;
                    if (model.IsCaptain)
                    {
                        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == player.UserId);
                        if (user != null)
                        {
                            user.UserType = UserType.Captain;
                            user.UserRole = "Captain";
                            await _context.SaveChangesAsync();
                        }
                    }


                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Oyuncu başarıyla güncellendi.";
                    // Oyuncunun ait olduğu takımın detaylarına yönlendir

                    return RedirectToAction("Teams"); // Veya Dashboard

                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Oyuncu güncellenirken hata oluştu. PlayerID: {PlayerID}", model.PlayerID);
                    TempData["Error"] = "Oyuncu güncellenirken bir hata oluştu: " + ex.Message;
                    // Hata durumunda formu tekrar göstermeden önce gerekli listeleri doldur
                    // model.Teams = GetTeams();
                    return View("EditPlayer", model); // Hata durumunda EditPlayer view'ını göster
                }
            }


            // ModelState geçerli değilse formu tekrar göster
            // Gerekli listeleri tekrar yükle (ViewModel'de zaten olmalı)
            // model.Positions, model.Nationalities...


            // ModelState hatalarını loglayalım
            foreach (var modelStateEntry in ModelState.Values)
            {
                foreach (var error in modelStateEntry.Errors)
                {
                    _logger.LogWarning($"Model Hatası ({modelStateEntry}): {error.ErrorMessage}");
                }
            }


            return View("EditPlayer", model); // ModelState geçerli değilse EditPlayer view'ını göster
        }

        // ... AdminController.cs içinde ...


        public async Task<IActionResult> LeagueStandings(int leagueId, int seasonId, int? groupId = null)
        {
            bool isAjaxRequest = Request.Headers["X-Requested-With"] == "XMLHttpRequest";
            var result = await _leagueManager.GetLeagueStandingsAsync(leagueId, seasonId, groupId, isAjaxRequest);

            if (result.LeagueNotFound)
            {
                TempData["Error"] = result.ErrorMessage;
                if (result.IsAjaxRequest)
                {
                    return BadRequest(result.ErrorMessage);
                }
                return RedirectToAction("Dashboard");
            }

            if (!string.IsNullOrEmpty(result.ErrorMessage))
            {
                _logger.LogError("LeagueStandings metodunda manager'dan hata döndü. LeagueID: {LeagueID}, SeasonID: {SeasonID}, GroupID: {GroupID}, Hata: {Error}", leagueId, seasonId, groupId, result.ErrorMessage);
                TempData["Error"] = result.ErrorMessage; // Genel hata mesajı
                if (result.IsAjaxRequest)
                {
                    return StatusCode(500, "Puan durumu yüklenirken bir sunucu hatası oluştu.");
                }
                return RedirectToAction("Dashboard");
            }

            if (result.IsAjaxRequest)
            {
                return PartialView("_LeagueStandingsTable", result.ViewModel);
            }

            return View(result.ViewModel);
        }


        public async Task<IActionResult> Teams()
        {
            try
            {
                var teams = await _context.Teams
                    .Join(_context.City, // City tablosuyla join
                          team => team.CityID, // Teams tablosundaki join anahtarı
                          city => city.CityID, // City tablosundaki join anahtarı
                          (team, city) => new TeamListViewModel // Sonuç projeksiyonu
                          {
                              TeamID = team.TeamID,
                              Name = team.Name,
                              CityName = city.Name // City tablosundan şehir adını al
                          })
                    .OrderBy(t => t.Name)
                    .ToListAsync();

                return View(teams);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Takımlar listelenirken hata oluştu");
                TempData["Error"] = "Takımlar yüklenirken bir hata oluştu: " + ex.Message;
                // TeamListViewModel'in CityName alanı varsa, bu satır geçerli.
                // Yoksa, boş bir liste döndürmek için new List<TeamListViewModel>() kullanın.
                return View(new List<TeamListViewModel>()); // Hata durumunda boş liste döndür
            }
        }
        [HttpPost]
        public IActionResult SubscribePlayer(int playerId)
        {
            var player = _context.Players.FirstOrDefault(p => p.PlayerID == playerId);
            if (player != null)
            {
                player.isSubscribed = true;
                player.SubscriptionExpireDate = DateTime.Now.AddMonths(6);
                _context.SaveChanges();
                TempData["Success"] = "Oyuncu 6 ay abone yapıldı.";
            }
            else
            {
                TempData["Error"] = "Oyuncu bulunamadı.";
            }
            // Takım detaylarına geri dön
            return RedirectToAction("TeamDetails", new { teamId = player?.TeamID });
        }

        public async Task<IActionResult> TeamDetails(int id)
        {
            try
            {
                var team = await _context.Teams
                    .Select(t => new TeamDetailsViewModel
                    {
                        TeamID = t.TeamID,
                        Name = t.Name,
                        TeamIsFree=t.TeamIsFree,
                        Stadium = t.Stadium ?? "",
                        Manager = t.Manager ?? "",
                        Players = t.Players 
                            .OrderBy(p => p.Number)
                            .Select(p => new PlayerListViewModel
                            {
                                PlayerID = p.PlayerID,
                                FirstName = p.FirstName,
                                LastName = p.LastName,
                                Position = p.Position ?? "",
                                Number = p.Number ?? 0,
                                isSubscribed=p.isSubscribed,
                                SubscriptionExpireDate=p.SubscriptionExpireDate,
                                Icon = p.Icon,
                                LicensedPlayer=p.LicensedPlayer,
                                IsArchived = p.isArchived,                                
                                FrontIDImageUrl = p.FrontIdentityImage,
                                BackIDImageUrl = p.BackIdentityImage,
                                PlayerType = p.PlayerType != null ? p.PlayerType.GetDisplayName() : "",
                                UserRole = p.UserId != null
                                    ? _context.Users.Where(u => u.Id == p.UserId).Select(u => u.UserRole).FirstOrDefault()
                                    : null
                            })
                            .ToList()
                    })
                    .FirstOrDefaultAsync(t => t.TeamID == id);

                if (team == null)
                {
                    TempData["Error"] = "Takım bulunamadı.";
                    return RedirectToAction("Teams");
                }

                return View(team);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Takım detayları yüklenirken hata oluştu. TeamID: {TeamID}", id);
                TempData["Error"] = "Takım detayları yüklenirken bir hata oluştu: " + ex.Message;
                return RedirectToAction("Teams");
            }
        }

        public async Task<IActionResult> Settings()
        {
            var settings = await _context.Settings.FirstOrDefaultAsync();
            if (settings == null)
            {
                settings = new Settings
                {
                    LastUpdated = DateTime.Now
                };
                _context.Settings.Add(settings);
                await _context.SaveChangesAsync();
            }

            // OneSignal ayarlarını appsettings'den al ve ViewBag'e ekle
            ViewBag.OneSignalAppId = _configuration["OneSignal:AppId"];
            ViewBag.OneSignalApiKey = _configuration["OneSignal:ApiKey"];

            return View(settings);
        }

        [HttpPost]
        public async Task<IActionResult> SaveSettings(Settings model)
        {
            try
            {
                var settings = await _context.Settings.FirstOrDefaultAsync();
                if (settings == null)
                {
                    settings = new Settings();
                    _context.Settings.Add(settings);
                }

                settings.iOSVersion = model.iOSVersion;
                settings.AndroidVersion = model.AndroidVersion;
                settings.ForceUpdate = model.ForceUpdate;
                settings.AppStop = model.AppStop;
                settings.AppStopMessage = model.AppStopMessage;
                settings.LastUpdated = DateTime.Now;

                await _context.SaveChangesAsync();

                TempData["Success"] = "Ayarlar başarıyla kaydedildi.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ayarlar kaydedilirken hata oluştu");
                TempData["Error"] = "Ayarlar kaydedilirken bir hata oluştu.";
            }
            return RedirectToAction("Dashboard");
        }

        [HttpGet]
        public async Task<IActionResult> EditWeek(int weekId)
        {
            var week = await _context.Weeks
                .Include(w => w.League)
                .FirstOrDefaultAsync(w => w.WeekID == weekId);

            if (week == null)
                return NotFound();

            var model = new WeekViewModel
            {
                WeekID = week.WeekID,
                WeekNumber = week.WeekNumber,
                WeekName = week.WeekName,
                WeekStatus = week.WeekStatus,
                LeagueID = week.LeagueID,
                StartDate = week.StartDate,
                EndDate = week.EndDate
            };

            return PartialView("EditWeek", model);
        }

        [HttpPost]
        public async Task<IActionResult> EditWeek(WeekViewModel model)
        {
            if (!ModelState.IsValid)
                return PartialView("EditWeek", model);

            var week = await _context.Weeks.FindAsync(model.WeekID);
            if (week == null)
                return NotFound();

            week.WeekNumber = model.WeekNumber;
            week.WeekName = model.WeekName;
            week.WeekStatus = model.WeekStatus;
            week.StartDate = model.StartDate;
            week.EndDate = model.EndDate;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Hafta başarıyla güncellendi.";
            return RedirectToAction("Dashboard");
        }


        public IActionResult CreateSeason(int leagueId)
        {
            var league = _context.Leagues.FirstOrDefault(l => l.LeagueID == leagueId);
            if (league == null)
                return NotFound();

            var model = new SeasonViewModel
            {
                LeagueID = leagueId
            };

            ViewBag.LeagueName = league.Name;
            return PartialView("CreateSeason", model);
        }


        [HttpPost]
        public async Task<IActionResult> CreateSeason(SeasonViewModel model)
        {
            if (ModelState.IsValid)
            {
                var season = new Season
                {
                    Name = model.Name,
                    IsActive = false, // önce aktif yapma
                    LeagueID = model.LeagueID
                };

                if (model.IsActive)
                {
                    // Aynı ligin tüm aktif sezonlarını pasif yap
                    var activeSeasons = await _context.Season
                        .Where(s => s.LeagueID == model.LeagueID && s.IsActive)
                        .ToListAsync();

                    foreach (var activeSeason in activeSeasons)
                    {
                        activeSeason.IsActive = false;
                    }

                    // Yeni sezonu aktif olarak işaretle
                    season.IsActive = model.IsActive;

                }

                _context.Season.Add(season);
                await _context.SaveChangesAsync();
                return RedirectToAction("Dashboard");
            }

            ViewBag.Leagues = _context.Leagues.ToList();

            return PartialView("CreateSeason", model);
        }


        public async Task<IActionResult> GetSeasons(int leagueId)
        {
            var seasons = await _context.Season
                .Where(s => s.LeagueID == leagueId)
                .OrderByDescending(s => s.IsActive)
                .ThenByDescending(s => s.SeasonID)
                .Select(s => new { s.SeasonID, s.Name, s.IsActive })
                .ToListAsync();

            return Json(seasons);
        }

        [HttpGet]
        public async Task<IActionResult> GetWeekMatches(int weekId)
        {
            var matches = await _context.Matches
                .Where(m => m.WeekID == weekId)
                .Select(m => new
                {
                    matchId = m.MatchID,
                    homeTeam = m.HomeTeam.Name,
                    awayTeam = m.AwayTeam.Name,
                    homeScore = m.HomeScore,
                    awayScore = m.AwayScore,
                    status = m.Status,
                    groupId = m.GroupID,
                    groupName = m.Group.GroupName // Group null ise bu alan null olacak
                })
                .ToListAsync();

            return Json(matches);
        }

        private List<SelectListItem> GetTeams(int leagueId)
        {
            var cityId = _context.Leagues
                .Where(l => l.LeagueID == leagueId)
                .Select(l => l.CityID)
                .FirstOrDefault();

            if (cityId == 0)
            {
                return new List<SelectListItem>();
            }

            return _context.Teams
                .Where(t => t.CityID == cityId && t.TeamIsFree != true)
                .OrderBy(t => t.Name)
                .Select(t => new SelectListItem
                {
                    Value = t.TeamID.ToString(),
                    Text = t.Name
                })
                .ToList();
        }


        [HttpGet]
        public async Task<IActionResult> EditSeason(int seasonId)
        {
            var season = await _context.Season.FindAsync(seasonId);
            if (season == null)
            {
                return NotFound();
            }
            return PartialView(season);
        }

        [HttpPost]
        public async Task<IActionResult> EditSeason(int seasonId, string name, string isActive)
        {
            try
            {
                var season = await _context.Season.FindAsync(seasonId);

                // Checkbox işaretli ise "true", değilse "false" gelir
                bool isActiveBool = isActive == "true";

                if (isActiveBool)
                {
                    // Aynı ligin tüm aktif sezonlarını pasif yap
                    var activeSeasons = await _context.Season
                        .Where(s => s.LeagueID == season.LeagueID && s.IsActive)
                        .ToListAsync();

                    foreach (var activeSeason in activeSeasons)
                    {
                        activeSeason.IsActive = false;
                    }

                    // Yeni sezonu aktif olarak işaretle
                    season.IsActive = true;
                    season.Name = name;
                }
                else
                {
                    season.IsActive = false;
                    season.Name = name;
                }

                await _context.SaveChangesAsync();

                TempData["Success"] = "Sezon başarıyla güncellendi.";
                return RedirectToAction("Dashboard");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Sezon güncellenemedi.";
                return RedirectToAction("Dashboard");
            }
        }


        [HttpGet]
        public IActionResult CreateGroup(int leagueId,int seasonId)
        {
            var model = new CreateGroupViewModel { LeagueID = leagueId ,SeasonID= seasonId };
            return PartialView("_CreateGroup", model);
        }

        [HttpPost]
        public async Task<IActionResult> CreateGroup(CreateGroupViewModel model)
        {
            if (ModelState.IsValid)
            {
                var group = new Group
                {
                    LeagueID = model.LeagueID,
                    SeasonID=model.SeasonID,
                    GroupName = model.GroupName,
                    Description = model.Description
                };

                _context.Group.Add(group);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Grup başarıyla oluşturuldu.";
                return RedirectToAction("Dashboard");
            }
            TempData["Error"] = "Grup oluşturulamadı.";
            return RedirectToAction("Dashboard");
        }

        [HttpGet]
        public IActionResult EditGroups(int leagueId)
        {
            var groups = _context.Group
                .Where(g => g.LeagueID == leagueId)
                .ToList();

            return PartialView("_EditGroups", groups);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateGroup(int groupId, string groupName, string description)
        {
            var group = await _context.Group.FindAsync(groupId);
            if (group == null)
                return Json(new { success = false, message = "Grup bulunamadı" });

            group.GroupName = groupName;
            group.Description = description;
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteGroup(int groupId)
        {
            try
            {
                var group = await _context.Group.FindAsync(groupId);
                if (group == null)
                    return Json(new { success = false, message = "Grup bulunamadı" });

                _context.Group.Remove(group);
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Grup silinemedi." });
            }
        }

        [HttpPost]
        public IActionResult DeletePlayer(int playerId)
        {
            try
            {
                var player = _context.Players.FirstOrDefault(p => p.PlayerID == playerId);

                if (player == null)
                {
                    TempData["DeleteError"] = "Oyuncu bulunamadı.";
                    return RedirectToAction("TeamDetails", new { id = player.TeamID });
                }

                player.isArchived = true;
                _context.SaveChanges();

                return RedirectToAction("TeamDetails", new { id = player.TeamID });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Oyuncu silinemedi. Lütfen daha sonra tekrar deneyin." });
            }
        }


        public class DeleteMatchRequest
        {
            public int MatchId { get; set; }
        }
        [HttpPost]
        public async Task<IActionResult> DeleteMatch([FromBody] DeleteMatchRequest request)
        {
            try
            {
                var match = await _context.Matches
                    .FirstOrDefaultAsync(m => m.MatchID == request.MatchId);

                if (match == null)
                {
                    return Json(new { success = false, message = "Maç bulunamadı." });
                }

                _context.Matches.Remove(match);
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Maç silinirken bir hata oluştu." });
            }
        }

        public async Task<IActionResult> EditMatch(int matchId)
        {
            var match = await _context.Matches
                .Include(m => m.HomeTeam)
                .Include(m => m.AwayTeam)
                .Include(m => m.Week)
                        .ThenInclude(s => s.League)
                .FirstOrDefaultAsync(m => m.MatchID == matchId);

            if (match == null)
            {
                return NotFound();
            }

            var teams = await _context.Teams.Where(x => x.TeamIsFree != true && x.CityID == match.League.CityID).ToListAsync();

            var viewModel = new EditMatchViewModel
            {
                MatchId = match.MatchID,
                HomeTeamId = match.HomeTeamID,
                AwayTeamId = match.AwayTeamID,
                MatchDate = match.MatchDate,
                Teams = teams.Select(t => new SelectListItem
                {
                    Value = t.TeamID.ToString(),
                    Text = t.Name
                }).ToList()
            };

            return PartialView("_EditMatch", viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> EditMatch(EditMatchInputModel model) // ViewModel'i InputModel ile değiştirin
        {
            // HomeTeamId ve AwayTeamId'nin aynı olup olmadığını kontrol et
            if (model.HomeTeamId == model.AwayTeamId)
            {
                // ModelState'e özel bir hata ekle veya doğrudan JSON döndür
                // ModelState.AddModelError("", "Ev sahibi ve deplasman takımları aynı olamaz.");
                return Json(new { success = false, message = "Ev sahibi ve deplasman takımları aynı olamaz." });
            }

            if (!ModelState.IsValid)
            {
                // Model geçerli değilse, hataları birleştirip döndür
                var errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage));
                return Json(new { success = false, message = "Geçersiz veri: " + string.Join(", ", errors) });
            }

            try
            {
                var match = await _context.Matches.FindAsync(model.MatchId);
                if (match == null)
                {
                    return Json(new { success = false, message = "Maç bulunamadı." });
                }

                match.HomeTeamID = model.HomeTeamId;
                match.AwayTeamID = model.AwayTeamId;
                match.MatchDate = model.MatchDate;

                await _context.SaveChangesAsync();

                TempData["Success"] = "Maç başarıyla güncellendi."; // Başarı mesajını TempData'ya ekle
                return RedirectToAction("Dashboard");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Maç güncellenirken hata oluştu. MatchId: {MatchId}", model.MatchId); // Hata loglama
                return Json(new { success = false, message = "Maç güncellenirken bir sunucu hatası oluştu." }); // Genel hata mesajı
            }
        }

        public async Task<IActionResult> Notifications()
        {
            // Şehir listesini ViewBag ile gönder
            ViewBag.Cities = await _context.City.OrderBy(c => c.Name).ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendGeneralPush([FromBody] PushGeneralDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Title) || string.IsNullOrWhiteSpace(dto.Detail))
                return BadRequest("Başlık ve detay zorunludur.");

            var notification = new NotificationViewModel
            {
                TitleTr = dto.Title,
                MessageTr = dto.Detail
            };

            var (success, message) = await _notificationManager.SendNotificationToAllUsers(notification);
            if (success)
                return Ok(new { message });
            return StatusCode(500, message);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendTeamPush([FromBody] PushTeamDto dto)
        {
            if (dto.TeamId <= 0 || string.IsNullOrWhiteSpace(dto.Title) || string.IsNullOrWhiteSpace(dto.Detail))
                return BadRequest("Takım, başlık ve detay zorunludur.");

            var notification = new NotificationViewModel
            {
                TitleTr = dto.Title,
                MessageTr = dto.Detail
            };

            var (success, message) = await _notificationManager.SendNotificationToGroup($"team_{dto.TeamId}", notification);
            if (success)
                return Ok(new { message });
            return StatusCode(500, message);
        }


        [HttpPost]
        public async Task<IActionResult> CreateLeague(CreateLeagueViewModel model)
        {
            if (ModelState.IsValid)
            {
                var league = new League
                {
                    Name = model.Name,
                    LeagueType = model.LeagueType,
                    StartDate = model.StartDate,
                    EndDate = model.EndDate,
                    IsActive = true
                };

                _context.Leagues.Add(league);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Lig başarıyla oluşturuldu.";
                return RedirectToAction("Dashboard");
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> EditLeague(EditLeagueViewModel model)
        {
            if (ModelState.IsValid)
            {
                var league = await _context.Leagues.FindAsync(model.LeagueID);
                if (league == null)
                {
                    return NotFound();
                }

                league.Name = model.Name;
                league.LeagueType = model.LeagueType;
                league.StartDate = model.StartDate;
                league.EndDate = model.EndDate;

                await _context.SaveChangesAsync();
                TempData["Success"] = "Lig başarıyla güncellendi.";
                return RedirectToAction("Dashboard");
            }

            return View(model);
        }


        public async Task<IActionResult> WeekBestTeam()
        {
            var leagues = await _context.Leagues
                   .Select(l => new { l.LeagueID, l.Name })
                   .ToListAsync();

            ViewBag.Leagues = new SelectList(leagues, "LeagueID", "Name");
            return View(new WeekBestTeamViewModel());
        }

        [HttpGet]
        public async Task<IActionResult> GetSeasonWeeks(int leagueId, int seasonId)
        {
            var weeks = await _context.Weeks
                .Where(w => w.LeagueID == leagueId && w.SeasonID == seasonId)
                .OrderBy(w => w.WeekNumber)
                .Select(w => new { value = w.WeekID, text = w.WeekName })
                .ToListAsync();

            return Json(weeks);
        }

        [HttpGet]
        public async Task<IActionResult> GetWeekPlayers(int weekId)
        {
            var players = await _context.Players
                .Include(p => p.Team)
                .Where(p => p.Team.HomeMatches.Any(m => m.WeekID == weekId) ||
                p.Team.AwayMatches.Any(m => m.WeekID == weekId))
                .Select(p => new PlayerSelectionViewModel
                {
                    PlayerID = p.PlayerID,
                    FullName = p.FirstName + " " + p.LastName,
                    Position = p.Position,
                    TeamName = p.Team.Name,
                    PlayerImage = p.Icon,
                    IsSelected = false,
                    IsBestPlayer = false
                })
                .ToListAsync();

            return Json(players);
        }

        [HttpGet]
        public async Task<IActionResult> GetWeekBestTeamData(int weekId)
        {
            var weekBestTeam = await _context.WeekBestTeams
                .Include(wbt => wbt.Players) // İlişkili oyuncuları da getir
                .FirstOrDefaultAsync(wbt => wbt.WeekID == weekId);

            if (weekBestTeam == null)
            {
                return Json(new { found = false }); // Veri bulunamadı durumu
            }

            var data = new
            {
                found = true,
                weekBestTeamId = weekBestTeam.WeekBestTeamID,
                bestPlayerID = weekBestTeam.BestPlayerID,
                bestTeamID = weekBestTeam.BestTeamID,
                selectedPlayerIDs = weekBestTeam.Players.Select(p => p.PlayerID).ToList()
            };

            return Json(data);
        }

        [HttpPost]
        public async Task<IActionResult> SaveWeekBestTeam([FromBody] RequestWeekBestTeamViewModel model)
        {
            if (!model.BestPlayerID.HasValue)
                return BadRequest("Haftanın en iyi oyuncusu seçilmelidir.");

            if (!model.BestTeamID.HasValue)
                return BadRequest("Haftanın takımı seçilmelidir.");

            try
            {
                var existingRecord = await _context.WeekBestTeams
                    .Include(wbt => wbt.Players) // Güncelleme için ilişkili oyuncuları getir
                    .FirstOrDefaultAsync(wbt => wbt.WeekID == model.WeekID);

                if (existingRecord != null)
                {
                    // Mevcut kaydı güncelle
                    existingRecord.BestPlayerID = model.BestPlayerID.Value;
                    existingRecord.BestTeamID = model.BestTeamID.Value;

                    // Eski oyuncu kayıtlarını sil
                    _context.WeekBestTeamPlayers.RemoveRange(existingRecord.Players);

                    // Yeni oyuncu kayıtlarını ekle
                    var players = model.SelectedPlayers.Select((playerId, index) => new WeekBestTeamPlayers
                    {
                        WeekBestTeamID = existingRecord.WeekBestTeamID, // Mevcut ID'yi kullan
                        PlayerID = playerId,
                        OrderNumber = index + 1
                    }).ToList();
                    await _context.WeekBestTeamPlayers.AddRangeAsync(players);

                    await _context.SaveChangesAsync();
                    return Ok(new { success = true, message = "Haftanın 11'i başarıyla güncellendi." });
                }
                else
                {
                    // Yeni kayıt oluştur
                    var weekBestTeam = new WeekBestTeams
                    {
                        WeekID = model.WeekID,
                        LeagueID = model.LeagueID,
                        SeasonID = model.SeasonID,
                        BestPlayerID = model.BestPlayerID.Value,
                        BestTeamID = model.BestTeamID.Value
                    };

                    _context.WeekBestTeams.Add(weekBestTeam);
                    await _context.SaveChangesAsync(); // Önce ana kaydı kaydet

                    var players = model.SelectedPlayers.Select((playerId, index) => new WeekBestTeamPlayers
                    {
                        WeekBestTeamID = weekBestTeam.WeekBestTeamID, // Yeni ID'yi kullan
                        PlayerID = playerId,
                        OrderNumber = index + 1
                    }).ToList();

                    await _context.WeekBestTeamPlayers.AddRangeAsync(players);
                    await _context.SaveChangesAsync(); // Oyuncuları kaydet

                    return Ok(new { success = true, message = "Haftanın kadrosu başarıyla kaydedildi." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Haftanın kadrosu kaydedilirken/güncellenirken hata oluştu. WeekID: {WeekID}", model.WeekID);
                return StatusCode(500, new { success = false, message = "Bir hata oluştu: " + ex.Message });
            }
        }

        public IActionResult GetWeekTeams(int weekId)
        {
            var teams = _context.Matches
                .Where(m => m.WeekID == weekId)
                .Include(m => m.HomeTeam)
                .Include(m => m.AwayTeam)
                .AsEnumerable() // client-side geçiş
                .SelectMany(m => new[] { m.HomeTeam, m.AwayTeam })
                .Distinct()
                .Select(t => new { teamID = t.TeamID, teamName = t.Name })
                .ToList();

            return Json(teams);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateScore(int matchId, int homeScore, int awayScore, int? manOfTheMatchId, string? matchUrl)
        {
            try
            {
                var match = await _context.Matches
                    .Include(m => m.HomeTeam)
                    .Include(m => m.AwayTeam)
                    .FirstOrDefaultAsync(m => m.MatchID == matchId);

                if (match == null)
                {
                    if (Request.IsAjax())
                    {
                        return Json(new { success = false, message = "Maç bulunamadı." });
                    }
                    return NotFound();
                }

                bool homeScoreChanged = match.HomeScore != homeScore;
                bool awayScoreChanged = match.AwayScore != awayScore;

                match.HomeScore = homeScore;
                match.AwayScore = awayScore;
                match.ManOfTheMatchID = manOfTheMatchId;
                match.MatchURL = matchUrl;
                match.IsPlayed = true;

                await _context.SaveChangesAsync();

                // Bildirim gönder
                if (homeScoreChanged)
                {
                    var notification = new NotificationViewModel
                    {
                        TitleTr = $"{match.HomeTeam.Name} Golü!!!!",
                        MessageTr = $"Skor: {match.HomeTeam.Name} {homeScore} - {match.AwayTeam.Name} {awayScore}"
                    };
                    await _notificationManager.SendNotificationToGroup($"team_{match.HomeTeam.TeamID}", notification);
                }
                if (awayScoreChanged)
                {
                    var notification = new NotificationViewModel
                    {
                        TitleTr = $"{match.AwayTeam.Name} Golü!!!",
                        MessageTr = $"Skor: {match.HomeTeam.Name} {homeScore} - {match.AwayTeam.Name} {awayScore}"
                    };
                    await _notificationManager.SendNotificationToGroup($"team_{match.AwayTeam.TeamID}", notification);
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {

                return Json(new { success = false, message = "Skor güncellenirken bir hata oluştu." });

            }
        }

        public async Task<IActionResult> UpdateScore(int matchId)
        {
            var match = await _context.Matches
                .Select(m => new Match
                {
                    MatchID = m.MatchID,
                    LeagueID = m.LeagueID,
                    HomeTeamID = m.HomeTeamID,
                    AwayTeamID = m.AwayTeamID,
                    MatchDate = m.MatchDate,
                    HomeScore = m.HomeScore,
                    AwayScore = m.AwayScore,
                    IsPlayed = m.IsPlayed,
                    ManOfTheMatchID = m.ManOfTheMatchID,
                    MatchURL = m.MatchURL,
                    HomeTeam = new Team
                    {
                        TeamID = m.HomeTeam.TeamID,
                        Name = m.HomeTeam.Name
                    },
                    AwayTeam = new Team
                    {
                        TeamID = m.AwayTeam.TeamID,
                        Name = m.AwayTeam.Name
                    },
                    Goals = m.Goals.Select(g => new Goal
                    {
                        GoalID = g.GoalID,
                        MatchID = g.MatchID,
                        TeamID = g.TeamID,
                        PlayerID = g.PlayerID,
                        AssistPlayerID = g.AssistPlayerID,
                        Minute = g.Minute,
                        IsPenalty = g.IsPenalty,
                        IsOwnGoal = g.IsOwnGoal,
                        Player = new Player
                        {
                            PlayerID = g.Player.PlayerID,
                            FirstName = g.Player.FirstName,
                            LastName = g.Player.LastName
                        },
                        AssistPlayer = g.AssistPlayer != null ? new Player
                        {
                            PlayerID = g.AssistPlayer.PlayerID,
                            FirstName = g.AssistPlayer.FirstName,
                            LastName = g.AssistPlayer.LastName
                        } : null
                    }).ToList(),
                    Cards = m.Cards.Select(c => new Card
                    {
                        CardID = c.CardID,
                        MatchID = c.MatchID,
                        PlayerID = c.PlayerID,
                        CardType = c.CardType,
                        Minute = c.Minute,
                        Player = new Player
                        {
                            PlayerID = c.Player.PlayerID,
                            FirstName = c.Player.FirstName,
                            LastName = c.Player.LastName
                        }
                    }).ToList(),
                    ManOfTheMatch = m.ManOfTheMatch != null ? new Player
                    {
                        PlayerID = m.ManOfTheMatch.PlayerID,
                        FirstName = m.ManOfTheMatch.FirstName,
                        LastName = m.ManOfTheMatch.LastName
                    } : null
                })
                .FirstOrDefaultAsync(m => m.MatchID == matchId);

            if (match == null)
                return NotFound();

            // Oyuncular için de sadece gerekli alanları seçelim
            ViewBag.Players = await _context.Players
                .Where(p => p.TeamID == match.HomeTeamID || p.TeamID == match.AwayTeamID)
                .Select(p => new Player
                {
                    PlayerID = p.PlayerID,
                    FirstName = p.FirstName,
                    LastName = p.LastName,
                    TeamID = p.TeamID,
                    Position = p.Position,
                    Number = p.Number
                })
                .OrderBy(p => p.FirstName)
                .ToListAsync();

            return View(match);
        }

        [HttpPost]
        public async Task<IActionResult> AddGoal(int matchId, int playerId, int? assistPlayerId, int minute, string isPenalty, string isOwnGoal)
        {
            var match = await _context.Matches.FindAsync(matchId);
            if (match == null)
            {
                return NotFound();
            }

            var player = await _context.Players.FindAsync(playerId);
            if (player == null)
            {
                return BadRequest("Oyuncu bulunamadı!");
            }

            var goal = new Goal
            {
                MatchID = matchId,
                TeamID = player.TeamID,
                PlayerID = playerId,
                AssistPlayerID = assistPlayerId,
                Minute = minute,
                IsPenalty = isPenalty == "on",
                IsOwnGoal = isOwnGoal == "on"
            };

            _context.Goals.Add(goal);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> AddCard([FromForm] int matchId, [FromForm] int playerId,
            [FromForm] CardType cardType, [FromForm] int minute)
        {
            try
            {
                var card = new Card
                {
                    MatchID = matchId,
                    PlayerID = playerId,
                    CardType = cardType,
                    Minute = minute
                };

                _context.Cards.Add(card);
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteGoal(int goalId)
        {
            var goal = await _context.Goals.FindAsync(goalId);
            if (goal != null)
            {
                _context.Goals.Remove(goal);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            return Json(new { success = false, message = "Gol bulunamadı." });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteCard(int cardId)
        {
            var card = await _context.Cards.FindAsync(cardId);
            if (card != null)
            {
                _context.Cards.Remove(card);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            return Json(new { success = false, message = "Kart bulunamadı." });
        }

        [HttpPost]
        public async Task<IActionResult> SaveMatchSquad()
        {
            try
            {
                var form = Request.Form;
                if (!form.ContainsKey("matchId") || !form.ContainsKey("homeTeamId") || !form.ContainsKey("awayTeamId") || !form.ContainsKey("homeSquad") || !form.ContainsKey("awaySquad"))
                    return BadRequest("Eksik veri gönderildi.");

                int matchId = int.TryParse(form["matchId"], out var mId) ? mId : 0;
                int homeTeamId = int.TryParse(form["homeTeamId"], out var hId) ? hId : 0;
                int awayTeamId = int.TryParse(form["awayTeamId"], out var aId) ? aId : 0;
                if (matchId == 0 || homeTeamId == 0 || awayTeamId == 0)
                    return BadRequest("Geçersiz takım veya maç ID.");

                var homeSquadJson = form["homeSquad"].ToString();
                var awaySquadJson = form["awaySquad"].ToString();

                // Debug için loglama ekleyelim
                _logger.LogInformation($"Home Squad JSON: {homeSquadJson}");
                _logger.LogInformation($"Away Squad JSON: {awaySquadJson}");

                if (string.IsNullOrWhiteSpace(homeSquadJson) || string.IsNullOrWhiteSpace(awaySquadJson))
                    return BadRequest("Kadro verisi eksik.");

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var homeSquad = JsonSerializer.Deserialize<List<DashBoardPlayerSquadViewModel>>(homeSquadJson, options);
                var awaySquad = JsonSerializer.Deserialize<List<DashBoardPlayerSquadViewModel>>(awaySquadJson, options);

                // Debug için loglama ekleyelim
                _logger.LogInformation($"Home Squad Count: {homeSquad?.Count ?? 0}");
                _logger.LogInformation($"Away Squad Count: {awaySquad?.Count ?? 0}");

                if (homeSquad == null || awaySquad == null)
                    return BadRequest("Kadro verisi çözümlenemedi.");

                var match = await _context.Matches
                    .Include(m => m.MatchSquads)
                    .FirstOrDefaultAsync(m => m.MatchID == matchId);

                if (match == null)
                    return NotFound();

                // Mevcut kadroları temizle
                _context.MatchSquads.RemoveRange(match.MatchSquads);

                // Yeni kadroları ekle
                var allSquads = homeSquad.Concat(awaySquad)
                    //.Where(p => p.IsStarting11 || p.IsSubstitute) // Sadece kadroda olanları al
                    .Select(p => new MatchSquad
                    {
                        MatchID = matchId,
                        PlayerID = p.PlayerId,
                        TeamID = p.TeamId,
                        ShirtNumber = p.ShirtNumber,
                        IsStarting11 = p.IsStarting11,
                        IsSubstitute = p.IsSubstitute,
                        TopPosition = p.TopPosition,
                        LeftPosition = p.LeftPosition
                    });

                // Debug için loglama ekleyelim
                _logger.LogInformation($"All Squads Count: {allSquads.Count()}");

                await _context.MatchSquads.AddRangeAsync(allSquads);

                // Formasyon resimlerini işle
                var homeImg = Request.Form.Files["homeTeamFormationImg"];
                var awayImg = Request.Form.Files["awayTeamFormationImg"];

                var homeFormationUrl = homeImg != null ? await UploadFormationToCloudflare(matchId, homeTeamId, homeImg) : null;
                var awayFormationUrl = awayImg != null ? await UploadFormationToCloudflare(matchId, awayTeamId, awayImg) : null;

                await SaveOrUpdateFormationImageUrl(matchId, homeTeamId, homeFormationUrl);
                await SaveOrUpdateFormationImageUrl(matchId, awayTeamId, awayFormationUrl);

                await _context.SaveChangesAsync();

                TempData["Success"] = "Maç kadrosu başarıyla kaydedildi.";
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Maç kadrosu kaydedilirken hata oluştu");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        private async Task<string?> UploadFormationToCloudflare(int matchId, int teamId, IFormFile? file)
        {
            if (file == null || file.Length == 0)
                return null;
            var key = $"team_formations/{matchId}_{teamId}_{Guid.NewGuid()}.png";
            using var stream = file.OpenReadStream();
            await _r2Manager.UploadFileAsync(key, stream, file.ContentType);
            return _r2Manager.GetFileUrl(key);
        }

        private async Task SaveOrUpdateFormationImageUrl(int matchId, int teamId, string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
                return;
            var existingFormation = await _context.MatchSquadFormations
                .FirstOrDefaultAsync(f => f.MatchID == matchId && f.TeamID == teamId);
            if (existingFormation != null)
            {
                existingFormation.FormationImage = imageUrl;
                _context.MatchSquadFormations.Update(existingFormation);
            }
            else
            {
                var newFormation = new MatchSquadFormation
                {
                    MatchID = matchId,
                    TeamID = teamId,
                    FormationImage = imageUrl
                };
                await _context.MatchSquadFormations.AddAsync(newFormation);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Suspensions()
        {
            var leagues = await _context.Leagues
                .OrderBy(l => l.Name)
                .Select(l => new SelectListItem { Value = l.LeagueID.ToString(), Text = l.Name })
                .ToListAsync();

            var viewModel = new PlayerSuspensionViewModel
            {
                Leagues = new SelectList(leagues, "Value", "Text")
            };

            return View(viewModel);
        }

        // Haftanın oyuncularını getiren AJAX metodu (Ceza eklenecek oyuncu listesi için)

        [HttpGet]
        public async Task<IActionResult> GetWeekPlayersForSuspension(int weekId)
        {
            // Bu metod GetWeekPlayers ile benzer olabilir, belki birleştirilebilir.
            // O hafta maç yapan takımların oyuncularını getirir.
            var players = await _context.Players
               .Include(p => p.Team) // Takım bilgisi gerekebilir
               .Where(p => p.Team.HomeMatches.Any(m => m.WeekID == weekId) ||
                           p.Team.AwayMatches.Any(m => m.WeekID == weekId))
               .Where(p => p.isArchived != true)
               .OrderBy(p => p.Team.Name).ThenBy(p => p.LastName) // Takım ve soyada göre sırala
               .Select(p => new SelectListItem
               {
                   Value = p.PlayerID.ToString(),
                   Text = $"{p.FirstName} {p.LastName} ({p.Team.Name})" // Takım adını ekle
               })
               .ToListAsync();

            // Daha önce ceza almış oyuncuları listeden çıkarmak isteyebilirsiniz (opsiyonel)
            var alreadySuspendedPlayerIds = await _context.PlayerSuspension
               .Where(ps => ps.WeekID == weekId)
               .Select(ps => ps.PlayerID)
               .ToListAsync();

            players = players.Where(p => !alreadySuspendedPlayerIds.Contains(int.Parse(p.Value))).ToList();


            return Json(players);
        }

        // Seçilen haftadaki mevcut cezaları getiren AJAX metodu

        [HttpGet]
        public async Task<IActionResult> GetSuspendedPlayers(int weekId)
        {
            var suspensions = await _context.PlayerSuspension
                .Where(ps => ps.WeekID == weekId)
                .Include(ps => ps.Player) // Oyuncu adını almak için
                .Select(ps => new ExistingSuspensionViewModel
                {
                    PlayerSuspensionID = ps.PlayerSuspensionID,
                    PlayerID = ps.PlayerID,
                    PlayerFullName = ps.Player.FirstName + " " + ps.Player.LastName,
                    SuspensionType = ps.SuspensionType,
                    // Enum DisplayName'i almak için bir extension method veya switch case kullanın
                    SuspensionTypeDisplay = ps.SuspensionType.GetDisplayName(), // GetDisplayName() extension metodu varsayılıyor
                    GamesSuspended = ps.GamesSuspended,
                    Notes = ps.Notes
                })
                .OrderBy(vm => vm.PlayerFullName)
                .ToListAsync();

            return Json(suspensions);
        }


        [HttpPost]
        public async Task<IActionResult> AddSuspension([FromBody] PlayerSuspensionViewModel model)
        {
            // Gelen modelin doğruluğunu kontrol et (özellikle ID'ler)
            if (model.SelectedWeekId <= 0 || model.SelectedPlayerId <= 0 || model.GamesSuspended <= 0)
            {
                return Json(new { success = false, message = "Geçersiz veri gönderildi." });
            }

            // İlgili hafta ve oyuncu var mı kontrolü (opsiyonel ama önerilir)
            var weekExists = await _context.Weeks.AnyAsync(w => w.WeekID == model.SelectedWeekId);
            var playerExists = await _context.Players.AnyAsync(p => p.PlayerID == model.SelectedPlayerId);

            if (!weekExists || !playerExists)
            {
                return Json(new { success = false, message = "Hafta veya oyuncu bulunamadı." });
            }


            // Bu oyuncu için bu hafta zaten ceza var mı? (Aynı türden?) Tekrara izin veriliyor mu?
            // Şimdilik basitçe ekleyelim. Gerekirse buraya kontrol eklenebilir.
            // bool alreadyExists = await _context.PlayerSuspensions
            //    .AnyAsync(ps => ps.WeekID == model.SelectedWeekId && ps.PlayerID == model.SelectedPlayerId);
            // if (alreadyExists) {
            //     return Json(new { success = false, message = "Bu oyuncu için bu hafta zaten bir ceza kaydı var." });
            // }

            try
            {
                // Hafta bilgisinden Lig ve Sezon ID'lerini al
                var weekInfo = await _context.Weeks
                    .Where(w => w.WeekID == model.SelectedWeekId)
                    .Select(w => new { w.LeagueID, w.SeasonID })
                    .FirstOrDefaultAsync();

                if (weekInfo == null)
                {
                    return Json(new { success = false, message = "Hafta bilgisi bulunamadı." });
                }


                var suspension = new PlayerSuspensions
                {
                    WeekID = model.SelectedWeekId,
                    PlayerID = model.SelectedPlayerId,
                    LeagueID = weekInfo.LeagueID, // Haftadan aldık
                    SeasonID = weekInfo.SeasonID, // Haftadan aldık
                    SuspensionType = model.SuspensionType,
                    GamesSuspended = model.GamesSuspended,
                    Notes = model.Notes,
                    DateCreated = DateTime.UtcNow
                };

                _context.PlayerSuspension.Add(suspension);
                await _context.SaveChangesAsync();

                // Başarılı eklemeden sonra eklenen veriyi geri döndürebiliriz (opsiyonel)
                var addedSuspensionData = new ExistingSuspensionViewModel
                {
                    PlayerSuspensionID = suspension.PlayerSuspensionID,
                    PlayerID = suspension.PlayerID,
                    PlayerFullName = (await _context.Players.FindAsync(suspension.PlayerID))?.FirstName ?? "Bilinmeyen Oyuncu", // Oyuncu adını al
                    SuspensionType = suspension.SuspensionType,
                    SuspensionTypeDisplay = suspension.SuspensionType.GetDisplayName(),
                    GamesSuspended = suspension.GamesSuspended,
                    Notes = suspension.Notes
                };


                return Json(new { success = true, message = "Ceza başarıyla eklendi.", newItem = addedSuspensionData });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ceza eklenirken hata oluştu. WeekID: {WeekId}, PlayerID: {PlayerId}", model.SelectedWeekId, model.SelectedPlayerId);
                return Json(new { success = false, message = "Ceza eklenirken bir sunucu hatası oluştu." });
            }
        }


        // Ceza Silme Metodu

        [HttpPost] // Veya [HttpDelete] kullanabilirsiniz, ancak formdan AJAX ile post daha yaygın.
        public async Task<IActionResult> DeleteSuspension([FromBody] int suspensionId)
        {
            if (suspensionId <= 0)
            {
                return Json(new { success = false, message = "Geçersiz ID." });
            }

            try
            {
                var suspension = await _context.PlayerSuspension.FindAsync(suspensionId);
                if (suspension == null)
                {
                    return Json(new { success = false, message = "Silinecek ceza kaydı bulunamadı." });
                }

                _context.PlayerSuspension.Remove(suspension);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Ceza başarıyla silindi." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ceza silinirken hata oluştu. SuspensionID: {SuspensionID}", suspensionId);
                return Json(new { success = false, message = "Ceza silinirken bir sunucu hatası oluştu." });
            }
        }


        [HttpPost]
        // Gerekliyse yetkilendirme ekleyin
        public async Task<IActionResult> UpdateMatchStatus([FromBody] UpdateMatchStatusRequest model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Geçersiz veri." });
            }

            try
            {
                var match = await _context.Matches.FindAsync(model.MatchId);
                if (match == null)
                {
                    return Json(new { success = false, message = "Maç bulunamadı." });
                }

                // Gelen status değerini MatchStatus enum'una çevir (eğer enum kullanılıyorsa)
                // veya doğrudan int olarak ata. Enum kullanmak daha güvenli olabilir.
                // Enum'a çevirme işlemi için validation eklemek iyi olur.
                if (Enum.IsDefined(typeof(MatchStatus), model.Status)) // MatchStatus enum'unuzun adı varsayılan olarak kullanıldı
                {
                    match.Status = (MatchStatus)model.Status; // MatchStatus enum'unuzun adını buraya yazın
                }
                else
                {
                    _logger.LogWarning($"Geçersiz maç durumu değeri alındı: {model.Status} for MatchId: {model.MatchId}");

                    return Json(new { success = false, message = "Geçersiz maç durumu değeri." });
                }

                if (match.Status == MatchStatus.Started)
                {
                    match.MatchStarted = DateTime.Now;

                    match.HomeScore = 0;
                    match.AwayScore = 0;
                }

                if (match.Status == MatchStatus.Finished)
                {
                    var existingNews = _context.MatchNews
                            .Where(m => m.MatchID == match.MatchID)
                            .ToList();

                    if (existingNews.Any())
                    {
                        _context.MatchNews.RemoveRange(existingNews);
                        await _context.SaveChangesAsync(); // async değilse .SaveChanges() kullan
                    }

                    // Golleri ve kartları çek
                    var goals = await _context.Goals
                        .Where(g => g.MatchID == match.MatchID)
                        .Include(g => g.Player)
                        .Include(g => g.AssistPlayer)
                        .ToListAsync();

                    var cards = await _context.Cards
                        .Where(c => c.MatchID == match.MatchID)
                        .Include(c => c.Player)
                        .ToListAsync();

                    // Takım adları ve skorlar
                    var homeTeam = (await _context.Teams.FindAsync(match.HomeTeamID))?.Name ?? "Ev Sahibi";
                    var awayTeam = (await _context.Teams.FindAsync(match.AwayTeamID))?.Name ?? "Deplasman";

                    int homeScore = match.HomeScore ?? 0;
                    int awayScore = match.AwayScore ?? 0;

                    // Goller için özet string hazırla
                    string goalDetails = "";
                    foreach (var goal in goals.OrderBy(g => g.Minute))
                    {
                        var scorer = $"{goal.Player?.FirstName} {goal.Player?.LastName}".Trim();
                        var assist = goal.AssistPlayer != null ? $"(Asist: {goal.AssistPlayer.FirstName} {goal.AssistPlayer.LastName})" : "";
                        var type = goal.IsOwnGoal ? " (Kendi Kalesine)" : goal.IsPenalty ? " (Penaltı)" : "";
                        goalDetails += $"{goal.Minute}. dk: {scorer} {assist}{type}\n";
                    }
                    if (string.IsNullOrWhiteSpace(goalDetails)) goalDetails = "Gol yok.\n";

                    // Kartlar için özet string hazırla
                    string cardDetails = "";
                    foreach (var card in cards.OrderBy(c => c.Minute))
                    {
                        var player = $"{card.Player?.FirstName} {card.Player?.LastName}".Trim();
                        var cardType = card.CardType == CardType.Yellow ? "Sarı Kart" : "Kırmızı Kart";
                        cardDetails += $"{card.Minute}. dk: {player} - {cardType}\n";
                    }
                    if (string.IsNullOrWhiteSpace(cardDetails)) cardDetails = "Kart yok.\n";

                    try
                    {
                        var news = await _openAIManager.GenerateMatchSummaryAsync(homeTeam, awayTeam, homeScore, awayScore, goalDetails, cardDetails);

                        // Gerekli bilgileri alıyoruz
                        var homeTeamName = match.HomeTeam.Name;
                        var awayTeamName = match.AwayTeam.Name;
                        var score = $"{match.HomeScore ?? 0} - {match.AwayScore ?? 0}";
                        var matchDateTime = match.MatchDate.ToString("dd MMMM dddd - HH:mm", new System.Globalization.CultureInfo("tr-TR"));

                        // YENİ BAŞLIK: İsteğiniz üzerine başlığı formatlıyoruz.
                        string title = $"Maç Sonu: {homeTeamName} {score} {awayTeamName}";

                        // YENİ ALT BAŞLIK: OpenAI metninin ilk satırını alt başlık yapalım.
                        // Bu, haberin en ilgi çekici kısmını öne çıkarır.
                        string subtitle = news.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";

                        // MatchNews nesnesi oluştur
                        var matchNews = new MatchNews
                        {
                            Title = title,
                            Subtitle = subtitle,
                            DetailsTitle = title, // Detay başlığını da ana başlıkla aynı yapıyoruz. 
                            Details = news,
                            CreatedDate = DateTime.UtcNow,
                            Published = true,
                            IsMainNews = false,
                            CityID = match.HomeTeam.CityID,
                            TeamID = match.HomeTeamID,
                            MatchNewsMainPhoto = "https://minifutbolturkiye.agx-labs.com/images/matchcardresult.jpg"
                        };

                        _context.Set<MatchNews>().Add(matchNews);

                        await _context.SaveChangesAsync();

                        _logger.LogInformation($"Oluşturulan maç özeti MatchNews tablosuna kaydedildi. NewsId: {matchNews.Id}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "OpenAI ile maç özeti oluşturulurken veya MatchNews kaydedilirken hata oluştu.");
                    }



                }
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Maç durumu başarıyla güncellendi." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Maç durumu güncellenirken hata oluştu. MatchId: {MatchId}", model.MatchId);
                return Json(new { success = false, message = "Maç durumu güncellenirken bir sunucu hatası oluştu." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> MatchSquad(int matchId)
        {
            var match = await _context.Matches
                .Select(m => new
                {
                    m.MatchID,
                    HomeTeam = new Team
                    {
                        TeamID = m.HomeTeam.TeamID,
                        Name = m.HomeTeam.Name,
                        Players = m.HomeTeam.Players
                            .Where(p => p.isArchived != true)
                            .Select(p => new Player
                            {
                                PlayerID = p.PlayerID,
                                TeamID = p.TeamID,
                                Number = p.Number,
                                FirstName = p.FirstName,
                                LastName = p.LastName,
                                Icon = p.Icon
                            })
                            .OrderBy(p => p.Number)
                            .ToList()
                    },
                    AwayTeam = new Team
                    {
                        TeamID = m.AwayTeam.TeamID,
                        Name = m.AwayTeam.Name,
                        Players = m.AwayTeam.Players
                        .Where(p => p.isArchived != true)
                            .Select(p => new Player
                            {
                                PlayerID = p.PlayerID,
                                TeamID = p.TeamID,
                                Number = p.Number,
                                FirstName = p.FirstName,
                                LastName = p.LastName,
                                Icon = p.Icon
                            })
                            .OrderBy(p => p.Number)
                            .ToList()
                    },
                    MatchSquads = m.MatchSquads.Select(ms => new

                    {
                        ms.PlayerID,
                        ms.TeamID,
                        ms.ShirtNumber,
                        ms.IsStarting11,
                        ms.IsSubstitute,
                        ms.TopPosition,
                        ms.LeftPosition
                    })
                })
                .FirstOrDefaultAsync(m => m.MatchID == matchId);

            if (match == null)
                return NotFound();

            var viewModel = new DashBoardMatchSquadViewModel
            {
                MatchId = match.MatchID,
                HomeTeam = match.HomeTeam,
                AwayTeam = match.AwayTeam,
                HomeSquad = match.MatchSquads
                    .Where(ms => ms.TeamID == match.HomeTeam.TeamID)
                    .Select(ms => new DashBoardPlayerSquadViewModel
                    {
                        PlayerId = ms.PlayerID,
                        TeamId = ms.TeamID,
                        ShirtNumber = ms.ShirtNumber ?? 0,
                        IsStarting11 = ms.IsStarting11,
                        IsSubstitute = ms.IsSubstitute,
                        PlayerImage = match.HomeTeam.Players.FirstOrDefault(p => p.PlayerID == ms.PlayerID)?.Icon,
                        LeftPosition = ms.LeftPosition,
                        TopPosition = ms.TopPosition,
                        PlayerName = match.HomeTeam.Players.FirstOrDefault(p => p.PlayerID == ms.PlayerID)?.FirstName + " " +
                        match.HomeTeam.Players.FirstOrDefault(p => p.PlayerID == ms.PlayerID)?.LastName,



                    }).ToList(),
                AwaySquad = match.MatchSquads
                    .Where(ms => ms.TeamID == match.AwayTeam.TeamID)
                    .Select(ms => new DashBoardPlayerSquadViewModel
                    {
                        PlayerId = ms.PlayerID,
                        TeamId = ms.TeamID,
                        ShirtNumber = ms.ShirtNumber ?? 0,
                        IsStarting11 = ms.IsStarting11,
                        IsSubstitute = ms.IsSubstitute,
                        PlayerImage = match.AwayTeam.Players.FirstOrDefault(p => p.PlayerID == ms.PlayerID)?.Icon,
                        LeftPosition = ms.LeftPosition,
                        TopPosition = ms.TopPosition,
                        PlayerName = match.AwayTeam.Players.FirstOrDefault(p => p.PlayerID == ms.PlayerID)?.FirstName + " " +
                        match.AwayTeam.Players.FirstOrDefault(p => p.PlayerID == ms.PlayerID)?.LastName,
                    }).ToList()
            };

            return View(viewModel);
        }

        // UpdateMatchStatus için istek modelini tanımlayın (Controller sınıfının dışında veya içinde olabilir)
        public class UpdateMatchStatusRequest
        {
            [Required]
            public int MatchId { get; set; }
            [Required]
            public int Status { get; set; } // JS'den gelen int değeri alır
        }

        // Takım düzenleme (GET)
        public async Task<IActionResult> EditTeam(int teamId)
        {
            var team = await _context.Teams.FindAsync(teamId);
            
            if (team.TeamIsFree==true)
            {
                TempData["Error"] = "Serbest takım güncellenemez.";
                return RedirectToAction("Teams");
            }


            if (team == null)
            {
                TempData["Error"] = "Takım bulunamadı.";
                return RedirectToAction("Teams");
            }

            var cities = await _context.City
                .OrderBy(c => c.Name)
                .Select(c => new SelectListItem
                {
                    Value = c.CityID.ToString(),
                    Text = c.Name
                })
                .ToListAsync();

            var viewModel = new TeamViewModel
            {
                TeamID = team.TeamID,
                Name = team.Name,
                CityID = team.CityID,
                Stadium = team.Stadium,
                ExistingLogoUrl = team.LogoUrl,
                Manager = team.Manager,
                TeamPassword = team.TeamPassword,
                AvailableCities = cities
            };

            return View(viewModel);
        }

        // Takım düzenleme (POST)
        [HttpPost]
        public async Task<IActionResult> EditTeam(TeamViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.AvailableCities = await _context.City
                    .OrderBy(c => c.Name)
                    .Select(c => new SelectListItem
                    {
                        Value = c.CityID.ToString(),
                        Text = c.Name
                    })
                    .ToListAsync();
                return View(model);
            }

            var team = await _context.Teams.FindAsync(model.TeamID);
            if (team == null)
            {
                TempData["Error"] = "Takım bulunamadı.";
                return RedirectToAction("Teams");
            }

            team.Name = model.Name;
            team.CityID = model.CityID;
            team.Stadium = model.Stadium;
            team.Manager = model.Manager;
            team.TeamPassword = model.TeamPassword;

            // Logo güncellendi mi?
            if (model.Logo != null && model.Logo.Length > 0)
            {
                var key = $"teamimages/{Guid.NewGuid()}{Path.GetExtension(model.Logo.FileName)}";
                using var stream = model.Logo.OpenReadStream();
                await _r2Manager.UploadFileAsync(key, stream, model.Logo.ContentType);
                team.LogoUrl = _r2Manager.GetFileUrl(key);
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Takım başarıyla güncellendi.";
            return RedirectToAction("Teams");
        }

        [HttpPost]
        public async Task<IActionResult> SetCaptain(int playerId, bool isCaptain)
        {
            var player = await _context.Players.FindAsync(playerId);
            if (player == null)
            {
                TempData["Error"] = "Oyuncu bulunamadı.";
                return RedirectToAction("TeamDetails", new { id = player?.TeamID });
            }

            // Eğer oyuncunun bir User kaydı varsa
            if (player.UserId != null)
            {
                var user = await _context.Users.FindAsync(player.UserId);
                if (user != null)
                {
                    // UserRole ve UserType'ı güncelle
                    user.UserRole = isCaptain ? "Captain" : "Player";
                    user.UserType = isCaptain ? UserType.Captain : UserType.Player;

                    // IdentityRole tablosu ile ilişkili ise rolleri güncelle
                    var userRoles = _context.UserRoles.Where(ur => ur.UserId == user.Id);
                    _context.UserRoles.RemoveRange(userRoles);

                    if (isCaptain)
                    {
                        // Captain rolünü bul veya oluştur
                        var captainRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Captain");
                        if (captainRole == null)
                        {
                            captainRole = new Microsoft.AspNetCore.Identity.IdentityRole { Name = "Captain", NormalizedName = "CAPTAIN" };
                            _context.Roles.Add(captainRole);
                            await _context.SaveChangesAsync();
                        }
                        // Captain rolünü ata
                        _context.UserRoles.Add(new Microsoft.AspNetCore.Identity.IdentityUserRole<string>
                        {
                            UserId = user.Id,
                            RoleId = captainRole.Id
                        });
                    }
                    else
                    {
                        // Player rolünü bul veya oluştur
                        var playerRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Player");
                        if (playerRole == null)
                        {
                            playerRole = new Microsoft.AspNetCore.Identity.IdentityRole { Name = "Player", NormalizedName = "PLAYER" };
                            _context.Roles.Add(playerRole);
                            await _context.SaveChangesAsync();
                        }
                        // Player rolünü ata
                        _context.UserRoles.Add(new Microsoft.AspNetCore.Identity.IdentityUserRole<string>
                        {
                            UserId = user.Id,
                            RoleId = playerRole.Id
                        });
                    }
                }
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = isCaptain ? "Oyuncu kaptan yapıldı." : "Oyuncunun kaptanlığı kaldırıldı.";
            return RedirectToAction("TeamDetails", new { id = player.TeamID });
        }

        [HttpPost]
        public async Task<IActionResult> TogglePlayerArchive(int playerId)
        {
            try
            {
                var player = await _context.Players.FirstOrDefaultAsync(p => p.PlayerID == playerId);

                if (player == null)
                {
                    TempData["Error"] = "Oyuncu bulunamadı.";
                    return RedirectToAction("TeamDetails", new { id = player?.TeamID });
                }

                // Arşiv durumunu tersine çevir
                player.isArchived = !player.isArchived;
                await _context.SaveChangesAsync();

                string message = player.isArchived ? "Oyuncu arşive alındı." : "Oyuncu arşivden çıkarıldı.";
                TempData["Success"] = message;

                return RedirectToAction("TeamDetails", new { id = player.TeamID });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Oyuncu arşiv durumu değiştirilirken hata oluştu. PlayerID: {PlayerID}", playerId);
                TempData["Error"] = "Oyuncu arşiv durumu değiştirilirken bir hata oluştu.";
                return RedirectToAction("Teams");
            }
        }
        [HttpPost]
        public IActionResult TogglePlayerLicense(int playerId, bool isLicensed)
        {
            var player = _context.Players.FirstOrDefault(p => p.PlayerID == playerId);
            if (player == null)
            {
                TempData["Error"] = "Oyuncu bulunamadı.";
                return RedirectToAction("TeamDetails", new { id = player.TeamID });
            }

            player.LicensedPlayer = isLicensed;
            _context.SaveChanges();

            TempData["Success"] = "Oyuncu lisans durumu güncellendi.";
            return RedirectToAction("TeamDetails", new { id = player.TeamID });
        }
         
    }

}