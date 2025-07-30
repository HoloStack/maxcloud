using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using cloudDev.Models;
using cloudDev.Services;

namespace cloudDev.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly TableStorageService _tableStorageService;

    public HomeController(ILogger<HomeController> logger, TableStorageService tableStorageService)
    {
        _logger = logger;
        _tableStorageService = tableStorageService;
    }

    public async Task<IActionResult> Index(string searchQuery = "", string category = "", string sortBy = "", decimal minPrice = 0, decimal maxPrice = 0)
    {
        var allItems = await _tableStorageService.GetAllItemsAsync();
        // Filter out items with null required fields
        var items = allItems.AsEnumerable().Where(i => !string.IsNullOrEmpty(i.Name) && !string.IsNullOrEmpty(i.Description));

        // Filter by search query
        if (!string.IsNullOrEmpty(searchQuery))
        {
            items = items.Where(i => (!string.IsNullOrEmpty(i.Name) && i.Name.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)) ||
                                   (!string.IsNullOrEmpty(i.Description) && i.Description.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)));
        }

        // Filter by category
        if (!string.IsNullOrEmpty(category))
        {
            items = items.Where(i => !string.IsNullOrEmpty(i.Category) && i.Category == category);
        }

        // Filter by price range
        if (minPrice > 0)
        {
            items = items.Where(i => i.price >= (double)minPrice);
        }
        if (maxPrice > 0)
        {
            items = items.Where(i => i.price <= (double)maxPrice);
        }

        // Sort items
        items = sortBy switch
        {
            "name_asc" => items.OrderBy(i => i.Name ?? ""),
            "name_desc" => items.OrderByDescending(i => i.Name ?? ""),
            "price_asc" => items.OrderBy(i => i.price),
            "price_desc" => items.OrderByDescending(i => i.price),
            _ => items.OrderBy(i => i.Name ?? "")
        };

        // Use predefined categories instead of dynamic ones
        var categories = AddItemViewModel.AvailableCategories;

        var viewModel = new StoreIndexViewModel
        {
            Items = items.ToList(),
            SearchQuery = searchQuery,
            SelectedCategory = category,
            SortBy = sortBy,
            MinPrice = minPrice,
            MaxPrice = maxPrice,
            Categories = categories
        };

        return View(viewModel);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
