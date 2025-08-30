using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BussinessCupApi.Data;
using BussinessCupApi.Models;

namespace BussinessCupApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StatusController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        [HttpGet("env")]
        [AllowAnonymous]
        public IActionResult GetEnvironment()
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown";
            return Ok(new
            {
                environment
            });
        }

        [HttpGet("health")]
        [AllowAnonymous]
        public IActionResult Health()
        {
            return Ok(new
            {
                status = "Healthy",
                timestamp = DateTime.UtcNow
            });
        }


        //[HttpPost("import-teams-from-excel")]
        //[AllowAnonymous]
        //public IActionResult ImportTeamsFromExcel([FromServices] IWebHostEnvironment env, [FromServices] ApplicationDbContext db)
        //{
        //    var appFolder = Path.Combine(env.WebRootPath, "APP");
        //    var excelFiles = Directory.GetFiles(appFolder, "*.xlsx");
        //    var result = new List<string>();

        //    foreach (var file in excelFiles)
        //    {
        //        var cityName = Path.GetFileNameWithoutExtension(file).Trim();
        //        // City var mı kontrol et, yoksa ekle
        //        var city = db.City.FirstOrDefault(c => c.Name == cityName);
        //        if (city == null)
        //        {
        //            city = new City { Name = cityName };
        //            db.City.Add(city);
        //            db.SaveChanges();
        //        }

        //        using (var workbook = new XLWorkbook(file))
        //        {
        //            var worksheet = workbook.Worksheets.First();
        //            foreach (var row in worksheet.RowsUsed().Skip(1)) // İlk satır başlık olabilir
        //            {
        //                var teamName = row.Cell(2).GetString().Trim();
        //                if (string.IsNullOrEmpty(teamName)) continue;

        //                var stadium = row.Cell(3).GetString().Trim();
        //                var manager = row.Cell(4).GetString().Trim();
        //                var teamPassword = row.Cell(5).GetString().Trim();

        //                // Aynı isimde takım var mı kontrol et
        //                if (db.Teams.Any(t => t.Name == teamName && t.CityID == city.CityID))
        //                {
        //                    result.Add($"{teamName} zaten mevcut, atlandı.");
        //                    continue;
        //                }

        //                var team = new Team
        //                {
        //                    Name = teamName,
        //                    Stadium = stadium,
        //                    Manager = manager,
        //                    TeamPassword = teamPassword,
        //                    CityID = city.CityID,
        //                    TeamIsFree = true
        //                };
        //                db.Teams.Add(team);
        //                result.Add($"{teamName} eklendi.");
        //            }
        //            db.SaveChanges();
        //        }
        //    }

        //    return Ok(new { message = "İşlem tamamlandı", detaylar = result });
        //}
    }
}
