using cloudDev.Models;
using System.Security.Cryptography;
using System.Text;

namespace cloudDev.Services;

public class DatabaseSeeder
{
    private readonly TableStorageService _tableStorageService;

    public DatabaseSeeder(TableStorageService tableStorageService)
    {
        _tableStorageService = tableStorageService;
    }

    public async Task SeedDatabaseAsync()
    {
        await SeedAdminUserAsync();
        await SeedSampleProductsAsync();
    }

    private async Task SeedAdminUserAsync()
    {
        try
        {
            // Check if admin user already exists
            var existingAdmin = await _tableStorageService.GetCustomerByEmailAsync("admin@admin.admin");
            if (existingAdmin != null)
            {
                Console.WriteLine("Admin user already exists.");
                return;
            }

            // Create admin user
            var adminUser = new CustomerModel
            {
                PartitionKey = "Region1",
                RowKey = Guid.NewGuid().ToString(),
                Name = "Administrator",
                Email = "admin@admin.admin",
                PasswordHash = HashPassword("admin"),
                IsAdmin = true
            };

            await _tableStorageService.AddCustomerAsync(adminUser);
            Console.WriteLine("✅ Admin user created successfully!");
            Console.WriteLine("   Email: admin@admin.admin");
            Console.WriteLine("   Password: admin");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error creating admin user: {ex.Message}");
        }
    }

    private async Task SeedSampleProductsAsync()
    {
        try
        {
            // Check if products already exist
            var existingProducts = await _tableStorageService.GetAllItemsAsync();
            if (existingProducts.Any())
            {
                Console.WriteLine("Sample products already exist.");
                return;
            }

            // Create sample products
            var sampleProducts = new List<ItemsModel>
            {
                new ItemsModel
                {
                    PartitionKey = "Items",
                    RowKey = Guid.NewGuid().ToString(),
                    Name = "MacBook Pro 14-inch",
                    Description = "Apple MacBook Pro with M3 chip, 14-inch Liquid Retina XDR display, 16GB RAM, 512GB SSD storage.",
                    Category = "Electronics",
                price = 1999.99,
                    quantity = 10,
                    Image = "https://i.imgur.com/gYf2sCh.png"
                },
                new ItemsModel
                {
                    PartitionKey = "Items",
                    RowKey = Guid.NewGuid().ToString(),
                    Name = "Wireless Bluetooth Headphones",
                    Description = "Premium noise-cancelling wireless headphones with 30-hour battery life and superior sound quality.",
                    Category = "Electronics",
                price = 899.99,
                    quantity = 25,
                    Image = "https://i.imgur.com/p5L6a8j.png"
                },
                new ItemsModel
                {
                    PartitionKey = "Items",
                    RowKey = Guid.NewGuid().ToString(),
                    Name = "Programming T-Shirt",
                    Description = "Comfortable cotton t-shirt with 'Hello World' design. Perfect for developers and tech enthusiasts.",
                    Category = "Clothing",
                price = 29.99,
                    quantity = 50,
                    Image = "https://i.imgur.com/mpb2N2t.png"
                },
                new ItemsModel
                {
                    PartitionKey = "Items",
                    RowKey = Guid.NewGuid().ToString(),
                    Name = "Clean Code Book",
                    Description = "A Handbook of Agile Software Craftsmanship by Robert C. Martin. Essential reading for developers.",
                    Category = "Books",
                price = 59.99,
                    quantity = 15,
                    Image = "https://i.imgur.com/sWqXh5g.png"
                },
                new ItemsModel
                {
                    PartitionKey = "Items",
                    RowKey = Guid.NewGuid().ToString(),
                    Name = "Mechanical Keyboard",
                    Description = "RGB backlit mechanical gaming keyboard with Cherry MX switches and programmable keys.",
                    Category = "Electronics",
                price = 149.99,
                    quantity = 20,
                    Image = "https://i.imgur.com/sWqXh5g.png"
                },
                new ItemsModel
                {
                    PartitionKey = "Items",
                    RowKey = Guid.NewGuid().ToString(),
                    Name = "Designer Coffee Mug",
                    Description = "Premium ceramic coffee mug with unique geometric design. Microwave and dishwasher safe.",
                    Category = "Home & Garden",
                    price = 19.99,
                    quantity = 100,
                    Image = "https://i.imgur.com/K5A3A6h.png"
                }
            };

            foreach (var product in sampleProducts)
            {
                await _tableStorageService.CreateItemAsync(product);
            }

            Console.WriteLine($"✅ Created {sampleProducts.Count} sample products!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error creating sample products: {ex.Message}");
        }
    }

    private string HashPassword(string password)
    {
        using (var sha256 = SHA256.Create())
        {
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }
    }
}
