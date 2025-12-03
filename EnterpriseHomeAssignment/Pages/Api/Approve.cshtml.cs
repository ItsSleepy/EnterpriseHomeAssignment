using EnterpriseHomeAssignment.Data;
using EnterpriseHomeAssignment.Models;
using EnterpriseHomeAssignment.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseHomeAssignment.Pages.Api
{
    [Authorize]
    public class ApproveModel : PageModel
    {
        private readonly IItemsRepository _dbRepository;
        private readonly ApplicationDbContext _context;

        public ApproveModel(
            [FromKeyedServices("database")] IItemsRepository dbRepository,
            ApplicationDbContext context)
        {
            _dbRepository = dbRepository;
            _context = context;
        }

        public async Task<IActionResult> OnPostAsync([FromBody] ApproveRequest request)
        {
            var userEmail = User.Identity?.Name;

            if (string.IsNullOrEmpty(userEmail))
            {
                return Forbid();
            }

            if (request.ItemIds == null || !request.ItemIds.Any())
            {
                return BadRequest("No item IDs provided");
            }

            // Check authorization using GetValidators()
            var restaurants = await _context.Restaurants
                .Where(r => request.ItemIds.Contains(r.Id))
                .ToListAsync();

            foreach (var restaurant in restaurants)
            {
                var validators = restaurant.GetValidatorEmails();
                if (!validators.Contains(userEmail))
                {
                    return StatusCode(403, "User is not authorized to approve this item");
                }
            }

            // Approve items using repository
            await _dbRepository.ApproveAsync(request.ItemIds);

            return new JsonResult(new { success = true, message = "Items approved successfully" });
        }

        public class ApproveRequest
        {
            public List<int> ItemIds { get; set; } = new();
        }
    }
}
