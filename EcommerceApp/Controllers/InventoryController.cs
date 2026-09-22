using EcommerceApp.Data;
using EcommerceApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class InventoryController : Controller
    {
        private readonly ApplicationDbContext _context;

        public InventoryController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? category)
        {
            var productsWithoutInventory = await _context.Products
                .Where(p => !p.IsArchived &&
                            !_context.Inventories.Any(i => i.ProductId == p.Id))
                .ToListAsync();

            if (productsWithoutInventory.Count > 0)
            {
                foreach (var product in productsWithoutInventory)
                {
                    _context.Inventories.Add(new Inventory
                    {
                        ProductId = product.Id,
                        CurrentQuantity = product.Stock,
                        MinimumStock = 5,
                        UpdatedAt = DateTime.UtcNow
                    });
                }

                await _context.SaveChangesAsync();
            }

            var inventarioQuery = _context.Inventories
                .Include(i => i.Product)
                .ThenInclude(p => p.CategoryNavigation)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(category))
            {
                inventarioQuery = inventarioQuery
                    .Where(i => i.Product!.CategoryNavigation!.Name == category);
            }

            var inventario = await inventarioQuery
                .OrderBy(i => i.Product!.Name)
                .ToListAsync();

            var categorias = await _context.Categories
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .ToListAsync();

            ViewBag.Categorias = categorias;
            ViewBag.CategoriaSeleccionada = category;

            return View(inventario);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var inventory = await _context.Inventories
                .Include(i => i.Product)
                .ThenInclude(p => p.CategoryNavigation)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (inventory == null)
                return NotFound();

            return View(inventory);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            int currentQuantity,
            int minimumStock)
        {
            var inventory = await _context.Inventories
                .Include(i => i.Product)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (inventory == null)
                return NotFound();

            if (currentQuantity < 0 || minimumStock < 0)
            {
                ModelState.AddModelError(
                    "",
                    "Las cantidades no pueden ser negativas.");

                inventory.CurrentQuantity = currentQuantity;
                inventory.MinimumStock = minimumStock;

                return View(inventory);
            }

            inventory.CurrentQuantity = currentQuantity;
            inventory.MinimumStock = minimumStock;
            inventory.UpdatedAt = DateTime.UtcNow;

            if (inventory.Product != null)
            {
                inventory.Product.Stock = currentQuantity;
                inventory.Product.IsAvailable = currentQuantity > 0;
                inventory.Product.IsArchived = currentQuantity <= 0;
                inventory.Product.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = currentQuantity <= 0
                ? "Inventario actualizado. El producto fue archivado automáticamente por falta de stock."
                : "Inventario actualizado correctamente.";

            return RedirectToAction(nameof(Index));
        }
    }
}