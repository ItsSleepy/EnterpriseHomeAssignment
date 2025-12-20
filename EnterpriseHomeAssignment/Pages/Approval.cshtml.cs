using EnterpriseHomeAssignment.Data;
using EnterpriseHomeAssignment.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseHomeAssignment.Pages
{
    [Authorize]
    public class ApprovalModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ApprovalModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Restaurant> PendingRestaurants { get; set; } = new();
        public List<MenuItem> PendingMenuItems { get; set; } = new();

        [BindProperty]
        public List<int> SelectedRestaurantIds { get; set; } = new();

        [BindProperty]
        public List<Guid> SelectedMenuItemIds { get; set; } = new();

        public async Task OnGetAsync()
        {
            var userEmail = User.Identity?.Name;

            // Site admin can approve both restaurants and all menu items
            if (userEmail == "siteadmin@example.com")
            {
                PendingRestaurants = await _context.Restaurants
                    .Where(r => r.Status == "Pending")
                    .ToListAsync();
                    
                PendingMenuItems = await _context.MenuItems
                    .Include(m => m.Restaurant)
                    .Where(m => m.Status == "Pending")
                    .ToListAsync();
            }
            else
            {
                // Restaurant owners can approve menu items for their restaurants only
                var ownedRestaurants = await _context.Restaurants
                    .Where(r => r.OwnerEmailAddress == userEmail)
                    .Select(r => r.Id)
                    .ToListAsync();

                PendingMenuItems = await _context.MenuItems
                    .Include(m => m.Restaurant)
                    .Where(m => m.Status == "Pending" && ownedRestaurants.Contains(m.RestaurantId))
                    .ToListAsync();
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Approve selected restaurants
            if (SelectedRestaurantIds.Any())
            {
                var restaurants = await _context.Restaurants
                    .Where(r => SelectedRestaurantIds.Contains(r.Id))
                    .ToListAsync();

                foreach (var restaurant in restaurants)
                {
                    restaurant.Status = "Approved";
                }
            }

            // Approve selected menu items
            if (SelectedMenuItemIds.Any())
            {
                var menuItems = await _context.MenuItems
                    .Where(m => SelectedMenuItemIds.Contains(m.Id))
                    .ToListAsync();

                foreach (var menuItem in menuItems)
                {
                    menuItem.Status = "Approved";
                }
            }

            await _context.SaveChangesAsync();

            return RedirectToPage();
        }
    }
}
