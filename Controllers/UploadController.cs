using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace VideoChatingApp.WebRTC.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class UploadController : ControllerBase
{
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
    private const long MaxFileSize = 10 * 1024 * 1024; // 10 MB
    private const int MaxFileCount = 5;

    private readonly IWebHostEnvironment _env;
    private readonly ILogger<UploadController> _logger;

    public UploadController(IWebHostEnvironment env, ILogger<UploadController> logger)
    {
        _env = env;
        _logger = logger;
    }

    [HttpPost("image")]
    public async Task<IActionResult> UploadImage(List<IFormFile> files)
    {
        if (files == null || files.Count == 0)
            return BadRequest(new { message = "No files provided" });

        if (files.Count > MaxFileCount)
            return BadRequest(new { message = $"Maximum {MaxFileCount} files allowed" });

        var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "images");
        Directory.CreateDirectory(uploadsDir);

        var results = new List<object>();

        foreach (var file in files)
        {
            if (file.Length == 0 || file.Length > MaxFileSize)
                return BadRequest(new { message = $"File '{file.FileName}' exceeds 10 MB limit" });

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext))
                return BadRequest(new { message = $"File type '{ext}' is not allowed. Use: {string.Join(", ", AllowedExtensions)}" });

            var fileName = $"{Guid.NewGuid():N}{ext}";
            var filePath = Path.Combine(uploadsDir, fileName);

            await using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);

            results.Add(new
            {
                fileName,
                originalName = file.FileName,
                url = $"/uploads/images/{fileName}",
                size = file.Length
            });

            _logger.LogInformation("Uploaded image {FileName} ({Size} bytes)", fileName, file.Length);
        }

        return Ok(new { files = results });
    }

    [HttpPost("attachment")]
    public async Task<IActionResult> UploadAttachment(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file provided" });

        if (file.Length > MaxFileSize)
            return BadRequest(new { message = "File exceeds 10 MB limit" });

        var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "attachments");
        Directory.CreateDirectory(uploadsDir);

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var fileName = $"{Guid.NewGuid():N}{ext}";
        var filePath = Path.Combine(uploadsDir, fileName);

        await using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream);

        _logger.LogInformation("Uploaded attachment {FileName} ({Size} bytes)", fileName, file.Length);

        return Ok(new
        {
            fileName,
            originalName = file.FileName,
            url = $"/uploads/attachments/{fileName}",
            size = file.Length
        });
    }

    [HttpGet("exists/{**filePath}")]
    public IActionResult FileExists(string filePath)
    {
        var fullPath = Path.Combine(_env.WebRootPath, "uploads", filePath);
        return System.IO.File.Exists(fullPath) ? Ok() : NotFound();
    }
}
