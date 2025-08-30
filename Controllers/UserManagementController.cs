using Microsoft.AspNetCore.Mvc;
using BussinessCupApi.Models;
using System.Linq;
using BussinessCupApi.Attributes;
using BussinessCupApi.Data;
using Microsoft.AspNetCore.Authorization;
using BussinessCupApi.Managers;

namespace BussinessCupApi.Controllers
{
    [Authorize(Roles = "Admin")]

    public class UserManagementController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly NotificationManager _notificationManager;

        public UserManagementController(ApplicationDbContext context, NotificationManager notificationManager)
        {
            _context = context;
            _notificationManager = notificationManager;
        }
        public IActionResult Index()
        { 
            return View();
        }

        [HttpGet]
        public IActionResult Edit(string id)
        {
            var user = _context.Users.FirstOrDefault(u => u.Id == id);
            if (user == null) return NotFound();
            return View(user);
        }

        [HttpPost]
        public IActionResult Edit(User model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = _context.Users.FirstOrDefault(u => u.Id == model.Id);
            if (user == null) return NotFound();

            user.Email = model.Email;
            user.Firstname = model.Firstname;
            user.Lastname = model.Lastname;

            _context.SaveChanges();
            TempData["Success"] = "Kullanıcı başarıyla güncellendi.";
            return RedirectToAction("Index");
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendPushToUser([FromBody] PushToUserDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.UserId) || string.IsNullOrWhiteSpace(dto.Title) || string.IsNullOrWhiteSpace(dto.Message))
                return BadRequest("Eksik bilgi.");

            var user = _context.Users.FirstOrDefault(u => u.Id == dto.UserId);
            if (user == null)
                return NotFound("Kullanıcı bulunamadı.");

            // Kullanıcının cihaz token'ı burada alınmalı (örnek: user.DeviceToken)
            var deviceToken = user.ExternalID;
            if (string.IsNullOrWhiteSpace(deviceToken))
                return BadRequest("Kullanıcının push token'ı yok.");

            var notification = new NotificationViewModel
            {
                TitleTr = dto.Title,
                MessageTr = dto.Message
            };


            var result = await _notificationManager.SendNotificationToUser(deviceToken, notification);

            if (result.success)
                return Ok();
            else
                return StatusCode(500, result.message ?? "Push bildirimi gönderilemedi.");
        }

        // DTO:
        public class PushToUserDto
        {
            public string UserId { get; set; }
            public string Title { get; set; }
            public string Message { get; set; }
        }

        [HttpGet]
        public IActionResult GetUsersByRole(string role, int page = 1, int pageSize = 50)
        {
            var query = _context.Users.Where(u => u.UserRole == role);

            var totalUsers = query.Count(); // Toplam kullanıcı sayısı
            var users = query
                .Skip((page - 1) * pageSize) // Sayfayı atla
                .Take(pageSize) // Belirtilen sayıda kullanıcı al
                .ToList();

            return Json(new
            {
                users,
                totalUsers
            });
        }
        [HttpGet]
public IActionResult SearchUsers(string query, int page = 1, int pageSize = 50)
{
    if (string.IsNullOrWhiteSpace(query))
        return Json(new { users = new List<User>(), totalUsers = 0 });

    var lowered = query.ToLower();
    var usersQuery = _context.Users
        .Where(u =>
            (!string.IsNullOrEmpty(u.Email) && u.Email.ToLower().Contains(lowered)) ||
            (!string.IsNullOrEmpty(u.Firstname) && u.Firstname.ToLower().Contains(lowered)) ||
            (!string.IsNullOrEmpty(u.Lastname) && u.Lastname.ToLower().Contains(lowered)) ||
            (!string.IsNullOrEmpty(u.UserName) && u.UserName.ToLower().Contains(lowered))
        );

    var totalUsers = usersQuery.Count();
    var users = usersQuery
        .OrderBy(u => u.Email)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToList();

    return Json(new { users, totalUsers });
}
    }
}