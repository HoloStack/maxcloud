namespace cloudDev.Services;

using Azure;
using Azure.Data.Tables;
using cloudDev.Models;

public class TableStorageService
{
    private readonly TableClient _tableClient;

    public TableStorageService(IConfiguration configuration)
    {
        string connectionString = configuration["AzureStorage:ConnectionString"];
        string tableName = "app";
        _tableClient = new TableClient(connectionString, tableName);
        _tableClient.CreateIfNotExists();
    }

    public async Task AddCustomerAsync(CustomerModel customer)
    {
        await _tableClient.AddEntityAsync(customer);
    }

    public async Task<CustomerModel> GetCustomerAsync(string partitionKey, string rowKey)
    {
        return await _tableClient.GetEntityAsync<CustomerModel>(partitionKey, rowKey);
    }

    public async Task<CustomerModel?> GetCustomerByEmailAsync(string email)
    {
        string filter = TableClient.CreateQueryFilter(
            $"PartitionKey eq {"Region1"} and Email eq {email}"
        );

        AsyncPageable<CustomerModel> results = _tableClient.QueryAsync<CustomerModel>(filter);
        await foreach (var result in results)
        {
            return result;
        }

        return null;
    }

    public async Task<bool> CheckPassword(string email, string password)
    {
        string filter = TableClient.CreateQueryFilter(
            $"PartitionKey eq {"Region1"} and Email eq {email} and PasswordHash eq {password}"
        );

        AsyncPageable<CustomerModel> results = _tableClient.QueryAsync<CustomerModel>(filter);
        await foreach (var result in results)
        {
            return true;
        }

        return false;
    }

    public async Task CreateItemAsync(ItemsModel item)
    {
        await _tableClient.AddEntityAsync(item);
        
    }

    public async Task<ItemsModel> GetItemAsync(string partitionKey, string rowKey)
    {
        return await _tableClient.GetEntityAsync<ItemsModel>(partitionKey, rowKey);
    }

    public async Task<IEnumerable<ItemsModel>> GetAllItemsAsync()
    {
        List<ItemsModel> items = new List<ItemsModel>();
        AsyncPageable<ItemsModel> results = _tableClient.QueryAsync<ItemsModel>();

        await foreach (var item in results)
        {
            items.Add(item);
        }

        return items;
    }

    public async Task UpdateItemAsync(ItemsModel item)
    {
        await _tableClient.UpdateEntityAsync(item, item.ETag, TableUpdateMode.Replace);
    }

    public async Task DeleteItemAsync(string partitionKey, string rowKey)
    {
        await _tableClient.DeleteEntityAsync(partitionKey, rowKey);
    }

    public async Task<IEnumerable<ItemsModel>> FilterItemsAsync(string filter)
    {
        {
            List<ItemsModel> items = new List<ItemsModel>();
            AsyncPageable<ItemsModel> results = _tableClient.QueryAsync<ItemsModel>(filter);

            await foreach (var item in results)
            {
                items.Add(item);
            }

            return items;
        }
    }

    // Cart operations
    public async Task AddToCartAsync(CartModel cartItem)
    {
        await _tableClient.AddEntityAsync(cartItem);
    }

    public async Task<IEnumerable<CartModel>> GetCartItemsAsync(string customerEmail)
    {
        string filter = TableClient.CreateQueryFilter(
            $"PartitionKey eq {"Cart"} and CustomerEmail eq {customerEmail}"
        );

        List<CartModel> cartItems = new List<CartModel>();
        AsyncPageable<CartModel> results = _tableClient.QueryAsync<CartModel>(filter);

        await foreach (var item in results)
        {
            cartItems.Add(item);
        }

        return cartItems;
    }

    public async Task UpdateCartItemAsync(CartModel cartItem)
    {
        await _tableClient.UpdateEntityAsync(cartItem, cartItem.ETag, TableUpdateMode.Replace);
    }

    public async Task RemoveFromCartAsync(string partitionKey, string rowKey)
    {
        await _tableClient.DeleteEntityAsync(partitionKey, rowKey);
    }

    public async Task ClearCartAsync(string customerEmail)
    {
        var cartItems = await GetCartItemsAsync(customerEmail);
        foreach (var item in cartItems)
        {
            await RemoveFromCartAsync(item.PartitionKey, item.RowKey);
        }
    }

    // Sales tracking operations
    public async Task RecordSaleAsync(SalesModel sale)
    {
        await _tableClient.AddEntityAsync(sale);
    }

    public async Task<IEnumerable<SalesModel>> GetSalesAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        List<SalesModel> sales = new List<SalesModel>();
        
        string filter = "PartitionKey eq 'Sales'";
        
        // Only add date filters if both dates are provided and are within reasonable range
        if (fromDate.HasValue && toDate.HasValue)
        {
            // Ensure dates are within reasonable range for Azure Table Storage
            var minDate = new DateTime(1601, 1, 1);
            var maxDate = new DateTime(9999, 12, 31);
            
            var safeFromDate = fromDate.Value < minDate ? minDate : fromDate.Value;
            var safeToDate = toDate.Value > maxDate ? maxDate : toDate.Value;
            
            filter += $" and SaleDate ge datetime'{safeFromDate:yyyy-MM-ddTHH:mm:ssZ}' and SaleDate le datetime'{safeToDate:yyyy-MM-ddTHH:mm:ssZ}'";
        }
        
        AsyncPageable<SalesModel> results = _tableClient.QueryAsync<SalesModel>(filter);
        await foreach (var sale in results)
        {
            sales.Add(sale);
        }
        
        return sales.OrderByDescending(s => s.SaleDate);
    }

    public async Task<IEnumerable<SalesModel>> GetSalesByItemAsync(string itemId)
    {
        string filter = TableClient.CreateQueryFilter(
            $"PartitionKey eq {"Sales"} and ItemId eq {itemId}"
        );

        List<SalesModel> sales = new List<SalesModel>();
        AsyncPageable<SalesModel> results = _tableClient.QueryAsync<SalesModel>(filter);

        await foreach (var sale in results)
        {
            sales.Add(sale);
        }

        return sales.OrderByDescending(s => s.SaleDate);
    }

    public async Task<IEnumerable<SalesModel>> GetSalesByCustomerAsync(string customerEmail)
    {
        string filter = TableClient.CreateQueryFilter(
            $"PartitionKey eq {"Sales"} and CustomerEmail eq {customerEmail}"
        );

        List<SalesModel> sales = new List<SalesModel>();
        AsyncPageable<SalesModel> results = _tableClient.QueryAsync<SalesModel>(filter);

        await foreach (var sale in results)
        {
            sales.Add(sale);
        }

        return sales.OrderByDescending(s => s.SaleDate);
    }
    
}
