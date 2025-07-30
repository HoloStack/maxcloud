using Azure;
using Azure.Data.Tables;

namespace cloudDev.Models;

public class CustomerModel: ITableEntity
{
    public string PartitionKey { get; set; }
    public string RowKey { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }
    public bool IsAdmin { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
}
