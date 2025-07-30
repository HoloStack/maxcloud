using Azure;
using Azure.Data.Tables;

namespace cloudDev.Models;

public class ItemsModel : ITableEntity
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string Image { get; set; }
    public int quantity { get; set; }
    public double price { get; set; }
    public string Category { get; set; }
    public string PartitionKey { get; set; }
    public string RowKey { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
}
