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
    public class NewsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FavouritesController> _logger;
        private readonly IMemoryCache _cache;

        public NewsController(ApplicationDbContext context, ILogger<FavouritesController> logger, IMemoryCache cache)
        {
            _context = context;
            _logger = logger;
            _cache = cache;
        }


        [HttpGet("main-news")]
        public async Task<IActionResult> GetMainNews()
        {
            var news = await _context.MatchNews
                .Where(n => n.IsMainNews && n.Published)
                .OrderByDescending(n => n.CreatedDate)
                .Select(n => new NewsDto
                {
                    Id = n.Id,
                    Title = n.Title,
                    Subtitle = n.Subtitle,
                    MatchNewsMainPhoto = n.MatchNewsMainPhoto,
                    CreatedDate = n.CreatedDate,
                    Published = n.Published,
                    IsMainNews = n.IsMainNews,
                    CityID = n.CityID,
                    TeamID = n.TeamID,
                    DetailsTitle = n.DetailsTitle,   // EKLENDİ
                    Details = n.Details,             // EKLENDİ
                    Photos = (n.MatchNewsMainPhoto != null && n.MatchNewsMainPhoto != ""
                                ? new List<string> { n.MatchNewsMainPhoto }
                                : new List<string>())
                             .Concat(n.Photos.Select(p => p.PhotoUrl)).ToList()
                })
                .ToListAsync();

            return Ok(news);
        }

        [HttpGet("by-city/{cityId}")]
        public async Task<IActionResult> GetNewsByCity(int cityId)
        {
            var news = await _context.MatchNews
                .Where(n => n.CityID == cityId && n.Published)
                .OrderByDescending(n => n.CreatedDate)
                .Select(n => new NewsDto
                {
                    Id = n.Id,
                    Title = n.Title,
                    Subtitle = n.Subtitle,
                    MatchNewsMainPhoto = n.MatchNewsMainPhoto,
                    CreatedDate = n.CreatedDate,
                    Published = n.Published,
                    IsMainNews = n.IsMainNews,
                    CityID = n.CityID,
                    TeamID = n.TeamID,
                    DetailsTitle = n.DetailsTitle,   // EKLENDİ
                    Details = n.Details,             // EKLENDİ
                    Photos = (n.MatchNewsMainPhoto != null && n.MatchNewsMainPhoto != ""
                                ? new List<string> { n.MatchNewsMainPhoto }
                                : new List<string>())
                             .Concat(n.Photos.Select(p => p.PhotoUrl)).ToList()
                })
                .ToListAsync();

            return Ok(news);
        }

        [HttpGet("by-team/{teamId}")]
        public async Task<IActionResult> GetNewsByTeam(int teamId)
        {
            var news = await _context.MatchNews
                .Where(n => n.TeamID == teamId && n.Published)
                .OrderByDescending(n => n.CreatedDate)
                .Select(n => new NewsDto
                {
                    Id = n.Id,
                    Title = n.Title,
                    Subtitle = n.Subtitle,
                    MatchNewsMainPhoto = n.MatchNewsMainPhoto,
                    CreatedDate = n.CreatedDate,
                    Published = n.Published,
                    IsMainNews = n.IsMainNews,
                    CityID = n.CityID,
                    TeamID = n.TeamID,
                    DetailsTitle = n.DetailsTitle,   // EKLENDİ
                    Details = n.Details,             // EKLENDİ
                    Photos = (n.MatchNewsMainPhoto != null && n.MatchNewsMainPhoto != ""
                                ? new List<string> { n.MatchNewsMainPhoto }
                                : new List<string>())
                             .Concat(n.Photos.Select(p => p.PhotoUrl)).ToList()
                })
                .ToListAsync();

            return Ok(news);
        }

    }
}