using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using cloudDev.Models;
using cloudDev.Services;
using System.Security.Claims;

namespace cloudDev.Controllers;

public class ItemsController : Controller
{
    private readonly TableStorageService _tableStorageService;
    private readonly BlobStorageService _blobStorageService;

    public ItemsController(TableStorageService tableStorageService, BlobStorageService blobStorageService)
    {
        _tableStorageService = tableStorageService;
        _blobStorageService = blobStorageService;
    }

    [Authorize]
    public IActionResult AddItem()
    {
        // Check if user is admin
        var isAdmin = User.FindFirst("IsAdmin")?.Value == "True";
        if (!isAdmin)
        {
            return Forbid();
        }

        return View();
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> AddItem(AddItemViewModel model)
    {
        // Check if user is admin
        var isAdmin = User.FindFirst("IsAdmin")?.Value == "True";
        if (!isAdmin)
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            // Debug: Log the received price value
            Console.WriteLine($"🔍 DEBUG: Received price value: {model.Price}");
            Console.WriteLine($"🔍 DEBUG: Price type: {model.Price.GetType()}");
            Console.WriteLine($"🔍 DEBUG: Price as string: '{model.Price.ToString()}'");
            
            string imageUrl = "";
            if (model.ImageFile != null)
            {
                imageUrl = await _blobStorageService.UploadImageAsync(model.ImageFile);
            }

            var item = new ItemsModel
            {
                PartitionKey = "Items",
                RowKey = Guid.NewGuid().ToString(),
                Name = model.Name,
                Description = model.Description,
                price = (double)model.Price,
                quantity = model.Quantity,
                Category = model.Category,
                Image = imageUrl
            };
            
            Console.WriteLine($"🔍 DEBUG: Item price before save: {item.price}");

            await _tableStorageService.CreateItemAsync(item);
            TempData["Success"] = "Item added successfully!";
            return RedirectToAction("AddItem");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", "Error adding item: " + ex.Message);
            return View(model);
        }
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> AddToCart(string itemId)
    {
        var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
        if (string.IsNullOrEmpty(userEmail))
        {
            return RedirectToAction("Login", "Account");
        }

        try
        {
            // Get the item details
            var item = await _tableStorageService.GetItemAsync("Items", itemId);
            if (item == null)
            {
                TempData["Error"] = "Item not found";
                return RedirectToAction("Index", "Home");
            }

            // Check if item already in cart
            var existingCartItems = await _tableStorageService.GetCartItemsAsync(userEmail);
            var existingItem = existingCartItems.FirstOrDefault(c => c.ItemId == itemId);

            if (existingItem != null)
            {
                // Update quantity
                existingItem.Quantity += 1;
                await _tableStorageService.UpdateCartItemAsync(existingItem);
            }
            else
            {
                // Add new cart item
                var cartItem = new CartModel
                {
                    PartitionKey = "Cart",
                    RowKey = Guid.NewGuid().ToString(),
                    CustomerEmail = userEmail,
                    ItemId = itemId,
                    ItemName = item.Name,
                    ItemPrice = item.price,
                    Quantity = 1,
                    ItemImage = item.Image
                };

                await _tableStorageService.AddToCartAsync(cartItem);
            }

            TempData["Success"] = "Item added to cart!";
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Error adding item to cart: " + ex.Message;
        }

        return RedirectToAction("Index", "Home");
    }

    [Authorize]
    public async Task<IActionResult> Cart()
    {
        var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
        if (string.IsNullOrEmpty(userEmail))
        {
            return RedirectToAction("Login", "Account");
        }

        var cartItems = await _tableStorageService.GetCartItemsAsync(userEmail);
        var cartViewModel = new CartViewModel
        {
            Items = cartItems.Select(c => new CartItemViewModel
            {
                ItemId = c.ItemId,
                ItemName = c.ItemName,
                ItemPrice = (decimal)c.ItemPrice,
                Quantity = c.Quantity,
                ItemImage = c.ItemImage
            }).ToList()
        };

        return View(cartViewModel);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> UpdateCartQuantity(string itemId, int quantity)
    {
        var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
        if (string.IsNullOrEmpty(userEmail))
        {
            return Json(new { success = false, message = "User not authenticated" });
        }

        try
        {
            var cartItems = await _tableStorageService.GetCartItemsAsync(userEmail);
            var cartItem = cartItems.FirstOrDefault(c => c.ItemId == itemId);

            if (cartItem != null)
            {
                if (quantity <= 0)
                {
                    await _tableStorageService.RemoveFromCartAsync(cartItem.PartitionKey, cartItem.RowKey);
                }
                else
                {
                    cartItem.Quantity = quantity;
                    await _tableStorageService.UpdateCartItemAsync(cartItem);
                }
            }

            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> RemoveFromCart(string itemId)
    {
        var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
        if (string.IsNullOrEmpty(userEmail))
        {
            return RedirectToAction("Login", "Account");
        }

        try
        {
            var cartItems = await _tableStorageService.GetCartItemsAsync(userEmail);
            var cartItem = cartItems.FirstOrDefault(c => c.ItemId == itemId);

            if (cartItem != null)
            {
                await _tableStorageService.RemoveFromCartAsync(cartItem.PartitionKey, cartItem.RowKey);
                TempData["Success"] = "Item removed from cart";
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Error removing item: " + ex.Message;
        }

        return RedirectToAction("Cart");
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Checkout()
    {
        var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
        var userName = User.FindFirst(ClaimTypes.Name)?.Value;
        
        if (string.IsNullOrEmpty(userEmail))
        {
            return RedirectToAction("Login", "Account");
        }

        try
        {
            var cartItems = await _tableStorageService.GetCartItemsAsync(userEmail);
            var cartList = cartItems.ToList();
            
            if (!cartList.Any())
            {
                TempData["Error"] = "Your cart is empty";
                return RedirectToAction("Cart");
            }

            // Process each cart item as a sale
            foreach (var cartItem in cartList)
            {
                // Record the sale
                var sale = new SalesModel
                {
                    PartitionKey = "Sales",
                    RowKey = Guid.NewGuid().ToString(),
                    CustomerEmail = userEmail,
                    CustomerName = userName ?? "Unknown",
                    ItemId = cartItem.ItemId,
                    ItemName = cartItem.ItemName,
                    ItemCategory = "Unknown", // We'll need to get this from the item
                    ItemPrice = cartItem.ItemPrice,
                    QuantitySold = cartItem.Quantity,
                    TotalAmount = cartItem.ItemPrice * cartItem.Quantity,
                    SaleDate = DateTime.UtcNow
                };

                // Get item details to update category and reduce stock
                try
                {
                    var item = await _tableStorageService.GetItemAsync("Items", cartItem.ItemId);
                    sale.ItemCategory = item.Category;
                    
                    // Reduce stock
                    if (item.quantity >= cartItem.Quantity)
                    {
                        item.quantity -= cartItem.Quantity;
                        await _tableStorageService.UpdateItemAsync(item);
                    }
                    else
                    {
                        TempData["Error"] = $"Insufficient stock for {item.Name}";
                        return RedirectToAction("Cart");
                    }
                }
                catch (Exception)
                {
                    // Item not found, but continue with sale recording
                }

                await _tableStorageService.RecordSaleAsync(sale);
            }

            // Clear the cart
            await _tableStorageService.ClearCartAsync(userEmail);
            
            TempData["Success"] = $"Order completed successfully! {cartList.Count} items purchased.";
            return RedirectToAction("Index", "Home");
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Error processing checkout: " + ex.Message;
            return RedirectToAction("Cart");
        }
    }
}
