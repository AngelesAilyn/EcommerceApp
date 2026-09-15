using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceApp.Data;
using EcommerceApp.Models;

namespace EcommerceApp.Controllers
{
    [Authorize]
    public class ProductsController(ApplicationDbContext context) : Controller
    {
        [AllowAnonymous]
        public async Task<IActionResult> Index(string? category)
        {
            var products = context.Products
                .AsNoTracking()
                .Where(p => !p.IsArchived);

            if (!string.IsNullOrEmpty(category))
            {
                products = products.Where(p => p.Category == category);
            }

            ViewBag.Categories = await context.Products
                .Where(p => !p.IsArchived &&
                            p.Category != null &&
                            p.Category != "")
                .Select(p => p.Category!)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            ViewBag.SelectedCategory = category;

            return View(await products.ToListAsync());
        }

        [AllowAnonymous]
        public async Task<IActionResult> Details(int id)
        {
            var product = await context.Products
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsArchived);

            if (product == null)
                return NotFound();

            return View(product);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(Product product)
        {
            if (!ModelState.IsValid)
                return View(product);

            product.IsArchived = false;
            product.CreatedAt = DateTime.UtcNow;

            context.Products.Add(product);
            await context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var product = await context.Products.FindAsync(id);

            if (product == null)
                return NotFound();

            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, Product product)
        {
            if (id != product.Id)
                return NotFound();

            if (!ModelState.IsValid)
                return View(product);

            var existingProduct = await context.Products.FindAsync(id);

            if (existingProduct == null)
                return NotFound();

            existingProduct.Name = product.Name;
            existingProduct.Description = product.Description;
            existingProduct.Price = product.Price;
            existingProduct.Stock = product.Stock;
            existingProduct.ImageUrl = product.ImageUrl;
            existingProduct.Category = product.Category;
            existingProduct.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Archive(int id)
        {
            var product = await context.Products.FindAsync(id);

            if (product == null)
                return NotFound();

            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        [ActionName("Archive")]
        public async Task<IActionResult> ArchiveConfirmed(int id)
        {
            var product = await context.Products.FindAsync(id);

            if (product == null)
                return NotFound();

            product.IsArchived = true;
            product.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Archived()
        {
            var products = await context.Products
                .AsNoTracking()
                .Where(p => p.IsArchived)
                .OrderByDescending(p => p.UpdatedAt ?? p.CreatedAt)
                .ToListAsync();

            return View(products);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Unarchive(int id)
        {
            var product = await context.Products.FindAsync(id);

            if (product == null)
                return NotFound();

            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        [ActionName("Unarchive")]
        public async Task<IActionResult> UnarchiveConfirmed(int id)
        {
            var product = await context.Products.FindAsync(id);

            if (product == null)
                return NotFound();

            product.IsArchived = false;
            product.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();

            return RedirectToAction(nameof(Archived));
        }
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await context.Products.FindAsync(id);

            if (product == null)
                return NotFound();

            return View(product);
        }

        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await context.Products.FindAsync(id);

            if (product != null)
            {
                context.Products.Remove(product);
                await context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}