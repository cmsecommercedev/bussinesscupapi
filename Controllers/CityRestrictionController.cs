using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BussinessCupApi.Data;
using BussinessCupApi.Managers; // Eğer CustomUserManager burada ise
using BussinessCupApi.Models;
using System.Linq;
using System.Threading.Tasks;

namespace BussinessCupApi.Controllers
{
    [Authorize(Roles = "Admin,CityAdmin")]

    public class CityRestrictionController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly CustomUserManager _customUserManager;

        public CityRestrictionController(ApplicationDbContext context, CustomUserManager customUserManager)
        {
            _context = context;
            _customUserManager = customUserManager;
        }

        public async Task<IActionResult> Manage()
        {
            var user = await _customUserManager.GetUserAsync(User);
            if (User.IsInRole("CityAdmin"))
            {
                int cityId = Convert.ToInt32(user.CityID);
                var cities = await _context.City.Where(x => x.CityID == cityId).ToListAsync();
                return View(cities);
            }
            else if (User.IsInRole("Admin"))
            {
                var cities = await _context.City.ToListAsync();
                return View(cities);
            }
            return View();

        }

        [HttpGet]
        public async Task<IActionResult> GetRestriction(int cityId)
        {
            var restriction = await _context.CityRestrictions.FirstOrDefaultAsync(x => x.CityID == cityId);
            return Json(restriction ?? new CityRestriction { CityID = cityId, IsTransferBanned = false, IsRegistrationStopped = false });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateRestriction([FromBody] CityRestriction model)
        {
            var restriction = await _context.CityRestrictions.FirstOrDefaultAsync(x => x.CityID == model.CityID);
            if (restriction == null)
            {
                _context.CityRestrictions.Add(model);
            }
            else
            {
                restriction.IsTransferBanned = model.IsTransferBanned;
                restriction.IsRegistrationStopped = model.IsRegistrationStopped;
                _context.CityRestrictions.Update(restriction);
            }
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }
    }
}