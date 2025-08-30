using BussinessCupApi.Data;
using BussinessCupApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Threading.Tasks;
using BussinessCupApi.Attributes;
using Microsoft.AspNetCore.Mvc.Rendering;
using BussinessCupApi.ViewModels;
using Microsoft.AspNetCore.Authorization;

namespace BussinessCupApi.Controllers
{
    [Authorize(Roles = "Admin,CityAdmin")]

    public class LeagueController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<LeagueController> _logger;
        private readonly CloudflareR2Manager _r2Manager;

        public LeagueController(ApplicationDbContext context, IConfiguration configuration,
            ILogger<LeagueController> logger, IWebHostEnvironment webHostEnvironment, CloudflareR2Manager r2Manager)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
            _r2Manager = r2Manager;
        }


        [HttpGet]
        public IActionResult CreateLeague()
        {
            // Controller Action içinde
            var cities = _context.City.Select(c => new SelectListItem
            {
                Value = c.CityID.ToString(),
                Text = c.Name
            }).ToList();


            var model = new CreateLeagueViewModel
            {
                Cities = cities,
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddMonths(9) // Varsayılan olarak 9 aylık bir sezon
            };
            return PartialView("_CreateLeague", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateLeague(CreateLeagueInputModel model)
        {
            if (ModelState.IsValid)
            {
                string? uniqueFileName = null;
                if (model.LogoFile != null)
                {
                    var key = $"leagueimages/{Guid.NewGuid()}{Path.GetExtension(model.LogoFile.FileName)}";

                    using var stream = model.LogoFile.OpenReadStream();
                    await _r2Manager.UploadFileAsync(key, stream, model.LogoFile.ContentType);


                    uniqueFileName = _r2Manager.GetFileUrl(key);
                }

                var league = new League
                {
                    Name = model.Name,
                    LeagueType = model.LeagueType,
                    StartDate = model.StartDate,
                    EndDate = model.EndDate,
                    IsActive = true,
                    LogoPath = uniqueFileName,
                    CityID = model.CityID,
                    TeamSquadCount = model.TeamSquadCount

                };

                _context.Leagues.Add(league);
                await _context.SaveChangesAsync(); 

                if (model.RankingStatuses != null && model.RankingStatuses.Any())
                {
                    foreach (var status in model.RankingStatuses)
                    {
                        var rankingStatus = new LeagueRankingStatus
                        {
                            LeagueID = league.LeagueID,
                            OrderNo = status.OrderNo,
                            ColorCode = status.ColorCode,
                            Description = status.Description
                        };
                        _context.LeagueRankingStatus.Add(rankingStatus);
                    }
                    await _context.SaveChangesAsync();
                }

                return RedirectToAction("Dashboard", "Admin");
            }

            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            _logger.LogWarning("Lig oluşturma modeli geçerli değil. Hatalar: {Errors}", string.Join(", ", errors));
            return PartialView("_CreateLeague", model);
        }

        [HttpGet]
        public async Task<IActionResult> EditLeague(int leagueId)
        {
            var league = await _context.Leagues.FindAsync(leagueId);
            if (league == null)
                return NotFound();

            var cities = _context.City.Select(c => new SelectListItem
            {
                Value = c.CityID.ToString(),
                Text = c.Name
            }).ToList();

            // RankingStatuses'i çek
            var rankingStatuses = await _context.LeagueRankingStatus
                .Where(x => x.LeagueID == leagueId)
                .OrderBy(x => x.OrderNo)
                .Select(x => new RankingStatusViewModel
                { 
                    OrderNo = x.OrderNo,
                    ColorCode = x.ColorCode,
                    Description = x.Description
                }).ToListAsync();

            var model = new EditLeagueViewModel
            {
                LeagueID = league.LeagueID,
                Name = league.Name,
                LeagueType = league.LeagueType,
                StartDate = league.StartDate,
                EndDate = league.EndDate,
                ExistingLogoPath = league.LogoPath,
                CityID = league.CityID,
                TeamSquadCount = league.TeamSquadCount,
                Cities = cities,
                RankingStatuses = rankingStatuses // <-- burası önemli
            };

            return PartialView("_EditLeague", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditLeague(EditLeagueInputViewModel model)
        {
            _logger.LogInformation("Lig düzenleme isteği alındı. ID: {LeagueID}", model.LeagueID);

            if (ModelState.IsValid)
            {
                var league = await _context.Leagues.FindAsync(model.LeagueID);
                if (league == null)
                {
                    _logger.LogWarning("Düzenlenecek lig bulunamadı. ID: {LeagueId}", model.LeagueID);
                    return NotFound();
                }

                // Check if there are any matches for this league
                bool hasMatches = await _context.Set<Match>().AnyAsync(m => m.LeagueID == league.LeagueID);

                // Prevent changing LeagueType if matches exist
                if (hasMatches && league.LeagueType != model.LeagueType)
                {
                    TempData["Error"] = "Ligde maçlar bulunduğu için lig türü değiştirilemez.";
                    return RedirectToAction("Dashboard", "Admin");

                }

                if (model.NewLogoFile != null)
                {
                    var key = $"leagueimages/{Guid.NewGuid()}{Path.GetExtension(model.NewLogoFile.FileName)}";
                    using var stream = model.NewLogoFile.OpenReadStream();
                    await _r2Manager.UploadFileAsync(key, stream, model.NewLogoFile.ContentType);
                    league.LogoPath = _r2Manager.GetFileUrl(key);
                }
                else
                {
                    _logger.LogInformation("Yeni logo yüklenmedi, mevcut logo korunuyor.");
                }

                league.Name = model.Name;
                league.StartDate = model.StartDate;
                league.EndDate = model.EndDate;
                league.TeamSquadCount = model.TeamSquadCount;
                league.CityID = model.CityID;

                try
                {
                    _context.Update(league);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Lig güncellendi: {LeagueName} (ID: {LeagueID})", league.Name, league.LeagueID);
                    // ... Lig güncelleme işlemleri ...
                    // 1. Eski statüleri sil
                    var oldStatuses = _context.LeagueRankingStatus.Where(x => x.LeagueID == model.LeagueID);
                    _context.LeagueRankingStatus.RemoveRange(oldStatuses);
                    await _context.SaveChangesAsync();

                    // 2. Yeni statüleri ekle
                    if (model.RankingStatuses != null && model.RankingStatuses.Any())
                    {
                        foreach (var status in model.RankingStatuses)
                        {
                            var rankingStatus = new LeagueRankingStatus
                            {
                                LeagueID = model.LeagueID,
                                OrderNo = status.OrderNo,
                                ColorCode = status.ColorCode,
                                Description = status.Description
                            };
                            _context.LeagueRankingStatus.Add(rankingStatus);
                        }
                        await _context.SaveChangesAsync();
                    }

                    return RedirectToAction("Dashboard", "Admin");
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    _logger.LogError(ex, "Lig güncellenirken concurrency hatası oluştu. ID: {LeagueID}", model.LeagueID);
                    ModelState.AddModelError(string.Empty, "Lig bilgileri başka bir kullanıcı tarafından güncellendi. Lütfen sayfayı yenileyip tekrar deneyin.");
                    model.ExistingLogoPath = league.LogoPath;
                    return PartialView("_EditLeague", model);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lig güncellenirken beklenmedik bir hata oluştu. ID: {LeagueID}", model.LeagueID);
                    ModelState.AddModelError(string.Empty, "Lig güncellenirken bir hata oluştu. Lütfen tekrar deneyin.");
                    model.ExistingLogoPath = league.LogoPath;
                    return PartialView("_EditLeague", model);
                }
            }

            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            _logger.LogWarning("Lig düzenleme modeli geçerli değil. ID: {LeagueID}, Hatalar: {Errors}", model.LeagueID, string.Join(", ", errors));
            if (!ModelState.IsValid && model.LeagueID > 0)
            {
                var originalLeague = await _context.Leagues.AsNoTracking().FirstOrDefaultAsync(l => l.LeagueID == model.LeagueID);
                if (originalLeague != null)
                {
                    model.ExistingLogoPath = originalLeague.LogoPath;
                }
            }
            return PartialView("_EditLeague", model);
        }

        [HttpGet]
        public async Task<IActionResult> ManageFixture(int leagueId)
        {
            var league = await _context.Leagues
                                     .FirstOrDefaultAsync(l => l.LeagueID == leagueId);

            if (league == null)
            {
                _logger.LogWarning("Fikstür yönetimi için lig bulunamadı. ID: {LeagueId}", leagueId);
                return NotFound();
            }

            // Ligin bulunduğu şehirdeki tüm takımları çek
            var teamsInCity = await _context.Teams.Where(t => t.CityID == league.CityID && t.TeamIsFree!=true).ToListAsync();

            // Gruplar lig ile doğrudan ilişkiliyse ve sıfırdan oluşturulacaksa boş liste gönderilebilir.
            // Eğer gruplar için farklı bir mantık varsa (örn: şehre göre ön tanımlı gruplar vb.) ona göre düzenlenmeli.
            // Şimdilik, kullanıcı her şeyi sıfırdan oluşturacağı için boş liste varsayıyoruz.
            var groups = new List<Group>(); // Ya da _context.Group.Where(g => g.LeagueID == leagueId).ToList() kalabilir,
                                            // ancak kullanıcı "her şeyi bu simulatörde biz oluşturacağız" dediği için
                                            // mevcut grupları yüklemek yerine boş başlatmak daha uygun olabilir.
                                            // Eğer mevcut grupları bir şablon olarak göstermek isteniyorsa eski hali kalabilir.
                                            // Kullanıcının isteğine göre burası netleştirilebilir. Şimdilik boş liste olarak güncelliyorum.


            var model = new ManageFixtureViewModel
            {
                LeagueId = league.LeagueID,
                LeagueName = league.Name,
                LeagueType = league.LeagueType,
                Teams = teamsInCity, // Şehirdeki tüm takımlar
                TeamSquadCount = league.TeamSquadCount,
                LeagueStartDate = league.StartDate,
                LeagueEndDate = league.EndDate,
                ExistingWeeks = new List<Week>(), // Haftalar sıfırdan oluşturulacak
                ExistingGroups = groups, // Gruplar sıfırdan oluşturulacak (veya yukarıdaki nota göre düzenlenecek)
            };

            if (league.LeagueType == LeagueType.GroupLeagueThenKnockout && !groups.Any())
            {
                // Eğer gruplu lig ve hiç grup yoksa, örnek bir değer önerelim veya kullanıcıdan isteyelim.
                // Örneğin, takım sayısına göre bir mantık kurulabilir. Şimdilik boş bırakalım.
            }

            return View("ManageFixture", model);
        }

        // ... (using ifadeleri ve class tanımı aynı) ...

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateFixture(GenerateFixtureInputModel model)
        {
            _logger.LogInformation("Fikstür oluşturma isteği alındı. Lig ID: {LeagueId}, Sezon Adı: {SeasonName}, Formdan Gelen IsGroupLeague: {IsGroupLeague}, Formdan Gelen Grup Sayısı: {NumGroups}",
                model.LeagueId, model.SeasonName, model.IsGroupLeague, model.NumberOfGroups);

            var league = await _context.Leagues.FindAsync(model.LeagueId);
            if (league == null)
            {
                _logger.LogWarning("Fikstür oluşturulacak lig bulunamadı. ID: {LeagueId}", model.LeagueId);
                TempData["ErrorMessage"] = "Fikstür oluşturulacak lig bulunamadı.";
                return RedirectToAction("ManageFixture", new { leagueId = model.LeagueId });
            }

            // Lig tipine göre gruplu olup olmayacağını kesin olarak belirle
            // Sadece GroupLeagueThenKnockout tipi grupludur.
            bool isTrulyGroupLeagueBasedOnType = league.LeagueType == LeagueType.GroupLeagueThenKnockout;

            if (isTrulyGroupLeagueBasedOnType)
            {
                // Lig tipi 'GroupLeagueThenKnockout' ise, model.IsGroupLeague true olmalı.
                if (!model.IsGroupLeague)
                {
                    _logger.LogInformation("Lig tipi {LeagueType} (ID: {LeagueId}) gruplu olmayı gerektiriyor ancak formdan IsGroupLeague=false geldi. IsGroupLeague=true olarak güncellendi.", league.LeagueType, league.LeagueID);
                    model.IsGroupLeague = true;
                }
                // NumberOfGroups için validasyon aşağıda yapılacak.
            }
            else // Lig tipi gruplu DEĞİL (League, Knockout, LeagueThenKnockout)
            {
                if (model.IsGroupLeague)
                {
                    _logger.LogInformation("Lig tipi {LeagueType} (ID: {LeagueId}) gruplu bir tip değil ancak formdan IsGroupLeague=true geldi. IsGroupLeague=false ve NumberOfGroups=0 olarak ayarlanıyor.", league.LeagueType, league.LeagueID);
                    model.IsGroupLeague = false;
                }
                model.NumberOfGroups = 0; // Grupsuz liglerde grup sayısı her zaman 0'dır.
            }

            // Model State Validasyonu (model.IsGroupLeague ve model.NumberOfGroups'un güncel değerlerine göre)
            if (model.SelectedTeamIds == null || model.SelectedTeamIds.Count < 2)
            {
                ModelState.AddModelError(nameof(model.SelectedTeamIds), "Fikstür oluşturmak için en az 2 takım seçmelisiniz.");
            }

            if (model.IsGroupLeague) // Bu blok sadece isTrulyGroupLeagueBasedOnType == true ise çalışacak
            {
                if (model.NumberOfGroups < 2)
                {
                    ModelState.AddModelError(nameof(model.NumberOfGroups), "Gruplu lig seçildiğinde grup sayısı en az 2 olmalıdır.");
                }
                else if (model.SelectedTeamIds != null && model.SelectedTeamIds.Any()) // model.SelectedTeamIds null değilse ve elemanı varsa
                {
                    if (model.SelectedTeamIds.Count < model.NumberOfGroups * 2)
                    {
                        ModelState.AddModelError(nameof(model.NumberOfGroups), $"Seçilen {model.SelectedTeamIds.Count} takım ile {model.NumberOfGroups} grup oluşturulamaz. Her gruba en az 2 takım düşmelidir.");
                    }
                    else if (model.SelectedTeamIds.Count % model.NumberOfGroups != 0)
                    {
                        ModelState.AddModelError(nameof(model.NumberOfGroups), $"Seçilen {model.SelectedTeamIds.Count} takım sayısı, belirtilen {model.NumberOfGroups} grup sayısına tam bölünmüyor.");
                    }
                }
            }
            // Grupsuz ligler için (model.IsGroupLeague == false), NumberOfGroups zaten 0 olarak ayarlandı,
            // bu nedenle grupla ilgili ek validasyonlara veya else bloğuna burada gerek yok.

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                _logger.LogWarning("Fikstür oluşturma modeli geçerli değil. Lig ID: {LeagueId}, Hatalar: {Errors}", model.LeagueId, string.Join(", ", errors));
                TempData["ErrorMessage"] = "Fikstür oluşturma ayarları geçersiz: " + string.Join("; ", errors);
                return RedirectToAction("ManageFixture", new { leagueId = model.LeagueId });
            }

            // Artık SelectedTeamIds null veya boş değil kontrolü yukarıda yapıldı.

            // 1. Sezon Oluşturma
            var newSeason = new Season
            {
                Name = model.SeasonName,
                LeagueID = model.LeagueId,
                IsActive = true
            };
            _context.Season.Add(newSeason); // _context.Seasons olmalı, ApplicationDbContext'teki DbSet adınızla eşleşmeli
            await _context.SaveChangesAsync();
            _logger.LogInformation("Yeni sezon oluşturuldu: {SeasonName} (ID: {SeasonID}), Lig ID: {LeagueID}", newSeason.Name, newSeason.SeasonID, newSeason.LeagueID);

            List<Group> createdGroups = new List<Group>();
            // 2. Grupları Oluşturma (Sadece IsGroupLeague true ve NumberOfGroups >= 2 ise)
            // Yukarıdaki mantıkla model.IsGroupLeague doğru ayarlandığı için bu blok doğru çalışacaktır.
            if (model.IsGroupLeague && model.NumberOfGroups >= 2)
            {
                for (int i = 0; i < model.NumberOfGroups; i++)
                {
                    var newGroup = new Group
                    {
                        LeagueID = model.LeagueId,
                        GroupName = $"Grup {Convert.ToChar(65 + i)}",
                        Description = $"{model.SeasonName} - Grup {Convert.ToChar(65 + i)}",
                        SeasonID = newSeason.SeasonID
                    };
                    createdGroups.Add(newGroup);
                }
                _context.Group.AddRange(createdGroups);
                await _context.SaveChangesAsync();
                _logger.LogInformation("{NumberOfGroups} adet grup oluşturuldu. Sezon ID: {SeasonID}, Lig ID: {LeagueID}", model.NumberOfGroups, newSeason.SeasonID, model.LeagueId);
            }
            else
            {
                _logger.LogInformation("Gruplu lig seçilmedi veya grup sayısı geçersiz ({NumberOfGroups}), bu nedenle lig (ID: {LeagueID}) için grup oluşturulmadı.", model.NumberOfGroups, model.LeagueId);
            }


            // TAKIMLARI GRUPLARA ATAMA MANTIĞI
            // Yukarıdaki mantıkla model.IsGroupLeague doğru ayarlandığı için bu blok doğru çalışacaktır.
            Dictionary<int, List<int>> groupTeamAssignments = new Dictionary<int, List<int>>();
            if (model.IsGroupLeague && createdGroups.Any()) // Sadece gruplu lig ve gruplar oluşturulduysa ata
            {
                _logger.LogInformation("Takımlar gruplara dağıtılıyor. Lig ID: {LeagueID}", model.LeagueId);
                int teamIndexInSelection = 0;
                foreach (var teamId in model.SelectedTeamIds)
                {
                    Group targetGroup = createdGroups[teamIndexInSelection % model.NumberOfGroups];
                    if (!groupTeamAssignments.ContainsKey(targetGroup.GroupID))
                    {
                        groupTeamAssignments[targetGroup.GroupID] = new List<int>();
                    }
                    groupTeamAssignments[targetGroup.GroupID].Add(teamId);
                    teamIndexInSelection++;
                }

                foreach (var entry in groupTeamAssignments)
                {
                    _logger.LogInformation("Grup ID: {GroupId} - Takım Sayısı: {TeamCount} - Takımlar: {TeamIds}", entry.Key, entry.Value.Count, string.Join(",", entry.Value));
                }
            }

            // ... (Hafta ve Maç oluşturma mantığının geri kalanı büyük ölçüde aynı kalabilir) ...
            // Önemli: Hafta ve maç oluşturma, model.IsGroupLeague ve groupTeamAssignments'ı dikkate alarak çalışmalı.
            // numberOfTeamsForWeekCalculation ve maç oluşturma içindeki gruplu/grupsuz mantığı buna göre çalışacaktır.
            // Eğer model.IsGroupLeague false ise, groupTeamAssignments boş kalacak ve grupsuz mantık devreye girecektir.

            // 3. Haftaları Oluşturma
            int numberOfTeamsForWeekCalculation = model.SelectedTeamIds.Count;
            if (model.IsGroupLeague && groupTeamAssignments.Any() && groupTeamAssignments.Values.Any(g => g.Any()))
            {
                numberOfTeamsForWeekCalculation = groupTeamAssignments.Values.Max(teamsInGroup => teamsInGroup.Count);
                _logger.LogInformation("Grup sistemi aktif (Lig ID: {LeagueID}). Hafta sayısı en büyük gruptaki takım sayısına göre hesaplanacak: {numberOfTeamsInLargestGroup} takım.", model.LeagueId, numberOfTeamsForWeekCalculation);
            }
            // ... (kalan hafta ve maç oluşturma kodları önceki gibi devam eder) ...
            // Sadece _context.Season.Add(newSeason); satırını kontrol edin, _context.Seasons olması daha muhtemel.
            // Bu değişiklikleri yaptıktan sonra ApplicationDbContext dosyanızdaki DbSet<Season> adını teyit edin.
            // Ben `_context.Season.Add` olarak gördüm, eğer bu doğruysa değiştirmedim.

            // ... (önceki yanıttaki hafta ve maç oluşturma mantığı devam eder)
            int numberOfGeneratedWeeks = 0;
            if (numberOfTeamsForWeekCalculation > 1)
            {
                numberOfGeneratedWeeks = (numberOfTeamsForWeekCalculation % 2 == 0) ? numberOfTeamsForWeekCalculation - 1 : numberOfTeamsForWeekCalculation;
                if (model.PlayReturnMatches)
                {
                    numberOfGeneratedWeeks *= 2;
                }
            }
            _logger.LogInformation("Hesaplanan hafta sayısı: {numberOfWeeks}. Referans Takım Sayısı: {numberOfTeams}, Rövanş: {PlayReturnMatches}", numberOfGeneratedWeeks, numberOfTeamsForWeekCalculation, model.PlayReturnMatches);

            List<Week> createdWeeks = new List<Week>();
            for (int i = 1; i <= numberOfGeneratedWeeks; i++)
            {
                var newWeek = new Week
                {
                    LeagueID = model.LeagueId,
                    SeasonID = newSeason.SeasonID,
                    WeekNumber = i,
                    WeekName = $"{i}. Hafta",
                    StartDate = league.StartDate.AddDays((i - 1) * 7),
                    EndDate = league.StartDate.AddDays((i * 7) - 1),
                    IsCompleted = false,
                    WeekStatus = "League"
                };
                createdWeeks.Add(newWeek);
            }
            if (createdWeeks.Any())
            {
                _context.Weeks.AddRange(createdWeeks);
                await _context.SaveChangesAsync();
                _logger.LogInformation("{numberOfWeeks} adet hafta oluşturuldu. Sezon ID: {SeasonID}", createdWeeks.Count, newSeason.SeasonID);
            }
            else
            {
                _logger.LogWarning("Hiç hafta oluşturulmadı. Takım sayısı yetersiz veya bir hata oluştu. Referans Takım Sayısı: {refTeams}", numberOfTeamsForWeekCalculation);
                if (numberOfTeamsForWeekCalculation <= 1 && model.SelectedTeamIds.Count > 1)
                {
                    _logger.LogWarning("Grup içi takım sayısı 1 veya daha az, ancak seçilen toplam takım sayısı > 1. Fikstür mantığı gözden geçirilmeli.");
                }
            }

            List<Match> createdMatches = new List<Match>();
            _logger.LogInformation("Maç oluşturma aşamasına geçildi. Lig ID: {LeagueID}", model.LeagueId);

            if (model.IsGroupLeague && createdGroups.Any() && groupTeamAssignments.Any()) // Gruplu sistem
            {
                _logger.LogInformation("Gruplu sistem için maçlar oluşturuluyor. Lig ID: {LeagueID}. Oluşturulan Hafta Sayısı: {CreatedWeeksCount}", model.LeagueId, createdWeeks.Count);

                foreach (var groupEntry in groupTeamAssignments)
                {
                    int groupId = groupEntry.Key;
                    List<int> teamsInThisGroup = groupEntry.Value;
                    _logger.LogInformation("Grup ID {GroupId} için ({TeamsCount} takım): {TeamsInGroup}", groupId, teamsInThisGroup.Count, string.Join(",", teamsInThisGroup));

                    if (teamsInThisGroup.Count < 2)
                    {
                        _logger.LogWarning("Grup ID {GroupId} ({TeamsCount} takım) için yeterli takım yok, bu grup için maç oluşturulamıyor.", groupId, teamsInThisGroup.Count);
                        continue;
                    }

                    List<int> currentGroupFixtureTeams = new List<int>(teamsInThisGroup);
                    bool isOddTeamCountInGroup = currentGroupFixtureTeams.Count % 2 != 0;
                    if (isOddTeamCountInGroup)
                    {
                        currentGroupFixtureTeams.Add(-1); // Bay için placeholder (-1 geçerli bir TeamID olmamalı)
                    }
                    int numTeamsForBergerInGroup = currentGroupFixtureTeams.Count;
                    int roundsInGroupSingleDevre = numTeamsForBergerInGroup - 1;
                    int matchesPerRoundInGroup = numTeamsForBergerInGroup / 2;

                    _logger.LogInformation("Grup {GroupId}: Takım sayısı (Berger için): {NumTeamsForBergerInGroup}, Tek devreli round sayısı: {RoundsInGroupSingleDevre}, Round başı maç: {MatchesPerRoundInGroup}",
                        groupId, numTeamsForBergerInGroup, roundsInGroupSingleDevre, matchesPerRoundInGroup);

                    if (createdWeeks.Count < roundsInGroupSingleDevre)
                    {
                        _logger.LogWarning("Grup {GroupId} için yetersiz hafta oluşturulmuş. Gereken: {RoundsInGroupSingleDevre}, Mevcut: {CreatedWeeksCount}. Bu grup için tüm 1. devre maçları oluşturulamayabilir.", groupId, roundsInGroupSingleDevre, createdWeeks.Count);
                    }
                    if (model.PlayReturnMatches && createdWeeks.Count < roundsInGroupSingleDevre * 2)
                    {
                        _logger.LogWarning("Grup {GroupId} için rövanş maçları dahil yetersiz hafta oluşturulmuş. Gereken: {RequiredWeeks}, Mevcut: {CreatedWeeksCount}. Bu grup için tüm rövanş maçları oluşturulamayabilir.", groupId, roundsInGroupSingleDevre * 2, createdWeeks.Count);
                    }

                    List<int> initialTeamOrderForGroupReturnMatches = new List<int>(currentGroupFixtureTeams);

                    // 1. Devre Maçları (Grup İçi)
                    for (int round = 0; round < roundsInGroupSingleDevre; round++)
                    {
                        if (round >= createdWeeks.Count)
                        {
                            _logger.LogWarning("Grup {GroupId}, 1. Devre: {RoundPlusOne}. tur için genel hafta kalmadı (Max: {CreatedWeeksCount}).", groupId, round + 1, createdWeeks.Count);
                            break;
                        }
                        Week currentWeek = createdWeeks[round];

                        for (int i = 0; i < matchesPerRoundInGroup; i++)
                        {
                            int homeTeamId = currentGroupFixtureTeams[i];
                            int awayTeamId = currentGroupFixtureTeams[numTeamsForBergerInGroup - 1 - i];

                            if (homeTeamId != -1 && awayTeamId != -1) // Bay olan takımların maçını ekleme
                            {
                                var match = new Match
                                {
                                    LeagueID = model.LeagueId,
                                    WeekID = currentWeek.WeekID,
                                    HomeTeamID = homeTeamId,
                                    AwayTeamID = awayTeamId,
                                    GroupID = groupId,
                                    MatchDate = currentWeek.StartDate.AddDays(round % 7).AddHours(15 + (i % 3)),
                                    IsPlayed = false,
                                    Status = Models.Match.MatchStatus.NotStarted,
                                };
                                createdMatches.Add(match);
                                _logger.LogDebug("Grup {GroupId} 1. Devre Maçı Eklendi: {HomeTeam} vs {AwayTeam}, Hafta: {WeekNumber} ({WeekId}), Round: {Round}", groupId, homeTeamId, awayTeamId, currentWeek.WeekNumber, currentWeek.WeekID, round + 1);
                            }
                        }

                        if (numTeamsForBergerInGroup > 2)
                        {
                            int lastTeam = currentGroupFixtureTeams[numTeamsForBergerInGroup - 1];
                            for (int k = numTeamsForBergerInGroup - 1; k > 1; k--)
                            {
                                currentGroupFixtureTeams[k] = currentGroupFixtureTeams[k - 1];
                            }
                            currentGroupFixtureTeams[1] = lastTeam;
                        }
                    }

                    // 2. Devre Maçları (Grup İçi - Rövanş)
                    if (model.PlayReturnMatches)
                    {
                        currentGroupFixtureTeams = new List<int>(initialTeamOrderForGroupReturnMatches);
                        _logger.LogInformation("Grup {GroupId} için rövanş maçları oluşturuluyor. Başlangıç takım sırası rövanş için sıfırlandı.", groupId);

                        for (int round = 0; round < roundsInGroupSingleDevre; round++)
                        {
                            int overallWeekIndexForReturn = round + roundsInGroupSingleDevre;
                            if (overallWeekIndexForReturn >= createdWeeks.Count)
                            {
                                _logger.LogWarning("Grup {GroupId}, 2. Devre: {RoundPlusOne}. turun rövanşı için genel hafta kalmadı (Genel Hafta Index: {OverallWeekIndexForReturn}, Max: {CreatedWeeksCount}).", groupId, round + 1, overallWeekIndexForReturn, createdWeeks.Count);
                                break;
                            }
                            Week currentReturnWeek = createdWeeks[overallWeekIndexForReturn];

                            for (int i = 0; i < matchesPerRoundInGroup; i++)
                            {
                                int originalHome = currentGroupFixtureTeams[i];
                                int originalAway = currentGroupFixtureTeams[numTeamsForBergerInGroup - 1 - i];

                                if (originalHome != -1 && originalAway != -1)
                                {
                                    var returnMatch = new Match
                                    {
                                        LeagueID = model.LeagueId,
                                        WeekID = currentReturnWeek.WeekID,
                                        HomeTeamID = originalAway,
                                        AwayTeamID = originalHome,
                                        GroupID = groupId,
                                        MatchDate = currentReturnWeek.StartDate.AddDays(round % 7).AddHours(15 + (i % 3)),
                                        IsPlayed = false,
                                        Status = Models.Match.MatchStatus.NotStarted,
                                    };
                                    createdMatches.Add(returnMatch);
                                    _logger.LogDebug("Grup {GroupId} 2. Devre (Rövanş) Maçı Eklendi: {HomeTeam} vs {AwayTeam}, Hafta: {WeekNumber} ({WeekId}), Round: {Round}", groupId, originalAway, originalHome, currentReturnWeek.WeekNumber, currentReturnWeek.WeekID, round + 1);
                                }
                            }
                            if (numTeamsForBergerInGroup > 2)
                            {
                                int lastTeam = currentGroupFixtureTeams[numTeamsForBergerInGroup - 1];
                                for (int k = numTeamsForBergerInGroup - 1; k > 1; k--)
                                {
                                    currentGroupFixtureTeams[k] = currentGroupFixtureTeams[k - 1];
                                }
                                currentGroupFixtureTeams[1] = lastTeam;
                            }
                        }
                    }
                }
            }
            else if (model.SelectedTeamIds != null && model.SelectedTeamIds.Count >= 2) // Grupsuz sistem
            {
                _logger.LogInformation("Grupsuz sistem için Berger algoritması ile maçlar oluşturuluyor. Lig ID: {LeagueID}. Oluşturulan Hafta Sayısı: {CreatedWeeksCount}", model.LeagueId, createdWeeks.Count);
                var teamsForFixture = model.SelectedTeamIds.ToList();

                List<int> leagueFixtureTeams = new List<int>(teamsForFixture);
                bool isOddLeagueTeamCount = leagueFixtureTeams.Count % 2 != 0;
                if (isOddLeagueTeamCount)
                {
                    leagueFixtureTeams.Add(-1);
                }
                int numTeamsForLeagueBerger = leagueFixtureTeams.Count;
                int roundsInLeagueSingleDevre = numTeamsForLeagueBerger - 1;
                int matchesPerRoundLeague = numTeamsForLeagueBerger / 2;

                _logger.LogInformation("Grupsuz Lig: Takım sayısı (Berger için): {NumTeamsForLeagueBerger}, Tek devreli round sayısı: {RoundsInLeagueSingleDevre}, Round başı maç: {MatchesPerRoundLeague}",
                    numTeamsForLeagueBerger, roundsInLeagueSingleDevre, matchesPerRoundLeague);

                if (createdWeeks.Count < roundsInLeagueSingleDevre)
                {
                    _logger.LogWarning("Grupsuz lig için yetersiz hafta oluşturulmuş. Gereken: {RoundsInLeagueSingleDevre}, Mevcut: {CreatedWeeksCount}. Tüm 1. devre maçları oluşturulamayabilir.", roundsInLeagueSingleDevre, createdWeeks.Count);
                }
                if (model.PlayReturnMatches && createdWeeks.Count < roundsInLeagueSingleDevre * 2)
                {
                    _logger.LogWarning("Grupsuz lig için rövanş maçları dahil yetersiz hafta oluşturulmuş. Gereken: {RequiredWeeks}, Mevcut: {CreatedWeeksCount}. Tüm rövanş maçları oluşturulamayabilir.", roundsInLeagueSingleDevre * 2, createdWeeks.Count);
                }

                List<int> initialLeagueTeamOrderForReturn = new List<int>(leagueFixtureTeams);

                // 1. Devre (Grupsuz)
                for (int round = 0; round < roundsInLeagueSingleDevre; round++)
                {
                    if (round >= createdWeeks.Count)
                    {
                        _logger.LogWarning("Grupsuz Lig, 1. Devre: {RoundPlusOne}. tur için genel hafta kalmadı (Max: {CreatedWeeksCount}).", round + 1, createdWeeks.Count);
                        break;
                    }
                    Week currentWeek = createdWeeks[round];

                    for (int i = 0; i < matchesPerRoundLeague; i++)
                    {
                        int homeTeamId = leagueFixtureTeams[i];
                        int awayTeamId = leagueFixtureTeams[numTeamsForLeagueBerger - 1 - i];

                        if (homeTeamId != -1 && awayTeamId != -1)
                        {
                            var match = new Match
                            {
                                LeagueID = model.LeagueId,
                                WeekID = currentWeek.WeekID,
                                HomeTeamID = homeTeamId,
                                AwayTeamID = awayTeamId,
                                GroupID = null,
                                MatchDate = currentWeek.StartDate.AddDays(round % 7).AddHours(15 + (i % 3)),
                                IsPlayed = false,
                                Status = Models.Match.MatchStatus.NotStarted,
                            };
                            createdMatches.Add(match);
                            _logger.LogDebug("Grupsuz Lig 1. Devre Maçı Eklendi: {HomeTeam} vs {AwayTeam}, Hafta: {WeekNumber} ({WeekId}), Round: {Round}", homeTeamId, awayTeamId, currentWeek.WeekNumber, currentWeek.WeekID, round + 1);
                        }
                    }
                    if (numTeamsForLeagueBerger > 2)
                    {
                        int lastTeam = leagueFixtureTeams[numTeamsForLeagueBerger - 1];
                        for (int k = numTeamsForLeagueBerger - 1; k > 1; k--)
                        {
                            leagueFixtureTeams[k] = leagueFixtureTeams[k - 1];
                        }
                        leagueFixtureTeams[1] = lastTeam;
                    }
                }

                // 2. Devre (Grupsuz - Rövanş)
                if (model.PlayReturnMatches)
                {
                    leagueFixtureTeams = new List<int>(initialLeagueTeamOrderForReturn);
                    _logger.LogInformation("Grupsuz lig için rövanş maçları oluşturuluyor. Başlangıç takım sırası rövanş için sıfırlandı.");
                    for (int round = 0; round < roundsInLeagueSingleDevre; round++)
                    {
                        int overallWeekIndexForReturn = round + roundsInLeagueSingleDevre;
                        if (overallWeekIndexForReturn >= createdWeeks.Count)
                        {
                            _logger.LogWarning("Grupsuz Lig, 2. Devre: {RoundPlusOne}. turun rövanşı için genel hafta kalmadı (Genel Hafta Index: {OverallWeekIndexForReturn}, Max: {CreatedWeeksCount}).", round + 1, overallWeekIndexForReturn, createdWeeks.Count);
                            break;
                        }
                        Week currentReturnWeek = createdWeeks[overallWeekIndexForReturn];

                        for (int i = 0; i < matchesPerRoundLeague; i++)
                        {
                            int originalHome = leagueFixtureTeams[i];
                            int originalAway = leagueFixtureTeams[numTeamsForLeagueBerger - 1 - i];

                            if (originalHome != -1 && originalAway != -1)
                            {
                                var returnMatch = new Match
                                {
                                    LeagueID = model.LeagueId,
                                    WeekID = currentReturnWeek.WeekID,
                                    HomeTeamID = originalAway,
                                    AwayTeamID = originalHome,
                                    GroupID = null,
                                    MatchDate = currentReturnWeek.StartDate.AddDays(round % 7).AddHours(15 + (i % 3)),
                                    IsPlayed = false,
                                    Status = Models.Match.MatchStatus.NotStarted,
                                };
                                createdMatches.Add(returnMatch);
                                _logger.LogDebug("Grupsuz Lig 2. Devre (Rövanş) Maçı Eklendi: {HomeTeam} vs {AwayTeam}, Hafta: {WeekNumber} ({WeekId}), Round: {Round}", originalAway, originalHome, currentReturnWeek.WeekNumber, currentReturnWeek.WeekID, round + 1);
                            }
                        }
                        if (numTeamsForLeagueBerger > 2)
                        {
                            int lastTeam = leagueFixtureTeams[numTeamsForLeagueBerger - 1];
                            for (int k = numTeamsForLeagueBerger - 1; k > 1; k--)
                            {
                                leagueFixtureTeams[k] = leagueFixtureTeams[k - 1];
                            }
                            leagueFixtureTeams[1] = lastTeam;
                        }
                    }
                }
            }


            if (createdMatches.Any())
            {
                _context.Matches.AddRange(createdMatches);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Tüm ({Count}) maçlar veritabanına kaydedildi. Lig ID: {LeagueID}", createdMatches.Count, model.LeagueId);
            }
            else
            {
                _logger.LogWarning("Veritabanına kaydedilecek hiç maç oluşturulmadı. Lig ID: {LeagueID}", model.LeagueId);
            }

            TempData["SuccessMessage"] = $"'{model.SeasonName}' sezonu için fikstür ({createdMatches.Count} maç) oluşturma işlemi tamamlandı!";
            return RedirectToAction("ManageFixture", new { leagueId = model.LeagueId });
        }

    }
}
