using Azure;
using Azure.Data.Tables;

namespace cloudDev.Models;

public class CartModel : ITableEntity
{
    public string CustomerEmail { get; set; }
    public string ItemId { get; set; }
    public string ItemName { get; set; }
    public double ItemPrice { get; set; }
    public int Quantity { get; set; }
    public string ItemImage { get; set; }
    public string PartitionKey { get; set; }
    public string RowKey { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
}
