using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using cloudDev.Models;
using cloudDev.Services;
using System.Security.Claims;

namespace cloudDev.Controllers;

[Authorize]
public class AdminController : Controller
{
    private readonly TableStorageService _tableStorageService;
    private readonly BlobStorageService _blobStorageService;

    public AdminController(TableStorageService tableStorageService, BlobStorageService blobStorageService)
    {
        _tableStorageService = tableStorageService;
        _blobStorageService = blobStorageService;
    }

    private bool IsAdmin()
    {
        return User.FindFirst("IsAdmin")?.Value == "True";
    }

    public async Task<IActionResult> ManageItems(string searchQuery = "", string category = "")
    {
        if (!IsAdmin())
        {
            return Forbid();
        }

        var allItems = await _tableStorageService.GetAllItemsAsync();
        var items = allItems.AsEnumerable();

        // Filter by search query
        if (!string.IsNullOrEmpty(searchQuery))
        {
            items = items.Where(i => i.Name.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) ||
                                   i.Description.Contains(searchQuery, StringComparison.OrdinalIgnoreCase));
        }

        // Filter by category
        if (!string.IsNullOrEmpty(category))
        {
            items = items.Where(i => i.Category == category);
        }

        var itemsList = items.ToList();

        var viewModel = new ManageItemsViewModel
        {
            Items = itemsList,
            SearchQuery = searchQuery,
            SelectedCategory = category,
            TotalItems = itemsList.Count,
            LowStockItems = itemsList.Count(i => i.quantity <= 5),
            TotalInventoryValue = (decimal)itemsList.Sum(i => i.price * i.quantity)
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> EditItem(string id)
    {
        if (!IsAdmin())
        {
            return Forbid();
        }

        try
        {
            var item = await _tableStorageService.GetItemAsync("Items", id);
            
            // Debug: Log the item data being loaded
            Console.WriteLine($"🔍 DEBUG GET EditItem: Loading item with ID: {id}");
            Console.WriteLine($"🔍 DEBUG GET EditItem: Item name: {item.Name}");
            Console.WriteLine($"🔍 DEBUG GET EditItem: Item price from database: {item.price}");
            Console.WriteLine($"🔍 DEBUG GET EditItem: Item price type: {item.price.GetType()}");
            Console.WriteLine($"🔍 DEBUG GET EditItem: Item quantity: {item.quantity}");
            
            var viewModel = new EditItemViewModel
            {
                RowKey = item.RowKey,
                Name = item.Name,
                Description = item.Description,
                Price = (decimal)item.price,
                Quantity = item.quantity,
                Category = item.Category,
                CurrentImage = item.Image
            };
            
            Console.WriteLine($"🔍 DEBUG GET EditItem: ViewModel price: {viewModel.Price}");

            return View(viewModel);
        }
        catch (Exception)
        {
            TempData["Error"] = "Item not found";
            return RedirectToAction("ManageItems");
        }
    }

    [HttpPost]
    public async Task<IActionResult> EditItem(EditItemViewModel model)
    {
        if (!IsAdmin())
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
            Console.WriteLine($"🔍 DEBUG EditItem: Received price value: {model.Price}");
            Console.WriteLine($"🔍 DEBUG EditItem: Price type: {model.Price.GetType()}");
            Console.WriteLine($"🔍 DEBUG EditItem: Price as string: '{model.Price.ToString()}'");
            
            var item = await _tableStorageService.GetItemAsync("Items", model.RowKey);
            Console.WriteLine($"🔍 DEBUG EditItem: Original item price: {item.price}");
            
            // Update item properties
            item.Name = model.Name;
            item.Description = model.Description;
            item.price = (double)model.Price;
            item.quantity = model.Quantity;
            item.Category = model.Category;
            
            Console.WriteLine($"🔍 DEBUG EditItem: Item price after update: {item.price}");

            // Handle image upload if new image is provided
            if (model.NewImageFile != null)
            {
                // Delete old image if exists
                if (!string.IsNullOrEmpty(item.Image))
                {
                    await _blobStorageService.DeleteImageAsync(item.Image);
                }
                
                // Upload new image
                item.Image = await _blobStorageService.UploadImageAsync(model.NewImageFile);
            }

            await _tableStorageService.UpdateItemAsync(item);
            TempData["Success"] = "Item updated successfully!";
            return RedirectToAction("ManageItems");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", "Error updating item: " + ex.Message);
            return View(model);
        }
    }

    [HttpPost]
    public async Task<IActionResult> DeleteItem(string id)
    {
        if (!IsAdmin())
        {
            return Json(new { success = false, message = "Unauthorized" });
        }

        try
        {
            var item = await _tableStorageService.GetItemAsync("Items", id);
            
            // Delete associated image if exists
            if (!string.IsNullOrEmpty(item.Image))
            {
                await _blobStorageService.DeleteImageAsync(item.Image);
            }

            await _tableStorageService.DeleteItemAsync("Items", id);
            return Json(new { success = true, message = "Item deleted successfully" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Error deleting item: " + ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> UpdateStock(string id)
    {
        if (!IsAdmin())
        {
            return Forbid();
        }

        try
        {
            var item = await _tableStorageService.GetItemAsync("Items", id);
            var viewModel = new UpdateStockViewModel
            {
                ItemId = item.RowKey,
                ItemName = item.Name,
                CurrentStock = item.quantity,
                NewStock = item.quantity
            };

            return View(viewModel);
        }
        catch (Exception)
        {
            TempData["Error"] = "Item not found";
            return RedirectToAction("ManageItems");
        }
    }

    [HttpPost]
    public async Task<IActionResult> UpdateStock(UpdateStockViewModel model)
    {
        if (!IsAdmin())
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var item = await _tableStorageService.GetItemAsync("Items", model.ItemId);
            item.quantity = model.NewStock;
            
            await _tableStorageService.UpdateItemAsync(item);
            TempData["Success"] = $"Stock updated for {item.Name}. New quantity: {model.NewStock}";
            return RedirectToAction("ManageItems");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", "Error updating stock: " + ex.Message);
            return View(model);
        }
    }

    public async Task<IActionResult> SalesReport(DateTime? startDate = null, DateTime? endDate = null, string category = "", string customer = "")
    {
        if (!IsAdmin())
        {
            return Forbid();
        }

        var from = startDate ?? DateTime.Today.AddMonths(-1);
        var to = endDate ?? DateTime.Today.AddDays(1);

        var sales = await _tableStorageService.GetSalesAsync(from, to);
        var salesList = sales.ToList();

        // Apply filters
        if (!string.IsNullOrEmpty(category))
        {
            salesList = salesList.Where(s => s.ItemCategory.Equals(category, StringComparison.OrdinalIgnoreCase)).ToList();
        }
        
        if (!string.IsNullOrEmpty(customer))
        {
            salesList = salesList.Where(s => s.CustomerName.Contains(customer, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        // Get categories for dropdown from all sales (no date filter)
        var allSales = await _tableStorageService.GetSalesAsync();
        var categories = allSales.Select(s => s.ItemCategory).Distinct().ToList();

        var viewModel = new SalesReportViewModel
        {
            StartDate = from,
            EndDate = to,
            Category = category,
            Customer = customer,
            Categories = categories,
            TotalRevenue = (decimal)salesList.Sum(s => s.TotalAmount),
            BestSellingCategory = salesList.GroupBy(s => s.ItemCategory)
                                          .OrderByDescending(g => g.Sum(s => s.QuantitySold))
                                          .FirstOrDefault()?.Key ?? "N/A",
            TopCustomer = salesList.GroupBy(s => s.CustomerName)
                                  .OrderByDescending(g => g.Sum(s => s.TotalAmount))
                                  .FirstOrDefault()?.Key ?? "N/A",
            SalesTransactions = salesList.OrderByDescending(s => s.SaleDate).ToList()
        };

        return View(viewModel);
    }

    public async Task<IActionResult> Dashboard()
    {
        if (!IsAdmin())
        {
            return Forbid();
        }

        try
        {
            // Get all items
            var allItems = await _tableStorageService.GetAllItemsAsync();
            var itemsList = allItems.ToList();
            
            // Get recent sales (last 30 days)
            var fromDate = DateTime.Today.AddDays(-30);
            var toDate = DateTime.Today.AddDays(1);
            var recentSales = await _tableStorageService.GetSalesAsync(fromDate, toDate);
            var recentSalesList = recentSales.ToList();
            
            // Calculate dashboard metrics
            var totalItems = itemsList.Count;
            var lowStockItems = itemsList.Where(i => i.quantity <= 5).ToList();
            var totalRevenue = (decimal)recentSalesList.Sum(s => s.TotalAmount);
            var recentSalesCount = recentSalesList.Count;
            
            var dashboardData = new DashboardViewModel
            {
                TotalItems = totalItems,
                TotalRevenue = totalRevenue,
                LowStockItems = lowStockItems.Count,
                RecentSalesCount = recentSalesCount,
                RecentSales = recentSalesList.OrderByDescending(s => s.SaleDate).Take(10).ToList(),
                LowStockItemsList = lowStockItems.Take(10).ToList()
            };
            
            return View(dashboardData);
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Error loading dashboard data: " + ex.Message;
            return View(new DashboardViewModel());
        }
    }
}
