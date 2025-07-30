
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace cloudDev.Services;
public class BlobStorageService
{
    private readonly BlobContainerClient _containerClient;
    private readonly Dictionary<string, string> _mimeTypes;

    public BlobStorageService(IConfiguration configuration)
    {
        var connectionString = configuration["AzureStorage:ConnectionString"];
        var containerName = configuration["AzureBlobSettings:ContainerName"];

        _containerClient = new BlobContainerClient(connectionString, containerName);
        _containerClient.CreateIfNotExists(PublicAccessType.None);
        
        // Initialize supported multimedia MIME types
        _mimeTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Image formats
            {".jpg", "image/jpeg"},
            {".jpeg", "image/jpeg"},
            {".png", "image/png"},
            {".gif", "image/gif"},
            {".bmp", "image/bmp"},
            {".webp", "image/webp"},
            {".svg", "image/svg+xml"},
            {".tiff", "image/tiff"},
            {".ico", "image/x-icon"},
            
            // Video formats
            {".mp4", "video/mp4"},
            {".avi", "video/x-msvideo"},
            {".mov", "video/quicktime"},
            {".wmv", "video/x-ms-wmv"},
            {".flv", "video/x-flv"},
            {".webm", "video/webm"},
            {".mkv", "video/x-matroska"},
            {".m4v", "video/x-m4v"},
            {".3gp", "video/3gpp"},
            
            // Audio formats
            {".mp3", "audio/mpeg"},
            {".wav", "audio/wav"},
            {".flac", "audio/flac"},
            {".aac", "audio/aac"},
            {".ogg", "audio/ogg"},
            {".wma", "audio/x-ms-wma"},
            {".m4a", "audio/mp4"},
            
            // Document formats (for completeness)
            {".pdf", "application/pdf"},
            {".txt", "text/plain"},
            {".doc", "application/msword"},
            {".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document"}
        };
    }

    /// Uploads a multimedia file to Azure Blob Storage return url of upladed file
    public async Task<string> UploadMultimediaFileAsync(IFormFile file, string? description = null)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("File is empty or null.");

        // Validate file size (50MB limit for multimedia files)
        const long maxFileSize = 50 * 1024 * 1024; // 50MB
        if (file.Length > maxFileSize)
            throw new ArgumentException($"File size exceeds the maximum limit of {maxFileSize / (1024 * 1024)}MB.");

        var fileExtension = Path.GetExtension(file.FileName);
        if (string.IsNullOrEmpty(fileExtension))
            throw new ArgumentException("File must have a valid extension.");

        // Determine content type
        var contentType = GetContentType(fileExtension, file.ContentType);
        
        // Generate unique blob name with timestamp for better organization
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        var blobName = $"{timestamp}-{Guid.NewGuid()}{fileExtension}";
        var blobClient = _containerClient.GetBlobClient(blobName);

        // Set blob properties for multimedia files
        var blobHttpHeaders = new BlobHttpHeaders
        {
            ContentType = contentType,
            CacheControl = "public, max-age=31536000", // 1 year cache for multimedia
        };

        // Add metadata including description/title
        var metadata = new Dictionary<string, string>
        {
            {"OriginalFileName", file.FileName},
            {"FileSize", file.Length.ToString()},
            {"UploadDate", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")},
            {"FileType", GetFileCategory(fileExtension)}
        };
        
        // Add description/title if provided, otherwise use filename without extension as fallback
        if (!string.IsNullOrWhiteSpace(description))
        {
            metadata["Description"] = description.Trim();
        }
        else
        {
            // Use filename without extension as a fallback description
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file.FileName);
            metadata["Description"] = fileNameWithoutExtension;
        }

        using (var stream = file.OpenReadStream())
        {
            await blobClient.UploadAsync(stream, new BlobUploadOptions
            {
                HttpHeaders = blobHttpHeaders,
                Metadata = metadata,
                ProgressHandler = new Progress<long>(bytesTransferred => 
                {
                    // Optional: Log progress for large files
                    var progressPercentage = (double)bytesTransferred / file.Length * 100;
                    if (progressPercentage % 10 == 0) // Log every 10%
                    {
                        Console.WriteLine($"Upload progress: {progressPercentage:F1}%");
                    }
                })
            });
        }

        return blobClient.Uri.ToString();
    }

    /// Legacy method for backward compatibility - now uses the enhanced multimedia upload
    public async Task<string> UploadImageAsync(IFormFile file)
    {
        return await UploadMultimediaFileAsync(file);
    }

    /// Deletes a multimedia file from Azure Blob Storage
    public async Task DeleteMultimediaFileAsync(string blobUrl)
    {
        if (string.IsNullOrEmpty(blobUrl))
            return;

        try
        {
            var blobName = Path.GetFileName(new Uri(blobUrl).LocalPath);
            var blobClient = _containerClient.GetBlobClient(blobName);
            
            // Check if blob exists before attempting to delete
            var exists = await blobClient.ExistsAsync();
            if (exists.Value)
            {
                await blobClient.DeleteAsync();
            }
        }
        catch (UriFormatException)
        {
            throw new ArgumentException("Invalid blob URL format.");
        }
    }

    /// Legacy method for backward compatibility
    public async Task DeleteImageAsync(string blobUrl)
    {
        await DeleteMultimediaFileAsync(blobUrl);
    }

    /// Gets multimedia file information
    public async Task<MultimediaFileInfo> GetFileInfoAsync(string blobUrl)
    {
        if (string.IsNullOrEmpty(blobUrl))
            throw new ArgumentException("Blob URL cannot be null or empty.");

        try
        {
            var blobName = Path.GetFileName(new Uri(blobUrl).LocalPath);
            var blobClient = _containerClient.GetBlobClient(blobName);
            
            var properties = await blobClient.GetPropertiesAsync();
            var metadata = properties.Value.Metadata;

            return new MultimediaFileInfo
            {
                Url = blobUrl,
                FileName = metadata.ContainsKey("OriginalFileName") ? metadata["OriginalFileName"] : blobName,
                ContentType = properties.Value.ContentType,
                FileSize = properties.Value.ContentLength,
                LastModified = properties.Value.LastModified,
                FileType = metadata.ContainsKey("FileType") ? metadata["FileType"] : "Unknown",
                UploadDate = metadata.ContainsKey("UploadDate") ? DateTime.Parse(metadata["UploadDate"]) : properties.Value.CreatedOn.DateTime
            };
        }
        catch (UriFormatException)
        {
            throw new ArgumentException("Invalid blob URL format.");
        }
    }

    /// Lists all multimedia files in the container
    public async Task<IEnumerable<MultimediaFileInfo>> ListFilesAsync(string? prefix = null)
    {
        var files = new List<MultimediaFileInfo>();
        
        await foreach (var blobItem in _containerClient.GetBlobsAsync(BlobTraits.Metadata, prefix: prefix))
        {
            var blobClient = _containerClient.GetBlobClient(blobItem.Name);
            var fileInfo = new MultimediaFileInfo
            {
                Url = blobClient.Uri.ToString(),
                FileName = blobItem.Metadata?.ContainsKey("OriginalFileName") == true ? 
                          blobItem.Metadata["OriginalFileName"] : blobItem.Name,
                Description = blobItem.Metadata?.ContainsKey("Description") == true ? 
                             blobItem.Metadata["Description"] : string.Empty,
                ContentType = blobItem.Properties.ContentType,
                FileSize = blobItem.Properties.ContentLength ?? 0,
                LastModified = blobItem.Properties.LastModified,
                FileType = blobItem.Metadata?.ContainsKey("FileType") == true ? 
                          blobItem.Metadata["FileType"] : GetFileCategory(Path.GetExtension(blobItem.Name))
            };
            
            files.Add(fileInfo);
        }

        return files;
    }

    /// Determines the appropriate content type for a file
    private string GetContentType(string fileExtension, string providedContentType)
    {
        // First try to use the extension-based MIME type
        if (_mimeTypes.TryGetValue(fileExtension, out var mimeType))
        {
            return mimeType;
        }

        // Fall back to provided content type if available
        if (!string.IsNullOrEmpty(providedContentType))
        {
            return providedContentType;
        }

        // Default to application/octet-stream for unknown types
        return "application/octet-stream";
    }

    /// Downloads a blob as a stream with content information
    public async Task<(Stream stream, string contentType, string fileName)> DownloadBlobAsync(string blobUrl)
    {
        if (string.IsNullOrEmpty(blobUrl))
            throw new ArgumentException("Blob URL cannot be null or empty.");

        try
        {
            var blobName = Path.GetFileName(new Uri(blobUrl).LocalPath);
            var blobClient = _containerClient.GetBlobClient(blobName);
            
            var response = await blobClient.DownloadStreamingAsync();
            var properties = await blobClient.GetPropertiesAsync();
            
            var fileName = properties.Value.Metadata.ContainsKey("OriginalFileName") 
                ? properties.Value.Metadata["OriginalFileName"] 
                : blobName;
            
            return (response.Value.Content, response.Value.Details.ContentType, fileName);
        }
        catch (UriFormatException)
        {
            throw new ArgumentException("Invalid blob URL format.");
        }
    }

    /// Categorizes file type based on extension
    private string GetFileCategory(string fileExtension)
    {
        if (string.IsNullOrEmpty(fileExtension))
            return "Unknown";

        var ext = fileExtension.ToLowerInvariant();
        
        if (new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".svg", ".tiff", ".ico" }.Contains(ext))
            return "Image";
            
        if (new[] { ".mp4", ".avi", ".mov", ".wmv", ".flv", ".webm", ".mkv", ".m4v", ".3gp" }.Contains(ext))
            return "Video";
            
        if (new[] { ".mp3", ".wav", ".flac", ".aac", ".ogg", ".wma", ".m4a" }.Contains(ext))
            return "Audio";
            
        if (new[] { ".pdf", ".txt", ".doc", ".docx" }.Contains(ext))
            return "Document";

        return "Other";
    }
}

/// Information about a multimedia file stored in blob storage
public class MultimediaFileInfo
{
    public string Url { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTimeOffset? LastModified { get; set; }
    public string FileType { get; set; } = string.Empty;
    public DateTime UploadDate { get; set; }
    
    /// Gets the display name - prefers description over filename
    public string DisplayName => !string.IsNullOrWhiteSpace(Description) ? Description : 
                                (!string.IsNullOrWhiteSpace(FileName) ? Path.GetFileNameWithoutExtension(FileName) : "Unnamed File");
}
