using EnterpriseHomeAssignment.Data;
using EnterpriseHomeAssignment.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseHomeAssignment.Pages
{
    public class CatalogModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CatalogModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<IItemValidating> Items { get; set; } = new();
        public string ViewMode { get; set; } = "card";

        public async Task OnGetAsync(string viewMode = "card")
        {
            ViewMode = viewMode;
            
            var restaurants = await _context.Restaurants
                .Where(r => r.Status == "Approved")
                .ToListAsync();

            var menuItems = await _context.MenuItems
                .Include(m => m.Restaurant)
                .Where(m => m.Status == "Approved")
                .ToListAsync();

            Items = new List<IItemValidating>();
            Items.AddRange(restaurants);
            Items.AddRange(menuItems);
        }
    }
}
