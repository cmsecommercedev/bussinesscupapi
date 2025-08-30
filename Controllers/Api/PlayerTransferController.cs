using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed; // IDistributedCache için (Opsiyonel ama iyi pratik)
using Microsoft.Extensions.Caching.Memory; // IMemoryCache için
using Microsoft.Extensions.Logging;
using BussinessCupApi.Attributes;
using BussinessCupApi.Data;
using BussinessCupApi.Dtos;
using BussinessCupApi.Managers;    // NotificationManager için
using BussinessCupApi.Models;
using BussinessCupApi.Models.Api;
using BussinessCupApi.Models.Dtos; // Eklediğimiz DTO'lar için
using BussinessCupApi.Models.UserPlayerTypes; // Kullanıcının kimliğini almak için (opsiyonel)
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace BussinessCupApi.Controllers.Api // Namespace'i kontrol edin
{
    // [ApiKeyAuth]
    [Route("api/[controller]")]
    [ApiController]
    public class PlayerTransferController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PlayerTransferController> _logger;
        private readonly IMemoryCache _cache;
        private readonly NotificationManager _notificationManager;
        private readonly IConfiguration _configuration;


        public PlayerTransferController(ApplicationDbContext context, ILogger<PlayerTransferController> logger, IMemoryCache cache, NotificationManager notificationManager, IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _cache = cache;
            _notificationManager = notificationManager;
            _configuration = configuration;
        }

        /// <summary>
        /// Yeni bir oyuncu transfer teklifi oluşturur.
        /// Teklifi yapan kaptan, authenticate olmuş kullanıcıdır.
        /// </summary>
        /// <param name="requestDto">Transfer teklifi detaylarını içerir: PlayerUserID ve TargetCaptainUserID.</param>
        [HttpPost("request")]
        public async Task<IActionResult> CreateTransferRequest([FromBody] PlayerTransferRequestDto requestDto)
        {
            try
            {
                // İsteği yapan kaptanın kimliğini alabiliriz (eğer JWT veya benzeri bir kimlik doğrulama varsa)
                // var offeringCaptainId = User.FindFirstValue(ClaimTypes.NameIdentifier); // Örnek
                // if (string.IsNullOrEmpty(offeringCaptainId))
                // {
                //     return Unauthorized(new { message = "Teklif yapan kaptan kimliği bulunamadı." });
                // }

                // TODO: requestDto.PlayerUserID'nin ve requestDto.TargetCaptainUserID'nin geçerli kullanıcılar
                // ve TargetCaptainUserID'nin gerçekten requestDto.PlayerUserID'nin kaptanı olup olmadığını doğrula.
                // First get the team ID from the player

                // ... mevcut kodlar ...
                 
                var playerCurrentTeamId = await _context.Players
                    .Where(p => p.UserId == requestDto.PlayerUserID)
                    .Select(p => p.TeamID)
                    .FirstOrDefaultAsync();

                var requestedCaptainTeamId = await _context.Players
                    .Where(p => p.UserId == requestDto.RequestedCaptainUserID)
                    .Select(p => p.TeamID)
                    .FirstOrDefaultAsync();

                // Eğer aynı takımdalarsa hata döndür
                if (playerCurrentTeamId == requestedCaptainTeamId)
                {
                    return BadRequest(new { message = "Requested captain ile oyuncu aynı takımda olamaz." });
                }


                var existingPendingRequest = await _context.PlayerTransferRequest
                    .FirstOrDefaultAsync(r => r.UserID == requestDto.PlayerUserID && !r.Approved);

                if (existingPendingRequest != null)
                {
                    return BadRequest(new { message = "Bu oyuncu için zaten onaylanmamış bir transfer isteği mevcut." });
                }

                // Transfer isteği başarıyla oluşturulduktan sonra haber ekle
                var player = await _context.Players
                    .Include(p => p.Team)
                    .FirstOrDefaultAsync(p => p.UserId == requestDto.PlayerUserID);

                if (player == null || player.Team == null)
                {
                    return BadRequest(new { message = "Oyuncu bulunamadı veya bir takıma ait değil." });
                }

                // Şehir transfer yasağı kontrolü
                var cityRestriction = await _context.CityRestrictions
                    .FirstOrDefaultAsync(cr => cr.CityID == player.Team.CityID);

                if (cityRestriction != null && cityRestriction.IsTransferBanned && !player.Team.TeamIsFree)
                {
                    return BadRequest(new { message = "Oyuncunun bulunduğu şehirde transfer yasağı var." });
                }



                var playerTeamId = await _context.Players
                    .Where(p => p.UserId == requestDto.PlayerUserID)
                    .Select(p => p.TeamID)
                    .FirstOrDefaultAsync();

                var playerTeam = await _context.Teams.FindAsync(playerTeamId);

                if (playerTeamId == 0 || playerTeam == null)
                {
                    return BadRequest(new { message = "Oyuncu bulunamadı veya bir takıma ait değil." });
                }

                var captainUser = await _context.Users
                    .Join(_context.Players,
                        user => user.Id,
                        player => player.UserId,
                        (user, player) => new { User = user, Player = player })
                    .Where(x => x.Player.TeamID == playerTeamId && x.User.UserType == UserType.Captain)
                    .Select(x => x.User.Id)
                    .FirstOrDefaultAsync();

                bool isFreeTeamAndNoCaptain = captainUser == null && playerTeam.TeamIsFree;
                if (captainUser == null && !playerTeam.TeamIsFree)
                    return BadRequest(new { message = "Hedef takımın kaptanı bulunamadı." });
                else if (isFreeTeamAndNoCaptain)
                    captainUser = requestDto.PlayerUserID;

                var transferRequest = new PlayerTransferRequest
                {
                    UserID = requestDto.PlayerUserID, // Transfer edilmek istenen oyuncu
                    RequestedCaptainUserID = requestDto.RequestedCaptainUserID,
                    RequestDate = DateTime.UtcNow,
                    ApprovalCaptainUserID = captainUser,
                    Approved = false
                };

                _context.PlayerTransferRequest.Add(transferRequest);
                await _context.SaveChangesAsync();

                if (isFreeTeamAndNoCaptain)
                {
                    // Sadece oyuncuya özel mesaj gönder
                    var notificationToPlayer = new NotificationViewModel
                    {
                        TitleTr = "Transfer Teklifi (Serbest Takım)",
                        MessageTr = "Serbest takıma transfer teklifiniz var. Onaylamak için uygulamadan kontrol edebilirsiniz.",
                        TitleEn = "Transfer Offer (Free Team)",
                        MessageEn = "You have a transfer offer to a free team. Please check the app to approve."
                    };

                    var playerUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == requestDto.PlayerUserID);
                    if (playerUser?.ExternalID != null)
                    {
                        await _notificationManager.SendNotificationToUser(playerUser.ExternalID, notificationToPlayer);
                    }
                }
                else
                {
                    // Kaptana bildirim gönder
                    var notificationToTargetCaptain = new NotificationViewModel
                    {
                        TitleTr = "Yeni Transfer Teklifi",
                        MessageTr = $"Bir oyuncunuz için yeni bir transfer teklifi aldınız.",
                        TitleEn = "New Transfer Offer",
                        MessageEn = "You have received a new transfer offer for one of your players."
                    };

                    var captain = await _context.Users.FirstOrDefaultAsync(u => u.Id == captainUser);
                    if (captain?.ExternalID != null)
                    {
                        await _notificationManager.SendNotificationToUser(captain.ExternalID, notificationToTargetCaptain);
                    }

                    // Oyuncuya bildirim gönder
                    var notificationToPlayer = new NotificationViewModel
                    {
                        TitleTr = "Transfer Teklifi",
                        MessageTr = "Sizin için yeni bir transfer teklifi yapıldı.",
                        TitleEn = "Transfer Offer",
                        MessageEn = "A new transfer offer has been made for you."
                    };

                    var playerUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == requestDto.PlayerUserID);
                    if (playerUser?.ExternalID != null)
                    {
                        await _notificationManager.SendNotificationToUser(playerUser.ExternalID, notificationToPlayer);
                    }
                }

                // ... mevcut kodlar ...



                if (player != null && player.Team != null)
                {
                    var playerName = $"{player.FirstName} {player.LastName}";
                    var teamName = player.Team.Name;
                    var teamLogo = player.Team.LogoUrl;
                    var playerIcon = player.Icon;

                    var kapImage = "https://minifutbolturkiye-test.agx-labs.com/images/kapimage.png";

                    var matchNews = new MatchNews
                    {
                        Title = "Kap Bildirimi",
                        Subtitle = $"{playerName} için {teamName} görüşmelere başlamıştır",
                        MatchNewsMainPhoto = kapImage,
                        DetailsTitle = "Transfer Görüşmesi",
                        Details = $"{playerName} için {teamName} ile transfer görüşmeleri başlamıştır.",
                        TeamID = player.TeamID,
                        CreatedDate = DateTime.UtcNow,
                        Published = true,
                        IsMainNews = false,
                        Photos = new List<MatchNewsPhoto>(),
                        CityID = player.Team.CityID

                    };

                    // Takım logosu ve oyuncu ikonu ek fotoğraf olarak ekleniyor
                    if (!string.IsNullOrEmpty(teamLogo))
                        matchNews.Photos.Add(new MatchNewsPhoto { PhotoUrl = teamLogo });
                    if (!string.IsNullOrEmpty(playerIcon))
                        matchNews.Photos.Add(new MatchNewsPhoto { PhotoUrl = playerIcon });

                    _context.MatchNews.Add(matchNews);
                    await _context.SaveChangesAsync();
                }


                return Ok(new { message = "Transfer isteği başarıyla oluşturuldu ve hedef kaptana bildirildi." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Transfer isteği oluşturulurken hata oluştu");
                return BadRequest(new { message = "Transfer isteği oluşturulurken bir hata oluştu." });
            }
        }

        /// <summary>
        /// Bir oyuncu transfer teklifini onaylar.
        /// Bu işlemi oyuncunun mevcut takım kaptanı (RequestedCaptainUserID) yapmalıdır.
        /// </summary>
        /// <param name="requestId">Onaylanacak transfer isteğinin ID'si.</param>
        [HttpPost("approve/{requestId}")]
        public async Task<IActionResult> ApproveTransferRequest(int requestId)
        {
            try
            {
                var transferRequest = await _context.PlayerTransferRequest
                                            .FirstOrDefaultAsync(r => r.PlayerTransferRequestID == requestId && (!r.Approved || !r.Rejected));

                if (transferRequest == null)
                    return NotFound(new { message = "Transfer isteği bulunamadı." });

                if (transferRequest.Approved)
                    return BadRequest(new { message = "Bu transfer isteği zaten onaylanmış." });

                // TODO: Bu endpoint'i çağıran kullanıcının gerçekten transferRequest.RequestedCaptainUserID olup olmadığını doğrula.
                // var approvingCaptainId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                // if (transferRequest.RequestedCaptainUserID.ToString() != approvingCaptainId)
                // {
                //     return Forbid("Bu transfer isteğini onaylama yetkiniz yok.");
                // }

                transferRequest.Approved = true;
                transferRequest.ApprovalDate = DateTime.UtcNow;
                transferRequest.ApprovalCaptainUserID = transferRequest.RequestedCaptainUserID; // Onaylayan kaptan, talep edilen kaptandır.

                // TODO: Oyuncunun takımını değiştirme lojiği burada veya bir serviste ele alınmalı.
                var player = await _context.Players.FirstOrDefaultAsync(p => p.UserId == transferRequest.UserID);
                var _requestedCaptain = await _context.Players.FirstOrDefaultAsync(p => p.UserId == transferRequest.RequestedCaptainUserID);

                if (player != null && _requestedCaptain != null) { player.TeamID = _requestedCaptain.TeamID; }

                await _context.SaveChangesAsync();

                // Oyuncuya bildirim gönder
                var notificationToPlayer = new NotificationViewModel
                {
                    TitleTr = "Transfer Teklifiniz Onaylandı",
                    MessageTr = "Bir takıma transfer teklifiniz onaylandı.",
                    TitleEn = "Your Transfer Offer Approved",
                    MessageEn = "Your transfer offer to a team has been approved."
                };
                var _userRequested = await _context.Users.FirstOrDefaultAsync(u => u.Id == transferRequest.UserID);

                await _notificationManager.SendNotificationToUser(_userRequested.ExternalID, notificationToPlayer);



                var notificationTocaptainuserRequested = new NotificationViewModel
                {
                    TitleTr = "Transfer Tamamlandı",
                    MessageTr = $"{player.FirstName} {player.LastName} takımınıza transfer oldu.",
                    TitleEn = "Transfer Completed",
                    MessageEn = $"{player.FirstName} {player.LastName} has been transferred to your team."
                };
                var _captainuserRequested = await _context.Users.FirstOrDefaultAsync(u => u.Id == transferRequest.RequestedCaptainUserID);

                await _notificationManager.SendNotificationToUser(_captainuserRequested.ExternalID, notificationTocaptainuserRequested);


                if (player != null && player.Team != null)
                {
                    var playerName = $"{player.FirstName} {player.LastName}";
                    var teamName = player.Team.Name;
                    var teamLogo = player.Team.LogoUrl;
                    var playerIcon = player.Icon;

                    var kapImage = "https://minifutbolturkiye-test.agx-labs.com/images/kapimage.png";

                    var matchNews = new MatchNews
                    {
                        Title = "Transfer Tamamlandı",
                        Subtitle = $"{playerName}, {teamName} ile resmi sözleşme imzaladı.",
                        MatchNewsMainPhoto = kapImage,
                        DetailsTitle = "Resmi Transfer",
                        Details = $"{playerName}, {teamName} takımına transfer oldu ve resmi sözleşmeye imza attı.",
                        TeamID = player.TeamID,
                        CreatedDate = DateTime.UtcNow,
                        Published = true,
                        IsMainNews = false,
                        Photos = new List<MatchNewsPhoto>()
                    };


                    // Takım logosu ve oyuncu ikonu ek fotoğraf olarak ekleniyor
                    if (!string.IsNullOrEmpty(teamLogo))
                        matchNews.Photos.Add(new MatchNewsPhoto { PhotoUrl = teamLogo });
                    if (!string.IsNullOrEmpty(playerIcon))
                        matchNews.Photos.Add(new MatchNewsPhoto { PhotoUrl = playerIcon });

                    _context.MatchNews.Add(matchNews);
                    await _context.SaveChangesAsync();
                }

                // İsteği yapan (teklif eden) kaptana bildirim göndermek için onun ID'si PlayerTransferRequest'te saklanmıyor.
                // Eğer bu gerekliyse, PlayerTransferRequest modeline OfferingCaptainUserID gibi bir alan eklenmelidir.

                return Ok(new { message = "Transfer isteği başarıyla onaylandı." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Transfer isteği onaylanırken hata oluştu");
                return BadRequest(new { message = "Transfer isteği onaylanırken bir hata oluştu." });
            }
        }

        /// <summary>
        /// Bir oyuncu transfer teklifini reddeder.
        /// Bu işlemi oyuncunun mevcut takım kaptanı (RequestedCaptainUserID) yapmalıdır.
        /// </summary>
        /// <param name="requestId">Reddedilecek transfer isteğinin ID'si.</param>
        [HttpPost("reject/{requestId}")]
        public async Task<IActionResult> RejectTransferRequest(int requestId)
        {
            try
            {
                var transferRequest = await _context.PlayerTransferRequest
                    .FirstOrDefaultAsync(r => r.PlayerTransferRequestID == requestId && !r.Approved && !r.Rejected);

                if (transferRequest == null)
                    return NotFound(new { message = "Transfer isteği bulunamadı veya zaten işlenmiş." });

                // TODO: Bu endpoint'i çağıran kullanıcının gerçekten transferRequest.RequestedCaptainUserID olup olmadığını doğrula.

                transferRequest.Rejected = true;
                transferRequest.RejectionDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Oyuncuya bildirim gönder
                var notificationToPlayer = new NotificationViewModel
                {
                    TitleTr = "Transfer Teklifiniz Reddedildi",
                    MessageTr = "Bir takıma transfer teklifiniz reddedildi.",
                    TitleEn = "Your Transfer Offer Rejected",
                    MessageEn = "Your transfer offer to a team has been rejected."
                };
                var _userRequested = await _context.Users.FirstOrDefaultAsync(u => u.Id == transferRequest.UserID);
                if (_userRequested?.ExternalID != null)
                    await _notificationManager.SendNotificationToUser(_userRequested.ExternalID, notificationToPlayer);

                return Ok(new { message = "Transfer isteği başarıyla reddedildi." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Transfer isteği reddedilirken hata oluştu");
                return BadRequest(new { message = "Transfer isteği reddedilirken bir hata oluştu." });
            }
        }

        [HttpGet("requests")]
        public async Task<IActionResult> GetTransferRequests([FromQuery] string userId)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                {
                    return NotFound(new { message = "Kullanıcı bulunamadı." });
                }

                var requests = await _context.PlayerTransferRequest
                    .Where(r => r.UserID == userId || r.RequestedCaptainUserID == userId || (r.ApprovalCaptainUserID == userId && (!r.Approved && !r.Rejected)))
                    .OrderByDescending(r => r.RequestDate)
                    .ToListAsync();


                var response = requests.Select(r => new PlayerTransferRequestResponseDto
                {
                    PlayerTransferRequestID = r.PlayerTransferRequestID,
                    PlayerName = _context.Players
                        .Where(p => p.UserId == r.UserID)
                        .Select(p => $"{p.FirstName} {p.LastName}")
                        .FirstOrDefault() ?? "Bilinmiyor",
                    UserID = r.UserID,
                    RequestedCaptainName = _context.Users
                        .Where(u => u.Id == r.RequestedCaptainUserID)
                        .Select(u => $"{u.Firstname} {u.Lastname}")
                        .FirstOrDefault() ?? "Bilinmiyor",
                    RequestedCaptainUserID = r.RequestedCaptainUserID,
                    ApprovalCaptainName = _context.Users
                        .Where(u => u.Id == r.ApprovalCaptainUserID)
                        .Select(u => $"{u.Firstname} {u.Lastname}")
                        .FirstOrDefault(),
                    ApprovalCaptainUserID = r.ApprovalCaptainUserID,
                    Approved = r.Approved,
                    Rejected = r.Rejected,
                    RequestDate = r.RequestDate,
                    ApprovalDate = r.ApprovalDate,
                    RejectionDate = r.RejectionDate,
                    Status = r.Approved ? "Onaylandı" : r.Rejected ? "Reddedildi" : "Beklemede",
                    Message = user.UserType == UserType.Player ?
                        "Size gelen transfer teklifi" :
                        user.UserType == UserType.Captain && r.RequestedCaptainUserID == userId ?
                        "Bir oyuncu için yaptığınız transfer teklifi" :
                        "Takımınızdaki bir oyuncu için gelen transfer teklifi",
                    ApprovalTeamId = _context.Players
                        .Where(p => p.UserId == r.ApprovalCaptainUserID)
                        .Select(p => p.TeamID)
                        .FirstOrDefault(),
                    ApprovalTeamName = _context.Players
                        .Where(p => p.UserId == r.ApprovalCaptainUserID)
                        .Select(p => p.Team.Name)
                        .FirstOrDefault(),
                    RequestedTeamId = _context.Players
                        .Where(p => p.UserId == r.RequestedCaptainUserID)
                        .Select(p => p.TeamID)
                        .FirstOrDefault(),
                    RequestedTeamName = _context.Players
                        .Where(p => p.UserId == r.RequestedCaptainUserID)
                        .Select(p => p.Team.Name)
                        .FirstOrDefault(),
                    ButtonShow = r.ApprovalCaptainUserID == userId && !r.Approved && !r.Rejected
                }).ToList();

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Transfer istekleri getirilirken hata oluştu");
                return BadRequest(new { message = "Transfer istekleri getirilirken bir hata oluştu." });
            }
        }

    }
}