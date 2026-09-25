using EcommerceApp.Data;
using EcommerceApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceApp.Reports;
using QuestPDF.Fluent;

namespace EcommerceApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ReportsController(ApplicationDbContext context) : Controller
    {
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var orders = await context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderDetails)
                    .ThenInclude(d => d.Product)
                .OrderByDescending(o => o.OrderDate)
                .AsNoTracking()
                .ToListAsync();

            var ventasPorMes = new List<VentaMensualViewModel>();
            var hoy = DateTime.Now;

            for (int i = 4; i >= 0; i--)
            {
                var fecha = hoy.AddMonths(-i);
                var pedidosMes = orders.Where(o =>
                    o.OrderDate.ToLocalTime().Year == fecha.Year &&
                    o.OrderDate.ToLocalTime().Month == fecha.Month).ToList();

                ventasPorMes.Add(new VentaMensualViewModel
                {
                    Mes = fecha.ToString("MMM"),
                    Total = pedidosMes.Sum(o => o.Total),
                    Pedidos = pedidosMes.Count
                });
            }

            return View(new AdminDashboardViewModel
            {
                TotalPedidos = orders.Count,
                VentasTotales = orders.Sum(o => o.Total),
                PedidosPagados = orders.Count(o => string.Equals(o.PaymentStatus, "Pagado", StringComparison.OrdinalIgnoreCase)),
                PedidosPendientes = orders.Count(o => string.Equals(o.PaymentStatus, "Pendiente", StringComparison.OrdinalIgnoreCase)),
                VentasPorMes = ventasPorMes,
                ProductosMasSolicitados = orders.SelectMany(o => o.OrderDetails)
                    .Where(d => d.Product != null)
                    .GroupBy(d => d.Product!.Name)
                    .Select(g => new ProductoSolicitadoViewModel { Nombre = g.Key, Cantidad = g.Sum(d => d.Quantity) })
                    .OrderByDescending(x => x.Cantidad).Take(4).ToList(),
                Pedidos = orders.Select(o => new PedidoReporteViewModel
                {
                    Id = o.Id,
                    Fecha = o.OrderDate,
                    Cliente = o.User?.FullName ?? o.User?.Email ?? "Sin cliente",
                    Total = o.Total,
                    MetodoPago = o.PaymentMethod ?? "-",
                    EstadoPago = o.PaymentStatus ?? "-",
                    Entrega = o.DeliveryMethod ?? "-",
                    EstadoPedido = o.Status ?? "-"
                }).ToList(),
                FechaGeneracion = DateTime.Now
            });
        }

        [HttpGet]
        public async Task<IActionResult> Orders()
        {
            var orders = await context.Orders
                .Include(o => o.User)
                .OrderByDescending(o => o.OrderDate)
                .AsNoTracking()
                .ToListAsync();

            var rows = orders.Select(order => new OrdersPdfRow(
                $"#{order.Id}",
                order.OrderDate.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
                order.User?.FullName ?? order.User?.Email ?? "Sin cliente",
                order.Total,
                order.PaymentMethod ?? "-",
                order.PaymentStatus ?? "-",
                order.DeliveryMethod ?? "-",
                order.Status ?? "-"
            )).ToList();

            var document = new OrdersPdfDocument(rows, DateTime.Now);
            var pdf = document.GeneratePdf();

            return File(pdf, "application/pdf", "ReportePedidos.pdf");
        }
    }
}
