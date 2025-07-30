using Azure;
using Azure.Data.Tables;

namespace cloudDev.Models;

public class SalesModel : ITableEntity
{
    public string CustomerEmail { get; set; }
    public string CustomerName { get; set; }
    public string ItemId { get; set; }
    public string ItemName { get; set; }
    public string ItemCategory { get; set; }
    public double ItemPrice { get; set; }
    public int QuantitySold { get; set; }
    public double TotalAmount { get; set; }
    public DateTime SaleDate { get; set; }
    public string PartitionKey { get; set; }
    public string RowKey { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
}
