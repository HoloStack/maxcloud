using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using cloudDev.Models;
using cloudDev.Services;

namespace cloudDev.Controllers;

public class MediaController : Controller
{
    private readonly BlobStorageService _blobStorageService;

    public MediaController(BlobStorageService blobStorageService)
    {
        _blobStorageService = blobStorageService;
    }

    public async Task<IActionResult> Browse(string category = "All")
    {
        try
        {
            var allFiles = await _blobStorageService.ListFilesAsync();
            
            var viewModel = new MediaBrowserViewModel
            {
                CurrentCategory = category
            };

            foreach (var file in allFiles)
            {
                switch (file.FileType?.ToLower())
                {
                    case "image":
                        viewModel.Images.Add(file);
                        break;
                    case "video":
                        viewModel.Videos.Add(file);
                        break;
                    case "audio":
                        viewModel.Audio.Add(file);
                        break;
                }
            }

            return View(viewModel);
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Error loading media files: " + ex.Message;
            return View(new MediaBrowserViewModel());
        }
    }

    [Authorize]
    public IActionResult Upload()
    {
        return View();
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Upload(UploadMediaViewModel model)
    {
        if (!ModelState.IsValid)
        {
            // For AJAX requests, return JSON error
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return Json(new { success = false, message = string.Join(", ", errors) });
            }
            return View(model);
        }

        try
        {
            var fileUrl = await _blobStorageService.UploadMultimediaFileAsync(model.MediaFile, model.Description);
            var displayName = !string.IsNullOrWhiteSpace(model.Description) ? model.Description : Path.GetFileNameWithoutExtension(model.MediaFile.FileName);
            
            // For AJAX requests, return JSON success
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, message = $"'{displayName}' uploaded successfully!" });
            }
            
            TempData["Success"] = $"'{displayName}' uploaded successfully!";
            return RedirectToAction("Browse");
        }
        catch (Exception ex)
        {
            // For AJAX requests, return JSON error
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = "Error uploading file: " + ex.Message });
            }
            
            ModelState.AddModelError("", "Error uploading file: " + ex.Message);
            return View(model);
        }
    }

    public async Task<IActionResult> Download(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return BadRequest("URL is required");
        }

        try
        {
            // Get file info
            var fileInfo = await _blobStorageService.GetFileInfoAsync(url);
            
            // Create a redirect to the blob URL (since Azure Blob Storage handles the download)
            return Redirect(url);
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Error downloading file: " + ex.Message;
            return RedirectToAction("Browse");
        }
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Delete(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return Json(new { success = false, message = "URL is required" });
        }

        try
        {
            await _blobStorageService.DeleteMultimediaFileAsync(url);
            return Json(new { success = true, message = "File deleted successfully" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Error deleting file: " + ex.Message });
        }
    }

    public async Task<IActionResult> GetFileInfo(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return BadRequest("URL is required");
        }

        try
        {
            var fileInfo = await _blobStorageService.GetFileInfoAsync(url);
            return Json(fileInfo);
        }
        catch (Exception ex)
        {
            return Json(new { error = ex.Message });
        }
    }
}
