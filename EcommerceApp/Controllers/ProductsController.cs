using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceApp.Data;
using EcommerceApp.Models;
using System.Globalization;
using System.Text;

namespace EcommerceApp.Controllers
{
    [Authorize]
    public class ProductsController(ApplicationDbContext context) : Controller
    {
        [AllowAnonymous]
        public async Task<IActionResult> Index(
            string? category,
            string? search)
        {

            var productsQuery = context.Products
                .AsNoTracking()
                .Include(p => p.CategoryNavigation)
                .Where(p =>
                    !p.IsArchived &&
                    p.IsAvailable &&
                    p.Stock > 0 &&
                    p.CategoryNavigation != null &&
                    p.CategoryNavigation.IsActive);


            var cleanSearch = NormalizarTexto(search);


            if (!string.IsNullOrWhiteSpace(category))
            {
                productsQuery = productsQuery.Where(p =>
                    p.CategoryNavigation != null &&
                    p.CategoryNavigation.Name == category);
            }


            List<Product> products;

            if (!string.IsNullOrWhiteSpace(cleanSearch))
            {
                var allProductsForSearch = await productsQuery
                    .OrderBy(p => p.CategoryNavigation!.Name)
                    .ThenBy(p => p.Name)
                    .ToListAsync();

                products = allProductsForSearch
                    .Where(p =>
                        NormalizarTexto(p.Name)
                            .Contains(cleanSearch)
                        ||
                        NormalizarTexto(p.Description)
                            .Contains(cleanSearch))
                    .ToList();
            }
            else
            {
                products = await productsQuery
                    .OrderBy(p => p.CategoryNavigation!.Name)
                    .ThenBy(p => p.Name)
                    .ToListAsync();
            }



            ViewBag.Categories = await context.Categories
                .AsNoTracking()
                .Where(c => c.IsActive)
                .Select(c => c.Name)
                .OrderBy(c => c)
                .ToListAsync();



            ViewBag.SelectedCategory = category;
            ViewBag.Search = cleanSearch;


            if (!string.IsNullOrWhiteSpace(cleanSearch) &&
                !string.IsNullOrWhiteSpace(category) &&
                products.Count == 0)
            {
                ViewBag.SearchMessage =
                    $"No encontramos \"{search}\" en la categoría \"{category}\".";

                ViewBag.CanSearchAllCategories = true;
            }
            else if (!string.IsNullOrWhiteSpace(cleanSearch) &&
                     products.Count == 0)
            {
                ViewBag.SearchMessage =
                    $"No encontramos productos relacionados con \"{search}\".";

                ViewBag.CanSearchAllCategories = false;
            }
            else if (!string.IsNullOrWhiteSpace(cleanSearch))
            {
                ViewBag.SearchMessage =
                    $"Encontrados: {products.Count} producto(s).";

                ViewBag.CanSearchAllCategories = false;
            }


            return View(products);
        }


        private static string NormalizarTexto(string? texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return string.Empty;

            texto = texto
                .Trim()
                .ToLowerInvariant()
                .Normalize(NormalizationForm.FormD);

            var resultado = new StringBuilder();

            foreach (var caracter in texto)
            {
                var categoria = CharUnicodeInfo.GetUnicodeCategory(caracter);

                if (categoria == UnicodeCategory.NonSpacingMark)
                    continue;

                if (char.IsLetterOrDigit(caracter) ||
                    char.IsWhiteSpace(caracter))
                {
                    resultado.Append(caracter);
                }
            }

            return string.Join(
                " ",
                resultado
                    .ToString()
                    .Split(
                        ' ',
                        StringSplitOptions.RemoveEmptyEntries));
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
                .FirstOrDefaultAsync(i =>
                    i.ProductId == existingProduct.Id);

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

            TempData["SuccessMessage"] =
                "Producto archivado correctamente.";

            return RedirectToAction(nameof(Index));
        }


        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Archived()
        {
            var products = await context.Products
                .AsNoTracking()
                .Where(p => p.IsArchived)
                .OrderByDescending(p =>
                    p.UpdatedAt ?? p.CreatedAt)
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
                TempData["ErrorMessage"] =
                    "No se puede desarchivar un producto sin stock. Primero actualiza su inventario.";

                return RedirectToAction(nameof(Archived));
            }

            product.IsArchived = false;
            product.IsAvailable = true;
            product.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Producto recuperado correctamente.";

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

            TempData["SuccessMessage"] =
                "Producto eliminado correctamente.";

            return RedirectToAction(nameof(Index));
        }
    }
}