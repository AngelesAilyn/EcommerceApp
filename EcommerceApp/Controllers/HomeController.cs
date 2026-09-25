using EcommerceApp.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var productos = await _context.Products
                .AsNoTracking()
                .Include(p => p.CategoryNavigation)
                .Where(p =>
                    !string.IsNullOrEmpty(p.Category) &&
                    p.Stock > 0 &&
                    !p.IsArchived &&
                    p.IsAvailable &&
                    p.CategoryNavigation != null &&
                    p.CategoryNavigation.IsActive)
                .OrderBy(p => p.Category)
                .ThenBy(p => p.Name)
                .ToListAsync();

            var productosDestacados = productos
                .GroupBy(p => p.Category!)
                .Select(g => g.First())
                .ToList();

            return View(productosDestacados);
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}