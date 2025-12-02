using EnterpriseHomeAssignment.Factories;
using EnterpriseHomeAssignment.Models;
using EnterpriseHomeAssignment.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text;

namespace EnterpriseHomeAssignment.Pages
{
    public class BulkImportModel : PageModel
    {
        private readonly ImportItemFactory _factory;
        private readonly IItemsRepository _memoryRepository;
        private readonly IItemsRepository _dbRepository;

        public BulkImportModel(
            ImportItemFactory factory,
            [FromKeyedServices("memory")] IItemsRepository memoryRepository,
            [FromKeyedServices("database")] IItemsRepository dbRepository)
        {
            _factory = factory;
            _memoryRepository = memoryRepository;
            _dbRepository = dbRepository;
        }

        public List<IItemValidating>? PreviewItems { get; set; }
        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        public async Task OnGetAsync()
        {
            // Load preview items from memory if they exist
            var items = await _memoryRepository.GetAllAsync();
            if (items.Any())
            {
                PreviewItems = items;
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
                SuccessMessage = $"Successfully parsed {items.Count} items. Review the data and click 'Commit to Database' to save.";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error parsing JSON: {ex.Message}";
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
                
                return Page();
            }
        }
    }
}
