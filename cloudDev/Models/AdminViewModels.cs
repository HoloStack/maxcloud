using System.ComponentModel.DataAnnotations;

namespace cloudDev.Models;

public class ManageItemsViewModel
{
    public List<ItemsModel> Items { get; set; } = new List<ItemsModel>();
    public string SearchQuery { get; set; } = "";
    public string SelectedCategory { get; set; } = "";
    public int TotalItems { get; set; }
    public int LowStockItems { get; set; }
    public decimal TotalInventoryValue { get; set; }
}

public class EditItemViewModel
{
    public string RowKey { get; set; }
    
    [Required(ErrorMessage = "Name is required")]
    public string Name { get; set; }

    [Required(ErrorMessage = "Description is required")]
    public string Description { get; set; }

    [Required(ErrorMessage = "Price is required")]
    [Range(0.01, (double)decimal.MaxValue, ErrorMessage = "Price must be greater than 0")]
    public decimal Price { get; set; }

    [Required(ErrorMessage = "Quantity is required")]
    [Range(0, int.MaxValue, ErrorMessage = "Quantity must be 0 or greater")]
    public int Quantity { get; set; }

    [Required(ErrorMessage = "Category is required")]
    public string Category { get; set; }

    public string CurrentImage { get; set; }
    public IFormFile? NewImageFile { get; set; }

    // Available categories
    public static List<string> AvailableCategories => new List<string>
    {
        "Electronics",
        "Clothing", 
        "Models"
    };
}

public class SalesReportViewModel
{
public DateTime StartDate { get; set; } = DateTime.Today.AddMonths(-1);
    public DateTime EndDate { get; set; } = DateTime.Today;
    public string Category { get; set; } = "";
    public string Customer { get; set; } = "";
    public List<string> Categories { get; set; } = new List<string>();
    public decimal TotalRevenue { get; set; }
    public string TopCustomer { get; set; } = "";
    public string BestSellingCategory { get; set; } = "";
    public List<SalesModel> SalesTransactions { get; set; } = new List<SalesModel>();
}

public class SalesSummary
{
    public string Name { get; set; }
    public int TotalQuantity { get; set; }
    public float TotalRevenue { get; set; }
    public int TransactionCount { get; set; }
}

public class SalesStatistics
{
    public float TotalRevenue { get; set; }
    public int TotalTransactions { get; set; }
    public int TotalItemsSold { get; set; }
    public float AverageOrderValue { get; set; }
    public string BestSellingCategory { get; set; }
    public string TopCustomer { get; set; }
}

public class UpdateStockViewModel
{
    public string ItemId { get; set; }
    public string ItemName { get; set; }
    public int CurrentStock { get; set; }
    
    [Required(ErrorMessage = "New stock quantity is required")]
    [Range(0, int.MaxValue, ErrorMessage = "Stock must be 0 or greater")]
    public int NewStock { get; set; }
    
    public string Reason { get; set; } = "";
}

public class DashboardViewModel
{
    public int TotalItems { get; set; }
    public decimal TotalRevenue { get; set; }
    public int LowStockItems { get; set; }
    public int RecentSalesCount { get; set; }
    public List<SalesModel> RecentSales { get; set; } = new List<SalesModel>();
    public List<ItemsModel> LowStockItemsList { get; set; } = new List<ItemsModel>();
}
