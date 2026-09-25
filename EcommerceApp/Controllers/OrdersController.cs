using System.Security.Claims;
using System.Text.Json;
using EcommerceApp.Data;
using EcommerceApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApp.Controllers
{
    [Authorize]
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrdersController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            var cartJson = HttpContext.Session.GetString("Cart");

            if (string.IsNullOrEmpty(cartJson))
            {
                TempData["ErrorMessage"] = "Tu carrito está vacío.";
                return RedirectToAction("Index", "Cart");
            }

            var cart = JsonSerializer.Deserialize<List<CartItem>>(cartJson)
                       ?? new List<CartItem>();

            if (!cart.Any())
            {
                TempData["ErrorMessage"] = "Tu carrito está vacío.";
                return RedirectToAction("Index", "Cart");
            }

            var productIds = cart.Select(x => x.ProductId).ToList();

            var products = await _context.Products
                .Where(p => productIds.Contains(p.Id))
                .ToListAsync();

            var items = new List<CheckoutItemViewModel>();

            foreach (var item in cart)
            {
                var product = products.FirstOrDefault(
                    p => p.Id == item.ProductId);

                if (product == null ||
                    product.IsArchived ||
                    !product.IsAvailable)
                {
                    TempData["ErrorMessage"] =
                        "Uno de los productos de tu carrito ya no está disponible.";

                    return RedirectToAction("Index", "Cart");
                }

                if (item.Quantity > product.Stock)
                {
                    TempData["ErrorMessage"] =
                        $"No hay suficiente stock disponible para {product.Name}.";

                    return RedirectToAction("Index", "Cart");
                }

                items.Add(new CheckoutItemViewModel
                {
                    ProductId = product.Id,
                    Name = product.Name,
                    Price = product.Price,
                    Quantity = item.Quantity,
                    Subtotal = product.Price * item.Quantity
                });
            }

            var model = new CheckoutViewModel
            {
                Items = items,
                Total = items.Sum(x => x.Subtotal)
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(
            string paymentMethod,
            string deliveryMethod)
        {
            var validPaymentMethods = new[]
            {
                "Efectivo",
                "QR",
                "Transferencia"
            };

            var validDeliveryMethods = new[]
            {
                "Delivery",
                "Recojo en tienda"
            };

            if (!validPaymentMethods.Contains(paymentMethod))
            {
                TempData["ErrorMessage"] =
                    "Selecciona un método de pago válido.";

                return RedirectToAction(nameof(Checkout));
            }

            if (!validDeliveryMethods.Contains(deliveryMethod))
            {
                TempData["ErrorMessage"] =
                    "Selecciona una forma de entrega válida.";

                return RedirectToAction(nameof(Checkout));
            }

            var cartJson = HttpContext.Session.GetString("Cart");

            if (string.IsNullOrEmpty(cartJson))
            {
                TempData["ErrorMessage"] = "Tu carrito está vacío.";
                return RedirectToAction("Index", "Cart");
            }

            var cart = JsonSerializer.Deserialize<List<CartItem>>(cartJson)
                       ?? new List<CartItem>();

            if (!cart.Any())
            {
                TempData["ErrorMessage"] = "Tu carrito está vacío.";
                return RedirectToAction("Index", "Cart");
            }

            var productIds = cart.Select(x => x.ProductId).ToList();

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                var products = await _context.Products
                    .Where(p => productIds.Contains(p.Id))
                    .ToListAsync();

                foreach (var item in cart)
                {
                    var product = products.FirstOrDefault(
                        p => p.Id == item.ProductId);

                    if (product == null ||
                        product.IsArchived ||
                        !product.IsAvailable)
                    {
                        await transaction.RollbackAsync();

                        TempData["ErrorMessage"] =
                            "Uno de los productos ya no está disponible.";

                        return RedirectToAction("Index", "Cart");
                    }

                    if (item.Quantity > product.Stock)
                    {
                        await transaction.RollbackAsync();

                        TempData["ErrorMessage"] =
                            $"No hay suficiente stock disponible para {product.Name}.";

                        return RedirectToAction("Index", "Cart");
                    }
                }

                var userId = User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

                if (string.IsNullOrEmpty(userId))
                {
                    await transaction.RollbackAsync();

                    return RedirectToAction(
                        "Login",
                        "Account",
                        new
                        {
                            returnUrl = Url.Action(
                                "Checkout",
                                "Orders")
                        });
                }

                var order = new Order
                {
                    UserId = userId,
                    OrderDate = DateTime.UtcNow,

                    // Estado inicial del pedido
                    Status = "Pendiente",

                    // Datos del pago
                    PaymentMethod = paymentMethod,
                    PaymentStatus = "Pendiente",

                    // Forma de entrega
                    DeliveryMethod = deliveryMethod,

                    Total = 0
                };

                _context.Orders.Add(order);

                await _context.SaveChangesAsync();

                decimal total = 0;

                foreach (var item in cart)
                {
                    var product = products.First(
                        p => p.Id == item.ProductId);

                    var subtotal =
                        product.Price * item.Quantity;

                    var detail = new OrderDetail
                    {
                        OrderId = order.Id,
                        ProductId = product.Id,
                        Quantity = item.Quantity,
                        UnitPrice = product.Price,
                        Subtotal = subtotal
                    };

                    _context.OrderDetails.Add(detail);

                    product.Stock -= item.Quantity;

                    if (product.Stock <= 0)
                    {
                        product.Stock = 0;
                        product.IsAvailable = false;
                        product.IsArchived = true;
                    }

                    var inventory =
                        await _context.Inventories
                            .FirstOrDefaultAsync(i =>
                                i.ProductId == product.Id);

                    if (inventory != null)
                    {
                        inventory.CurrentQuantity =
                            product.Stock;

                        inventory.UpdatedAt =
                            DateTime.UtcNow;
                    }

                    total += subtotal;
                }

                order.Total = total;

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                HttpContext.Session.Remove("Cart");

                TempData["SuccessMessage"] =
                    "Tu pedido fue registrado correctamente.";

                return RedirectToAction(
                    nameof(Details),
                    new { id = order.Id });
            }
            catch
            {
                await transaction.RollbackAsync();

                TempData["ErrorMessage"] =
                    "No fue posible completar el pedido. Intenta nuevamente.";

                return RedirectToAction(
                    "Index",
                    "Cart");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Challenge();

            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(o =>
                    o.Id == id &&
                    o.UserId == userId);

            if (order == null)
                return NotFound();

            return View(order);
        }

        [HttpGet]
        public async Task<IActionResult> MyOrders()
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Challenge();

            var orders = await _context.Orders
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .AsNoTracking()
                .ToListAsync();

            return View(orders);
        }
    }

    public class CheckoutViewModel
    {
        public List<CheckoutItemViewModel> Items { get; set; }
            = new();

        public decimal Total { get; set; }
    }

    public class CheckoutItemViewModel
    {
        public int ProductId { get; set; }

        public string Name { get; set; }
            = string.Empty;

        public decimal Price { get; set; }

        public int Quantity { get; set; }

        public decimal Subtotal { get; set; }
    }
}