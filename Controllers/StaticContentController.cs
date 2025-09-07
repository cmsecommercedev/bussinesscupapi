using System;
using System.Linq;
using System.Threading.Tasks;
using BussinessCupApi.Data;
using BussinessCupApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BussinessCupApi.Controllers
{
	[Authorize(Roles = "Admin")]
	public class StaticContentController : Controller
	{
		private readonly ApplicationDbContext _context;

		public StaticContentController(ApplicationDbContext context)
		{
			_context = context;
		}

		[HttpGet]
		public async Task<IActionResult> Index()
		{
			var items = await _context.StaticKeyValues
				.AsNoTracking()
				.OrderBy(x => x.Key)
				.ToListAsync();

			return View(items);
		}

		[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Edit(string key, [FromBody] StaticKeyValue model)
{
    if (string.IsNullOrWhiteSpace(key)) 
        return BadRequest();

    var entity = await _context.StaticKeyValues.FirstOrDefaultAsync(x => x.Key == key);
    if (entity == null) return NotFound();

    entity.Value = model?.Value ?? string.Empty;
    entity.UpdatedAt = DateTime.UtcNow;

    await _context.SaveChangesAsync();
    return Ok(new { message = $"'{key}' güncellendi." });
}
 
	}
}