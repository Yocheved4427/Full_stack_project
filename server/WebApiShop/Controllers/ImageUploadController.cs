using Microsoft.AspNetCore.Mvc;

namespace WebApiShop.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImageUploadController : ControllerBase
    {
        private readonly IWebHostEnvironment _environment;
        private const string ClientImagesPath = @"F:\project\client\public\images";

        public ImageUploadController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        [HttpGet("image")]
        public IActionResult GetImage([FromQuery] string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path))
                    return BadRequest("Image path is required");

                // Remove "images/" prefix if present
                var relativePath = path.StartsWith("images/") ? path.Substring("images/".Length) : path;
                
                var fullPath = Path.Combine(ClientImagesPath, relativePath);
                var normalizedPath = Path.GetFullPath(fullPath);

                // Security check: ensure file is within client images directory
                if (!normalizedPath.StartsWith(ClientImagesPath))
                    return BadRequest("Invalid file path");

                if (!System.IO.File.Exists(normalizedPath))
                    return NotFound();

                var contentType = GetContentType(normalizedPath);
                var fileBytes = System.IO.File.ReadAllBytes(normalizedPath);
                
                return File(fileBytes, contentType);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving image: {ex.Message}");
            }
        }

        private string GetContentType(string path)
        {
            var ext = Path.GetExtension(path).ToLowerInvariant();
            return ext switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                ".bmp" => "image/bmp",
                ".svg" => "image/svg+xml",
                _ => "application/octet-stream"
            };
        }

        [HttpGet("list")]
        public ActionResult<List<string>> GetProductImages([FromQuery] string categoryName, [FromQuery] string productName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(categoryName) || string.IsNullOrWhiteSpace(productName))
                    return BadRequest("Category name and product name are required");

                var productPath = Path.Combine(ClientImagesPath, categoryName, productName);
                
                if (!Directory.Exists(productPath))
                    return Ok(new List<string>());

                var imageFiles = Directory.GetFiles(productPath, "*.*", SearchOption.TopDirectoryOnly)
                    .Where(file => new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" }
                        .Contains(Path.GetExtension(file).ToLower()))
                    .Select(file => $"images/{categoryName}/{productName}/{Path.GetFileName(file)}")
                    .ToList();

                return Ok(imageFiles);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error listing images: {ex.Message}");
            }
        }

        [HttpPost]
        [RequestSizeLimit(104857600)] // 100 MB
        public async Task<ActionResult<string>> UploadImage(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest("No file uploaded");

                // Create images directory if it doesn't exist
                var imagesPath = Path.Combine(_environment.WebRootPath, "images", "products");
                if (!Directory.Exists(imagesPath))
                {
                    Directory.CreateDirectory(imagesPath);
                }

                // Generate unique filename
                var fileExtension = Path.GetExtension(file.FileName);
                var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(imagesPath, uniqueFileName);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Return relative path for database
                var relativePath = $"images/products/{uniqueFileName}";
                return Ok(new { url = relativePath });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error uploading file: {ex.Message}");
            }
        }

        [HttpPost("multiple")]
        [RequestSizeLimit(104857600)] // 100 MB
        public async Task<ActionResult<List<string>>> UploadMultipleImages([FromForm] List<IFormFile> files, [FromForm] string categoryName, [FromForm] string productName)
        {
            try
            {
                if (files == null || files.Count == 0)
                    return BadRequest("No files uploaded");

                if (string.IsNullOrWhiteSpace(categoryName) || string.IsNullOrWhiteSpace(productName))
                    return BadRequest("Category name and product name are required");

                var uploadedUrls = new List<string>();
                var imagesPath = Path.Combine(ClientImagesPath, categoryName, productName);
                
                if (!Directory.Exists(imagesPath))
                {
                    Directory.CreateDirectory(imagesPath);
                }

                foreach (var file in files)
                {
                    if (file.Length > 0)
                    {
                        // Use original filename
                        var fileName = Path.GetFileName(file.FileName);
                        var filePath = Path.Combine(imagesPath, fileName);
                        var relativeUrl = $"images/{categoryName}/{productName}/{fileName}";

                        // If file already exists, use the existing file instead of creating a duplicate
                        if (System.IO.File.Exists(filePath))
                        {
                            // File exists, just return its URL without re-uploading
                            uploadedUrls.Add(relativeUrl);
                        }
                        else
                        {
                            // File doesn't exist, upload it
                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await file.CopyToAsync(stream);
                            }
                            uploadedUrls.Add(relativeUrl);
                        }
                    }
                }

                return Ok(uploadedUrls);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error uploading files: {ex.Message}");
            }
        }

        [HttpDelete]
        public ActionResult DeleteImage([FromQuery] string imagePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(imagePath))
                    return BadRequest("Image path is required");

                // Ensure path starts with "images/"
                if (!imagePath.StartsWith("images/"))
                    return BadRequest("Invalid image path format");

                // Remove "images/" prefix and construct full path
                var relativePath = imagePath.Substring("images/".Length);
                var fullPath = Path.Combine(ClientImagesPath, relativePath);

                // Security check: ensure file is within client images directory
                var normalizedPath = Path.GetFullPath(fullPath);
                if (!normalizedPath.StartsWith(ClientImagesPath))
                    return BadRequest("Invalid file path");

                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                    return Ok(new { message = "Image deleted successfully" });
                }
                else
                {
                    return NotFound(new { message = "Image file not found" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error deleting image: {ex.Message}");
            }
        }
    }
}
