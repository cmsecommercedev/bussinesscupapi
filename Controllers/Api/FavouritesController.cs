using BussinessCupApi.Data;
using BussinessCupApi.Models;
using BussinessCupApi.Models.Dtos; // Eklediğimiz DTO'lar için
using BussinessCupApi.Managers;    // NotificationManager için
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory; // IMemoryCache için
using Microsoft.Extensions.Caching.Distributed; // IDistributedCache için (Opsiyonel ama iyi pratik)
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using BussinessCupApi.Dtos;
using BussinessCupApi.Attributes; // Kullanıcının kimliğini almak için (opsiyonel)

namespace BussinessCupApi.Controllers.Api // Namespace'i kontrol edin
{
    [ApiKeyAuth]
    [Route("api/[controller]")]
    [ApiController]
    public class FavouritesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FavouritesController> _logger;
        private readonly IMemoryCache _cache;
        private readonly NotificationManager _notificationManager;

        public FavouritesController(ApplicationDbContext context, ILogger<FavouritesController> logger, IMemoryCache cache, NotificationManager notificationManager)
        {
            _context = context;
            _logger = logger;
            _cache = cache;
            _notificationManager = notificationManager;
        }

        [HttpPost("addusertoall")]
        public async Task<IActionResult> AddUserToAll([FromQuery] string userToken,string culture="tr")
        {
            if (string.IsNullOrWhiteSpace(userToken))
                return BadRequest("Geçersiz macid veya kullanıcı token bilgisi.");


            var result = await _notificationManager.SubscribeToTopicAsync(new[] { userToken }, $"all_users_{culture}");

            if (result.success)
                return Ok(new { success = true, message = $"Kullanıcı başarıyla eklendi" });
            else
                return StatusCode(500, new { success = false, message = $"Hata: {result.message}" });
        }

    }
}