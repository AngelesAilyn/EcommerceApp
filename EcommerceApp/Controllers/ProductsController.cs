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
                .Include(p => p.CategoryNavigation)
                .Where(p => !p.IsArchived &&
                       p.IsAvailable &&
                       p.Stock > 0 &&
                       p.CategoryNavigation != null &&
                       p.CategoryNavigation.IsActive);

            if (!string.IsNullOrWhiteSpace(category))
            {
                products = products.Where(p =>
                    p.CategoryNavigation != null &&
                    p.CategoryNavigation.Name == category);
            }

            ViewBag.Categories = await context.Categories
                .AsNoTracking()
                .Where(c => c.IsActive)
                .Select(c => c.Name)
                .OrderBy(c => c)
                .ToListAsync();

            ViewBag.SelectedCategory = category;

            return View(await products
                .OrderBy(p => p.CategoryNavigation!.Name)
                .ThenBy(p => p.Name)
                .ToListAsync());
        }

        [AllowAnonymous]
        public async Task<IActionResult> Details(int id)
        {
            var product = await context.Products
                .Include(p => p.CategoryNavigation)
                .FirstOrDefaultAsync(p =>
                    p.Id == id &&
                    !p.IsArchived &&
                    p.IsAvailable &&
                    p.Stock > 0 &&
                    p.CategoryNavigation != null &&
                    p.CategoryNavigation.IsActive);

            if (product == null)
                return NotFound();

            return View(product);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = await context.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(Product product)
        {
            if (!product.CategoryId.HasValue)
            {
                ModelState.AddModelError(
                    "CategoryId",
                    "Debe seleccionar una categoría.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Categories = await context.Categories
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.Name)
                    .ToListAsync();

                return View(product);
            }

            var category = await context.Categories
                .FirstOrDefaultAsync(c =>
                    c.Id == product.CategoryId &&
                    c.IsActive);

            if (category == null)
            {
                ModelState.AddModelError(
                    "CategoryId",
                    "La categoría seleccionada no es válida.");

                ViewBag.Categories = await context.Categories
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.Name)
                    .ToListAsync();

                return View(product);
            }

            product.Category = category.Name;
            product.IsAvailable = product.Stock > 0;
            product.IsArchived = product.Stock <= 0;
            product.CreatedAt = DateTime.UtcNow;

            context.Products.Add(product);
            await context.SaveChangesAsync();

            var inventory = new Inventory
            {
                ProductId = product.Id,
                CurrentQuantity = product.Stock,
                MinimumStock = 5,
                UpdatedAt = DateTime.UtcNow
            };

            context.Inventories.Add(inventory);
            await context.SaveChangesAsync();

            TempData["SuccessMessage"] = product.IsArchived
                ? "Producto creado y archivado porque su stock es 0."
                : "Producto creado correctamente.";

            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var product = await context.Products.FindAsync(id);

            if (product == null)
                return NotFound();

            ViewBag.Categories = await context.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();

            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, Product product)
        {
            if (id != product.Id)
                return NotFound();

            if (!product.CategoryId.HasValue)
            {
                ModelState.AddModelError(
                    "CategoryId",
                    "Debe seleccionar una categoría.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Categories = await context.Categories
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.Name)
                    .ToListAsync();

                return View(product);
            }

            var existingProduct = await context.Products.FindAsync(id);

            if (existingProduct == null)
                return NotFound();

            var category = await context.Categories
                .FirstOrDefaultAsync(c =>
                    c.Id == product.CategoryId &&
                    c.IsActive);

            if (category == null)
            {
                ModelState.AddModelError(
                    "CategoryId",
                    "La categoría seleccionada no es válida.");

                ViewBag.Categories = await context.Categories
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.Name)
                    .ToListAsync();

                return View(product);
            }

            existingProduct.Name = product.Name;
            existingProduct.Description = product.Description;
            existingProduct.Price = product.Price;
            existingProduct.Stock = product.Stock;
            existingProduct.ImageUrl = product.ImageUrl;
            existingProduct.CategoryId = product.CategoryId;
            existingProduct.Category = category.Name;
            existingProduct.ExpirationDate = product.ExpirationDate;
            existingProduct.IsAvailable = product.Stock > 0;
            existingProduct.IsArchived = product.Stock <= 0;
            existingProduct.UpdatedAt = DateTime.UtcNow;

            var inventory = await context.Inventories
                .FirstOrDefaultAsync(i => i.ProductId == existingProduct.Id);

            if (inventory == null)
            {
                inventory = new Inventory
                {
                    ProductId = existingProduct.Id,
                    CurrentQuantity = product.Stock,
                    MinimumStock = 5,
                    UpdatedAt = DateTime.UtcNow
                };

                context.Inventories.Add(inventory);
            }
            else
            {
                inventory.CurrentQuantity = product.Stock;
                inventory.UpdatedAt = DateTime.UtcNow;
            }

            await context.SaveChangesAsync();

            TempData["SuccessMessage"] = existingProduct.IsArchived
                ? "Producto actualizado y archivado automáticamente porque su stock llegó a 0."
                : "Producto actualizado correctamente.";

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
            product.IsAvailable = false;
            product.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Producto archivado correctamente.";
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

            if (product.Stock <= 0)
            {
                TempData["ErrorMessage"] = "No se puede desarchivar un producto sin stock. Primero actualiza su inventario.";
                return RedirectToAction(nameof(Archived));
            }

            product.IsArchived = false;
            product.IsAvailable = true;
            product.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Producto recuperado correctamente.";
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

            TempData["SuccessMessage"] = "Producto eliminado correctamente.";
            return RedirectToAction(nameof(Index));
        }
    }
}