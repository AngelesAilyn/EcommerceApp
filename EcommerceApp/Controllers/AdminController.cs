using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceApp.Data;
using EcommerceApp.Models;

namespace EcommerceApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController(ApplicationDbContext context) : Controller
    {
        public async Task<IActionResult> Index()
        {
            var totalProductos = await context.Products
                .CountAsync(p => !p.IsArchived);

            var productosArchivados = await context.Products
                .CountAsync(p => p.IsArchived);

            var categoriasActivas = await context.Categories
                .CountAsync(c => c.IsActive);

            var productosStockBajo = await context.Products
                .CountAsync(p =>
                    !p.IsArchived &&
                    p.Stock > 0 &&
                    p.Stock <= 5);

            var productosSinStock = await context.Products
                .CountAsync(p =>
                    !p.IsArchived &&
                    p.Stock == 0);

            var productosVisuales = await context.Products
                .AsNoTracking()
                .Where(p =>
                    !p.IsArchived &&
                    !string.IsNullOrEmpty(p.ImageUrl))
                .OrderByDescending(p => p.CreatedAt)
                .Take(4)
                .ToListAsync();

            ViewBag.TotalProductos = totalProductos;
            ViewBag.ProductosArchivados = productosArchivados;
            ViewBag.CategoriasActivas = categoriasActivas;
            ViewBag.ProductosStockBajo = productosStockBajo;
            ViewBag.ProductosSinStock = productosSinStock;

            return View(productosVisuales);
        }

        [HttpGet]
        public async Task<IActionResult> Orders()
        {
            var orders = await context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderDetails)
                    .ThenInclude(d => d.Product)
                .OrderByDescending(o => o.OrderDate)
                .AsNoTracking()
                .ToListAsync();

            return View(orders);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsPaid(int id)
        {
            var order = await context.Orders
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                TempData["ErrorMessage"] = "El pedido no existe.";
                return RedirectToAction(nameof(Orders));
            }

            order.PaymentStatus = "Pagado";

            await context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                $"El pago del pedido #{order.Id} fue marcado como pagado.";

            return RedirectToAction(nameof(Orders));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOrderStatus(
            int id,
            string status)
        {
            var validStatuses = new[]
            {
                "Pendiente",
                "Enviado",
                "Entregado"
            };

            if (!validStatuses.Contains(status))
            {
                TempData["ErrorMessage"] =
                    "El estado seleccionado no es válido.";

                return RedirectToAction(nameof(Orders));
            }

            var order = await context.Orders
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                TempData["ErrorMessage"] =
                    "El pedido no existe.";

                return RedirectToAction(nameof(Orders));
            }

            order.Status = status;

            await context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                $"El pedido #{order.Id} ahora está en estado: {status}.";

            return RedirectToAction(nameof(Orders));
        }
    }
}