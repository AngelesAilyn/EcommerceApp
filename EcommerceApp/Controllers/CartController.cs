using System.Text.Json;
using EcommerceApp.Data;
using EcommerceApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApp.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CartController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var cart = GetCart();

            var productIds = cart.Select(x => x.ProductId).ToList();

            var products = await _context.Products
                .Where(p => productIds.Contains(p.Id) && !p.IsArchived)
                .ToListAsync();

            var items = cart
                .Where(item => products.Any(p => p.Id == item.ProductId))
                .Select(item =>
                {
                    var product = products.First(p => p.Id == item.ProductId);

                    return new CartItemViewModel
                    {
                        ProductId = product.Id,
                        Name = product.Name,
                        ImageUrl = product.ImageUrl,
                        Price = product.Price,
                        Quantity = item.Quantity,
                        Subtotal = product.Price * item.Quantity
                    };
                })
                .ToList();

            return View(items);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int productId, int quantity = 1)
        {
            if (quantity < 1)
                quantity = 1;

            var product = await _context.Products
                .FirstOrDefaultAsync(p =>
                    p.Id == productId &&
                    !p.IsArchived &&
                    p.IsAvailable);

            if (product == null)
            {
                TempData["ErrorMessage"] = "El producto no está disponible.";
                return RedirectToAction("Index", "Products");
            }

            if (product.Stock < quantity)
            {
                TempData["ErrorMessage"] = "No hay suficiente stock disponible.";
                return RedirectToAction("Details", "Products", new { id = productId });
            }

            var cart = GetCart();

            var existingItem = cart.FirstOrDefault(x => x.ProductId == productId);

            if (existingItem != null)
            {
                if (existingItem.Quantity + quantity > product.Stock)
                {
                    TempData["ErrorMessage"] = "No hay suficiente stock para agregar esa cantidad.";
                    return RedirectToAction("Details", "Products", new { id = productId });
                }

                existingItem.Quantity += quantity;
            }
            else
            {
                cart.Add(new CartItem
                {
                    ProductId = productId,
                    Quantity = quantity
                });
            }

            SaveCart(cart);

            TempData["SuccessMessage"] = "Producto agregado al carrito.";

            return RedirectToAction("Index", "Products");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Increase(int productId)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == productId && !p.IsArchived);

            if (product == null)
                return RedirectToAction(nameof(Index));

            var cart = GetCart();
            var item = cart.FirstOrDefault(x => x.ProductId == productId);

            if (item != null)
            {
                if (item.Quantity < product.Stock)
                {
                    item.Quantity++;
                    SaveCart(cart);
                }
                else
                {
                    TempData["ErrorMessage"] = "No hay más stock disponible.";
                }
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Decrease(int productId)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(x => x.ProductId == productId);

            if (item != null)
            {
                item.Quantity--;

                if (item.Quantity <= 0)
                {
                    cart.Remove(item);
                }

                SaveCart(cart);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Remove(int productId)
        {
            var cart = GetCart();

            var item = cart.FirstOrDefault(x => x.ProductId == productId);

            if (item != null)
            {
                cart.Remove(item);
                SaveCart(cart);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Clear()
        {
            HttpContext.Session.Remove("Cart");

            TempData["SuccessMessage"] = "Carrito vaciado.";

            return RedirectToAction(nameof(Index));
        }

        private List<CartItem> GetCart()
        {
            var cartJson = HttpContext.Session.GetString("Cart");

            if (string.IsNullOrEmpty(cartJson))
                return new List<CartItem>();

            return JsonSerializer.Deserialize<List<CartItem>>(cartJson)
                   ?? new List<CartItem>();
        }

        private void SaveCart(List<CartItem> cart)
        {
            var cartJson = JsonSerializer.Serialize(cart);

            HttpContext.Session.SetString("Cart", cartJson);
        }
    }

    public class CartItem
    {
        public int ProductId { get; set; }

        public int Quantity { get; set; }
    }

    public class CartItemViewModel
    {
        public int ProductId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? ImageUrl { get; set; }

        public decimal Price { get; set; }

        public int Quantity { get; set; }

        public decimal Subtotal { get; set; }
    }
}