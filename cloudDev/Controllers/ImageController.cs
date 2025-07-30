using Microsoft.AspNetCore.Mvc;
using cloudDev.Services;

namespace cloudDev.Controllers;

public class ImageController : Controller
{
    private readonly BlobStorageService _blobStorageService;

    public ImageController(BlobStorageService blobStorageService)
    {
        _blobStorageService = blobStorageService;
    }

    /// <summary>
    /// Serves images and media files from Azure Blob Storage
    /// </summary>
    /// <param name="url">The blob URL to serve</param>
    /// <returns>The file content</returns>
    [HttpGet]
    public async Task<IActionResult> Serve(string url)
    {
        Console.WriteLine($"ImageController.Serve called with URL: {url}");
        
        if (string.IsNullOrEmpty(url))
        {
            Console.WriteLine("URL is null or empty");
            return NotFound();
        }

        try
        {
            Console.WriteLine($"Attempting to download blob from: {url}");
            var (stream, contentType, fileName) = await _blobStorageService.DownloadBlobAsync(url);
            
            Console.WriteLine($"Successfully retrieved blob - ContentType: {contentType}, FileName: {fileName}");
            
            // Set cache headers for better performance
            Response.Headers["Cache-Control"] = "public, max-age=31536000"; // 1 year
            Response.Headers["ETag"] = $"\"{url.GetHashCode()}\"";
            
            // For video files, ensure proper headers for video playback
            if (contentType.StartsWith("video/"))
            {
                Console.WriteLine("Setting video-specific headers");
                Response.Headers["Accept-Ranges"] = "bytes";
                Response.Headers["Content-Disposition"] = $"inline; filename=\"{fileName}\"";
            }
            
            Console.WriteLine($"Returning file with contentType: {contentType}");
            return File(stream, contentType, enableRangeProcessing: true);
        }
        catch (Exception ex)
        {
            // Log the error with full details
            Console.WriteLine($"ERROR serving file {url}: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return NotFound();
        }
    }

    /// <summary>
    /// Downloads a media file
    /// </summary>
    /// <param name="url">The blob URL to download</param>
    /// <returns>The file as a download</returns>
    [HttpGet]
    public async Task<IActionResult> Download(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return NotFound();
        }

        try
        {
            var (stream, contentType, fileName) = await _blobStorageService.DownloadBlobAsync(url);
            
            return File(stream, contentType, fileName);
        }
        catch (Exception ex)
        {
            // Log the error if needed
            Console.WriteLine($"Error downloading file {url}: {ex.Message}");
            return NotFound();
        }
    }
}
