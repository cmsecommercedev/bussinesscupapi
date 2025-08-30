// Controllers/Admin/SubscriptionController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BussinessCupApi.Data;
using BussinessCupApi.ViewModels; // ViewModel yolu
using System.Linq;
using System.Threading.Tasks;

namespace BussinessCupApi.Controllers.Admin
{
    public class SubscriptionController : Controller
    {
        private readonly ApplicationDbContext _context;
        public SubscriptionController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var players = await _context.Players.Include(p => p.User).Include(p => p.Team).ToListAsync();
            var model = new SubscriptionSummaryViewModel
            {
                ToplamOyuncu = players.Count,
                UyeOlanOyuncu = players.Count(p => p.isSubscribed == true),
                UyeOlmayanOyuncu = players.Count(p => p.isSubscribed == false || p.isSubscribed == null),
                Oyuncular = players.Select(p => new SubscriptionPlayerViewModel
                {
                    PlayerID = p.PlayerID,
                    FirstName = p.FirstName,
                    LastName = p.LastName,
                    Position = p.Position,
                    Number = p.Number,
                    TeamID = p.TeamID,
                    TakimAdi = p.Team != null ? p.Team.Name : null,
                    isSubscribed = p.isSubscribed,
                    SubscriptionExpireDate = p.SubscriptionExpireDate,
                    UserEmail = p.User != null ? p.User.Email : null,
                    Icon = p.Icon // <-- yeni eklendi
                }).Where(x=>x.isSubscribed==true).ToList()
            };
            return View(model);
        }
    }

}