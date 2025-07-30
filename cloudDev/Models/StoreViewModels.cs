using System.ComponentModel.DataAnnotations;
using cloudDev.Services;

namespace cloudDev.Models;

public class StoreIndexViewModel
{
    public IEnumerable<ItemsModel> Items { get; set; } = new List<ItemsModel>();
    public string SearchQuery { get; set; } = "";
    public string SelectedCategory { get; set; } = "";
    public string SortBy { get; set; } = "";
    public decimal MinPrice { get; set; }
    public decimal MaxPrice { get; set; }
    public List<string> Categories { get; set; } = new List<string>();
}

public class AddItemViewModel
{
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

    public IFormFile ImageFile { get; set; }

    // Available categories
    public static List<string> AvailableCategories => new List<string>
    {
        "Electronics",
        "Clothing", 
        "Models"
    };
}

public class CartItemViewModel
{
    public string ItemId { get; set; }
    public string ItemName { get; set; }
    public decimal ItemPrice { get; set; }
    public int Quantity { get; set; }
    public string ItemImage { get; set; }
    public decimal Total => ItemPrice * Quantity;
}

public class CartViewModel
{
    public List<CartItemViewModel> Items { get; set; } = new List<CartItemViewModel>();
    public decimal GrandTotal => Items.Sum(i => i.Total);
}

public class MediaBrowserViewModel
{
    public List<MultimediaFileInfo> Images { get; set; } = new List<MultimediaFileInfo>();
    public List<MultimediaFileInfo> Videos { get; set; } = new List<MultimediaFileInfo>();
    public List<MultimediaFileInfo> Audio { get; set; } = new List<MultimediaFileInfo>();
    public string CurrentCategory { get; set; } = "All";
}

public class UploadMediaViewModel
{
    [Required(ErrorMessage = "Please select a file")]
    public IFormFile MediaFile { get; set; }
    
    public string Description { get; set; }
}
