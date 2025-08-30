using Microsoft.AspNetCore.Mvc;
using BussinessCupApi.Data;
using BussinessCupApi.Models;
using System.Linq;
using System.Threading.Tasks; // Eklendi
using Microsoft.EntityFrameworkCore;
using BussinessCupApi.ViewModels; // Eklendi
using Microsoft.Extensions.Configuration; // Yeni eklendi
using System.Security.Cryptography; // Yeni eklendi
using System.Text; // Yeni eklendi
using BussinessCupApi.Models.UserPlayerTypes;
using BussinessCupApi.Managers;
using Microsoft.AspNetCore.Authorization; // Yeni eklendi (UserType için)

namespace BussinessCupApi.Controllers
{    [Authorize(Roles = "Admin")]

    public class CityController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly CustomUserManager _customUserManager;

        public CityController(ApplicationDbContext context, IConfiguration configuration, CustomUserManager customUserManager)
        {
            _context = context;
            _configuration = configuration;
            _customUserManager = customUserManager;
        }

        // Şehir Listesi ve Ekleme Formu
        public async Task<IActionResult> Create()
        {
            ViewBag.Cities = await _context.City.ToListAsync();
            return View();
        }

        // Şehir Ekle (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        // Parametre adını ViewModel'deki özelliğe göre değiştirin
        public async Task<IActionResult> Create(CityViewModel model) // async Task ve ViewModel eklendi
        {
            // Gelen modelin NewCity özelliğini kontrol ediyoruz
            if (ModelState.IsValid)
            {
                _context.Add(new City() { Name=model.Name }); // viewModel.NewCity kullanıldı
                await _context.SaveChangesAsync(); // async kullanıldı
                return RedirectToAction(nameof(Create));
            }

            return View("Create"); // Hata durumunda Index view'ı viewModel ile döndürülüyor
        }

        // Şehir Sil (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id) // async Task eklendi
        {
            var city = await _context.City.FindAsync(id);
            if (city == null)
            {
                return NotFound();
            }

            _context.City.Remove(city);
            await _context.SaveChangesAsync(); // async kullanıldı
            return RedirectToAction(nameof(Create));
        }

        //GET: City/CreateCityAdmin
        // Yeni şehir admini oluşturma formunu gösterir
        [HttpGet]
        public IActionResult CreateCityAdmin(int id)
        {
           RegisterDto _dto = new RegisterDto() { CityID = id };

           return View(_dto);
        }

        // POST: City/CreateCityAdmin
        // Yeni şehir admini kullanıcısını oluşturur
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCityAdmin(RegisterDto model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Email'in zaten var olup olmadığını kontrol et
            if (await _context.Users.AnyAsync(u => u.Email == model.Email))
            {
                ModelState.AddModelError("Email", "Bu email adresi zaten kullanılıyor.");
                return View(model);
            }

            // CustomUserManager ile kullanıcı oluşturma
            var newUser = new User
            {
                Email = model.Email,
                UserName=model.Email,
                Firstname = model.Firstname,
                Lastname = model.Lastname,
                UserType = UserType.CityAdmin,
                UserRole = "CityAdmin",
                MacID = model.MacID ?? string.Empty,
                OS = model.OS ?? string.Empty,
                CityID = model.CityID,
                UserKey = GenerateUniqueUserKey(),
                ExternalID = string.Empty,
                isSubscribed = true
            };

            var createResult = await _customUserManager.CreateUserAsync(newUser, model.Password ?? string.Empty, "CityAdmin");
            if (!createResult.Succeeded)
            {
                foreach (var error in createResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }
                        

            TempData["SuccessMessage"] = "Şehir admini başarıyla oluşturuldu.";
            return RedirectToAction(nameof(Create));
        }
        [HttpGet]
        public IActionResult CreateAnnouncer(int id)
        {
            RegisterDto _dto = new RegisterDto() { CityID = id };

            return View(_dto);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAnnouncer(RegisterDto model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Email'in zaten var olup olmadığını kontrol et
            if (await _context.Users.AnyAsync(u => u.Email == model.Email))
            {
                ModelState.AddModelError("Email", "Bu email adresi zaten kullanılıyor.");
                return View(model);
            }

            // CustomUserManager ile kullanıcı oluşturma
            var newUser = new User
            {
                Email = model.Email,
                UserName=model.Email,
                Firstname = model.Firstname,
                Lastname = model.Lastname,
                UserType = UserType.Announcer,
                UserRole = "Announcer",
                MacID = model.MacID ?? string.Empty,
                OS = model.OS ?? string.Empty,
                CityID = model.CityID,
                UserKey = GenerateUniqueUserKey(),
                ExternalID = string.Empty,
                isSubscribed = true
            };

            var createResult = await _customUserManager.CreateUserAsync(newUser, model.Password ?? string.Empty, "Announcer");
            if (!createResult.Succeeded)
            {
                foreach (var error in createResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }

            TempData["SuccessMessage"] = "Şehir admini başarıyla oluşturuldu.";
            return RedirectToAction(nameof(Create));
        }

        private string GenerateUniqueUserKey()
        {
            // Benzersiz bir key oluştur (örn: USER_20240315_XXXXX)
            string timestamp = DateTime.UtcNow.ToString("yyyyMMdd");
            string randomPart = Guid.NewGuid().ToString("N").Substring(0, 5).ToUpper();
            return $"USER_{timestamp}_{randomPart}";
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOrder([FromBody] List<CityOrderUpdate> updates)
        {
            foreach (var update in updates)
            {
                var city = await _context.City.FindAsync(update.CityId);
                if (city != null)
                {
                    city.Order = update.NewOrder;
                }
            }
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }
    }

    public class CityOrderUpdate
    {
        public int CityId { get; set; }
        public int NewOrder { get; set; }
    }
}