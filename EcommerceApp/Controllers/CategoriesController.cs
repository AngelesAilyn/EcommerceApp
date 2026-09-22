using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceApp.Data;
using EcommerceApp.Models;

namespace EcommerceApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CategoriesController(ApplicationDbContext context) : Controller
    {
        // LISTAR CATEGORÍAS
        public async Task<IActionResult> Index()
        {
            var categories = await context.Categories
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .ToListAsync();

            return View(categories);
        }

        // CREAR CATEGORÍA
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category category)
        {
            if (!ModelState.IsValid)
                return View(category);

            var exists = await context.Categories
                .AnyAsync(c => c.Name.ToLower() == category.Name.ToLower());

            if (exists)
            {
                ModelState.AddModelError(
                    "Name",
                    "Ya existe una categoría con ese nombre.");

                return View(category);
            }

            category.Name = category.Name.Trim();
            category.IsActive = true;

            context.Categories.Add(category);
            await context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Categoría creada correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // EDITAR CATEGORÍA
        public async Task<IActionResult> Edit(int id)
        {
            var category = await context.Categories.FindAsync(id);

            if (category == null)
                return NotFound();

            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Category category)
        {
            if (id != category.Id)
                return NotFound();

            if (!ModelState.IsValid)
                return View(category);

            var existingCategory = await context.Categories
                .FindAsync(id);

            if (existingCategory == null)
                return NotFound();

            var exists = await context.Categories
                .AnyAsync(c =>
                    c.Id != id &&
                    c.Name.ToLower() == category.Name.ToLower());

            if (exists)
            {
                ModelState.AddModelError(
                    "Name",
                    "Ya existe otra categoría con ese nombre.");

                return View(category);
            }

            existingCategory.Name = category.Name.Trim();
            existingCategory.Description = category.Description;

            await context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Categoría actualizada correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // ACTIVAR / DESACTIVAR
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var category = await context.Categories.FindAsync(id);

            if (category == null)
                return NotFound();

            category.IsActive = !category.IsActive;

            await context.SaveChangesAsync();

            TempData["SuccessMessage"] = category.IsActive
                ? "Categoría activada correctamente."
                : "Categoría desactivada correctamente.";
            return RedirectToAction(nameof(Index));
        }
    }
}