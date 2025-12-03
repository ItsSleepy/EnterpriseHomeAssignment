using EnterpriseHomeAssignment.Factories;
using EnterpriseHomeAssignment.Models;
using EnterpriseHomeAssignment.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.IO.Compression;
using System.Text;

namespace EnterpriseHomeAssignment.Pages
{
    public class BulkImportModel : PageModel
    {
        private readonly ImportItemFactory _factory;
        private readonly IItemsRepository _memoryRepository;
        private readonly IItemsRepository _dbRepository;
        private readonly IWebHostEnvironment _environment;

        public BulkImportModel(
            ImportItemFactory factory,
            [FromKeyedServices("memory")] IItemsRepository memoryRepository,
            [FromKeyedServices("database")] IItemsRepository dbRepository,
            IWebHostEnvironment environment)
        {
            _factory = factory;
            _memoryRepository = memoryRepository;
            _dbRepository = dbRepository;
            _environment = environment;
        }

        public List<IItemValidating>? PreviewItems { get; set; }
        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }
        public bool CanDownloadZip { get; set; }

        public async Task OnGetAsync()
        {
            // Load preview items from memory if they exist
            var items = await _memoryRepository.GetAllAsync();
            if (items.Any())
            {
                PreviewItems = items;
                CanDownloadZip = true;
            }
        }

        public async Task<IActionResult> OnPostUploadAsync(IFormFile jsonFile)
        {
            if (jsonFile == null || jsonFile.Length == 0)
            {
                ErrorMessage = "Please select a JSON file to upload.";
                return Page();
            }

            if (!jsonFile.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                ErrorMessage = "Please upload a valid JSON file.";
                return Page();
            }

            try
            {
                // Read the JSON file
                string jsonContent;
                using (var reader = new StreamReader(jsonFile.OpenReadStream(), Encoding.UTF8))
                {
                    jsonContent = await reader.ReadToEndAsync();
                }

                // Parse JSON using Factory pattern
                var items = _factory.Create(jsonContent);

                if (!items.Any())
                {
                    ErrorMessage = "No valid items found in the JSON file.";
                    return Page();
                }

                // Store in memory repository for preview
                await _memoryRepository.SaveAsync(items);

                // Load preview
                PreviewItems = items;
                CanDownloadZip = true;
                SuccessMessage = $"Successfully parsed {items.Count} items. Download the ZIP template, add images, and upload it back.";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error parsing JSON: {ex.Message}";
            }

            return Page();
        }

        public async Task<IActionResult> OnGetDownloadZipAsync()
        {
            try
            {
                var items = await _memoryRepository.GetAllAsync();
                if (!items.Any())
                {
                    return NotFound("No items to generate ZIP for. Please upload JSON first.");
                }

                // Create ZIP in memory
                using var memoryStream = new MemoryStream();
                using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                {
                    foreach (var item in items)
                    {
                        string folderName = "";
                        if (item is Restaurant restaurant)
                        {
                            folderName = $"restaurant-{restaurant.Id}";
                        }
                        else if (item is MenuItem menuItem)
                        {
                            folderName = $"menuitem-{menuItem.Id}";
                        }

                        // Create folder with default.jpg
                        var entry = archive.CreateEntry($"{folderName}/default.jpg");
                        using var entryStream = entry.Open();
                        // Create a minimal placeholder (empty file for now)
                        // Users will replace this with actual images
                    }
                }

                memoryStream.Position = 0;
                return File(memoryStream.ToArray(), "application/zip", "item-folders.zip");
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error generating ZIP: {ex.Message}";
                PreviewItems = await _memoryRepository.GetAllAsync();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostUploadZipAsync(IFormFile zipFile)
        {
            if (zipFile == null || zipFile.Length == 0)
            {
                ErrorMessage = "Please select a ZIP file to upload.";
                PreviewItems = await _memoryRepository.GetAllAsync();
                CanDownloadZip = true;
                return Page();
            }

            if (!zipFile.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                ErrorMessage = "Please upload a valid ZIP file.";
                PreviewItems = await _memoryRepository.GetAllAsync();
                CanDownloadZip = true;
                return Page();
            }

            try
            {
                var items = await _memoryRepository.GetAllAsync();
                if (!items.Any())
                {
                    ErrorMessage = "No items in memory. Please upload JSON first.";
                    return Page();
                }

                // Extract ZIP to temporary location
                var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                Directory.CreateDirectory(tempPath);

                using (var stream = zipFile.OpenReadStream())
                {
                    using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
                    archive.ExtractToDirectory(tempPath);
                }

                // Process images and save to wwwroot
                var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads");
                Directory.CreateDirectory(uploadsPath);

                foreach (var item in items)
                {
                    string folderName = "";
                    string itemId = "";
                    
                    if (item is Restaurant restaurant)
                    {
                        folderName = $"restaurant-{restaurant.Id}";
                        itemId = restaurant.Id.ToString();
                    }
                    else if (item is MenuItem menuItem)
                    {
                        folderName = $"menuitem-{menuItem.Id}";
                        itemId = menuItem.Id.ToString();
                    }

                    var sourceFolderPath = Path.Combine(tempPath, folderName);
                    if (Directory.Exists(sourceFolderPath))
                    {
                        var imageFiles = Directory.GetFiles(sourceFolderPath, "*.*")
                            .Where(f => f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                                       f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                                       f.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                                       f.EndsWith(".gif", StringComparison.OrdinalIgnoreCase));

                        if (imageFiles.Any())
                        {
                            // Take the first image found
                            var sourceImage = imageFiles.First();
                            var extension = Path.GetExtension(sourceImage);
                            var uniqueFileName = $"{Guid.NewGuid()}{extension}";
                            var destPath = Path.Combine(uploadsPath, uniqueFileName);

                            System.IO.File.Copy(sourceImage, destPath, true);

                            // Update item with image path
                            if (item is Restaurant rest)
                            {
                                rest.ImagePath = $"/uploads/{uniqueFileName}";
                            }
                            else if (item is MenuItem menu)
                            {
                                menu.ImagePath = $"/uploads/{uniqueFileName}";
                            }
                        }
                    }
                }

                // Update items in memory
                await _memoryRepository.SaveAsync(items);

                // Clean up temp directory
                Directory.Delete(tempPath, true);

                PreviewItems = items;
                CanDownloadZip = true;
                SuccessMessage = "Images uploaded successfully! Review and click 'Commit to Database' to save.";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error processing ZIP file: {ex.Message}";
                PreviewItems = await _memoryRepository.GetAllAsync();
                CanDownloadZip = true;
            }

            return Page();
        }

        public async Task<IActionResult> OnPostCommitAsync()
        {
            try
            {
                // Get items from memory
                var items = await _memoryRepository.GetAllAsync();

                if (!items.Any())
                {
                    ErrorMessage = "No items to commit. Please upload a JSON file first.";
                    return Page();
                }

                // Save to database
                await _dbRepository.SaveAsync(items);

                // Clear memory after successful commit
                await _memoryRepository.SaveAsync(new List<IItemValidating>());

                SuccessMessage = $"Successfully committed {items.Count} items to the database!";
                PreviewItems = null;

                return RedirectToPage("/Catalog");
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error committing to database: {ex.Message}";
                
                // Reload preview items
                PreviewItems = await _memoryRepository.GetAllAsync();
                CanDownloadZip = true;
                
                return Page();
            }
        }
    }
}
