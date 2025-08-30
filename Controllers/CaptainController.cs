using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BussinessCupApi.Attributes; // CaptainAuthAttribute için
using BussinessCupApi.Data;
using BussinessCupApi.Models;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;
using BussinessCupApi.ViewModels.Captain; // Yeni ViewModel namespace'i
using Microsoft.AspNetCore.Http;
using BussinessCupApi.Models.UserPlayerTypes;
using BussinessCupApi.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering; // Session için

namespace BussinessCupApi.Controllers
{
    [Authorize(Roles = "Captain")]
   public class CaptainController : Controller
   {
       private readonly ApplicationDbContext _context;
       private readonly ILogger<CaptainController> _logger;
         private readonly UserManager<User> _userManager;
                 private readonly CloudflareR2Manager _r2Manager;


        public CaptainController(ApplicationDbContext context, ILogger<CaptainController> logger, UserManager<User> userManager, CloudflareR2Manager r2Manager)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
            _r2Manager = r2Manager;
        }

       private async Task<(string? UserId, int? TeamId)> GetCurrentUserAndTeamIdAsync()
       {
          var user = await _userManager.GetUserAsync(User); // Bu, ApplicationUser döner
                
                string? userId = user?.Id; // Kullanıcı ID'si

           var player = await _context.Players
                                    .Where(p => p.UserId == userId)
                                    .Select(p => new { p.TeamID })
                                    .FirstOrDefaultAsync();

           if (player == null || player.TeamID <= 0)
           {
               return (userId, null); // Kullanıcı bulundu ama takımı yok/geçersiz
           }

           return (userId, player.TeamID);
       }

       public async Task<IActionResult> Dashboard()
       {
           var (userId, teamId) = await GetCurrentUserAndTeamIdAsync();

           if (String.IsNullOrEmpty(userId))
           {
               _logger.LogWarning("Captain Dashboard: User ID not found in session.");
               return RedirectToAction("Login", "Account"); // Veya hata sayfası
           }

           if (!teamId.HasValue)
           { 
               // Kullanıcıya takımının atanmadığına dair bir mesaj gösterilebilir.
               TempData["Error"] = "Sisteme kayıtlı bir takımınız bulunamadı. Lütfen yöneticinizle iletişime geçin.";
               return View(new CaptainDashboardViewModel { Matches = new List<CaptainMatchViewModel>() }); // Boş model ile view'ı göster
           }

           try
           {
               var matches = await _context.Matches
                   .Where(m => m.HomeTeamID == teamId.Value || m.AwayTeamID == teamId.Value)
                   .Include(m => m.HomeTeam)
                   .Include(m => m.AwayTeam)
                   .Include(m => m.Week)
                       .ThenInclude(w => w.Season)
                           .ThenInclude(s => s.League)
                   .OrderByDescending(m => m.MatchDate)
                   .Select(m => new CaptainMatchViewModel
                   {
                       MatchID = m.MatchID,
                       HomeTeamName = m.HomeTeam.Name,
                       AwayTeamName = m.AwayTeam.Name,
                       HomeScore = m.HomeScore,
                       AwayScore = m.AwayScore,
                       MatchDate = m.MatchDate,
                       IsPlayed = m.Status == Match.MatchStatus.Finished,
                       LeagueName = m.Week.Season.League.Name,
                       SeasonName = m.Week.Season.Name,
                       WeekNumber = m.Week.WeekNumber,
                       IsCaptainHomeTeam = m.HomeTeamID == teamId.Value
                   })
                   .ToListAsync();

               var viewModel = new CaptainDashboardViewModel
               {
                   Matches = matches,
                   CaptainTeamId = teamId.Value
               };

               return View(viewModel);
           }
           catch (Exception ex)
           {
                 TempData["Error"] = "Dashboard yüklenirken bir hata oluştu.";
               return View(new CaptainDashboardViewModel { Matches = new List<CaptainMatchViewModel>() });
           }
       }

       // Kadro Düzenleme Action (GET)
       [HttpGet]
       public async Task<IActionResult> EditSquad(int matchId)
       {
           var (userId, teamId) = await GetCurrentUserAndTeamIdAsync();      



           if (String.IsNullOrEmpty(userId) || !teamId.HasValue)
            {
                return RedirectToAction("Dashboard"); // Gerekli bilgiler yoksa dashboard'a yönlendir
            }

       var match = await _context.Matches
    .Include(m => m.HomeTeam)
    .Include(m => m.AwayTeam)
    .Include(m => m.League) // Bunu ekleyin!
    .FirstOrDefaultAsync(m => m.MatchID == matchId && (m.HomeTeamID == teamId.Value || m.AwayTeamID == teamId.Value));
if (match != null)
{
    ViewBag.LeagueId = match.LeagueID;
    ViewBag.TeamSquadCount = match.League?.TeamSquadCount ?? 11; // Varsayılan 11
}
            if (match == null)
            {
                TempData["Error"] = "Maç bulunamadı veya bu maça erişim yetkiniz yok.";
                return RedirectToAction("Dashboard");
            }

           // Kaptanın takımının oyuncularını al
           var captainTeamPlayers = await _context.Players
               .Where(p => p.TeamID == teamId.Value)
               .Where(p => p.isArchived != true)
               .OrderBy(p => p.Number ?? 999) // Önce numaralılar, sonra numarasızlar
               .ThenBy(p => p.LastName)
               .Select(p => new CaptainPlayerViewModel
               {
                   PlayerID = p.PlayerID,
                   FirstName = p.FirstName,
                   LastName = p.LastName,
                   Number = p.Number
               })
               .ToListAsync();

           // Mevcut kadro bilgilerini al (sadece kaptanın takımı için)
           var existingSquad = await _context.MatchSquads
               .Where(ms => ms.MatchID == matchId && ms.TeamID == teamId.Value)
               .Select(ms => new CaptainPlayerSquadViewModel
               {
                   PlayerId = ms.PlayerID,
                   TeamId = ms.TeamID,
                   ShirtNumber = ms.ShirtNumber ?? 0,
                   IsStarting11 = ms.IsStarting11,
                   IsSubstitute = ms.IsSubstitute // Yedek bilgisi de gerekebilir
               })
               .ToListAsync();

           // ViewModel'i hazırla
           var viewModel = new CaptainSquadViewModel
           {
               MatchId = matchId,
               MatchDescription = $"{match.HomeTeam.Name} vs {match.AwayTeam.Name} ({match.MatchDate:dd.MM.yyyy})",
               CaptainTeamId = teamId.Value,
               CaptainTeamName = (match.HomeTeamID == teamId.Value) ? match.HomeTeam.Name : match.AwayTeam.Name,
               AvailablePlayers = captainTeamPlayers,
               CurrentSquad = existingSquad
           };


           return View(viewModel);
       }

       // Kadro Kaydetme Action (POST)
       [HttpPost]
       [ValidateAntiForgeryToken] // CSRF koruması ekleyelim
       public async Task<IActionResult> SaveSquad(CaptainSquadViewModel model)
       {
           var (userId, teamId) = await GetCurrentUserAndTeamIdAsync();

           if (String.IsNullOrEmpty(userId) || !teamId.HasValue)
           {
                return Unauthorized(); // Yetkisiz erişim
           }

           if (model.CaptainTeamId != teamId.Value)
           {
                return Unauthorized(); // Başka takımın kadrosunu kaydetmeye çalışıyor!
           }

           var match = await _context.Matches.FindAsync(model.MatchId);
           if (match == null || (match.HomeTeamID != teamId.Value && match.AwayTeamID != teamId.Value))
           {
                TempData["Error"] = "Maç bulunamadı veya bu maça erişim yetkiniz yok.";
               return RedirectToAction("Dashboard");
           }

           // Kadro sayısı kontrolü (örneğin 11 as, 7 yedek gibi limitler varsa)
           var startingCount = model.SelectedSquadPlayers?.Count(p => p.IsStarting11) ?? 0;
           // Gerekirse burada daha fazla validasyon eklenebilir (Örn: max yedek sayısı vb.)
           if (startingCount != 11)
           {
               TempData["Error"] = "İlk 11'de tam olarak 11 oyuncu seçilmelidir.";
               // Modeli View'a geri gönderirken oyuncu listesini tekrar doldurmamız gerekebilir.
               // Bu kısmı daha detaylı handle etmek için modeli tekrar doldurup View'a dönmek daha iyi olabilir.
               // Şimdilik dashboard'a yönlendirelim.
               return RedirectToAction("EditSquad", new { matchId = model.MatchId });
           }


           try
           {
               // 1. Bu maç için kaptanın takımına ait eski kadro kayıtlarını sil
               var existingSquadEntries = await _context.MatchSquads
                   .Where(ms => ms.MatchID == model.MatchId && ms.TeamID == teamId.Value)
                   .ToListAsync();
               _context.MatchSquads.RemoveRange(existingSquadEntries);

               // 2. Gelen yeni kadroyu ekle
               if (model.SelectedSquadPlayers != null)
               {
                   var newSquadEntries = model.SelectedSquadPlayers.Select(p => new MatchSquad
                   {
                       MatchID = model.MatchId,
                       PlayerID = p.PlayerId,
                       TeamID = teamId.Value, // KESİNLİKLE kaptanın kendi TeamID'si olmalı
                       ShirtNumber = p.ShirtNumber,
                       IsStarting11 = p.IsStarting11,
                       IsSubstitute = p.IsSubstitute // As değilse yedektir varsayımı (veya ayrı bir checkbox eklenebilir)
                   }).Where(x => x.IsSubstitute == true || x.IsStarting11 == true).ToList();

                   await _context.MatchSquads.AddRangeAsync(newSquadEntries);
               }

               await _context.SaveChangesAsync();
               TempData["Success"] = "Kadro başarıyla güncellendi.";
               return RedirectToAction("Dashboard");
           }
           catch (Exception ex)
           {
                TempData["Error"] = "Kadro kaydedilirken bir hata oluştu.";
               // Hata durumunda EditSquad sayfasına geri dönmek daha kullanıcı dostu olabilir.
               return RedirectToAction("EditSquad", new { matchId = model.MatchId });
           }
       }

       public async Task<IActionResult> TeamDetails()
       {
           try
           {
               var (userId, teamId) = await GetCurrentUserAndTeamIdAsync();
       
           if (String.IsNullOrEmpty(userId) || !teamId.HasValue)
               {
                   return RedirectToAction("Dashboard"); // Gerekli bilgiler yoksa dashboard'a yönlendir
               }
               var team = await _context.Teams
                   .Select(t => new TeamDetailsViewModel
                   {
                       TeamID = t.TeamID,
                       Name = t.Name ?? "",
                       Stadium = t.Stadium ?? "",
                       Manager = t.Manager ?? "",
                       Players = t.Players
                           .Where(p => p != null)
                           .Select(p => new PlayerListViewModel
                           {
                               PlayerID = p.PlayerID,
                               FirstName = p.FirstName ?? "",
                               LastName = p.LastName ?? "",
                               Position = p.Position ?? "",
                               Number = p.Number ?? 0,
                               Icon = p.Icon ?? "",
                               IsArchived = p.isArchived,
                               FrontIDImageUrl = p.FrontIdentityImage ?? "",
                               BackIDImageUrl = p.BackIdentityImage ?? ""
                           })
                           .Where(x => x.IsArchived != true)
                           .OrderBy(p => p.Number)
                           .ToList()
                   })
                   .FirstOrDefaultAsync(t => t.TeamID == teamId);


               return View(team);
           }
           catch (Exception ex)
           {
               TempData["Error"] = "Takım detayları yüklenirken bir hata oluştu: " + ex.Message;
               return RedirectToAction("Dashboard");
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
                    TeamID = Convert.ToInt32(player.Team) // Player modelinde TeamID nullable ise kontrol ekle
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
                        return RedirectToAction("TeamDetails"); // Veya Dashboard
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
                    // player.TeamID alanı modelden geliyorsa güncellenebilir, ancak bu formda TeamID değiştirme yok gibi duruyor.
                   


                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Oyuncu başarıyla güncellendi.";
                    // Oyuncunun ait olduğu takımın detaylarına yönlendir

                    return RedirectToAction("TeamDetails"); // Veya Dashboard

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

         public async Task<IActionResult> EditTeam()
        {
            var (userId, teamId) = await GetCurrentUserAndTeamIdAsync();

                var team = await _context.Teams.FindAsync(teamId);
                if (team == null)
                {
                    TempData["Error"] = "Takım bulunamadı.";
                    return RedirectToAction("TeamDetails");
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
                return RedirectToAction("TeamDetails");
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
            return RedirectToAction("TeamDetails");
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
       
   }
}