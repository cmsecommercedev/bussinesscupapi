using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BussinessCupApi.Data; // DbContext'inizin namespace'i
using System.Threading.Tasks;

namespace BussinessCupApi.ViewComponents // Projenizin namespace'ine göre ayarlayın
{
    public class CityListViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context; // DbContext sınıfınızın adı

        public CityListViewComponent(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var cities = await _context.City.ToListAsync(); // Veya veriyi nasıl çekiyorsanız
                                                              // ViewComponent'ler varsayılan olarak Views/Shared/Components/{ComponentName}/Default.cshtml dosyasını arar
            return View(cities);
        }
    }
}