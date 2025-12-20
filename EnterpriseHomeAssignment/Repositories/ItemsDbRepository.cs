using EnterpriseHomeAssignment.Data;
using EnterpriseHomeAssignment.Models;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseHomeAssignment.Repositories
{
    public class ItemsDbRepository : IItemsRepository
    {
        private readonly ApplicationDbContext _context;

        public ItemsDbRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<IItemValidating>> GetAllAsync()
        {
            var items = new List<IItemValidating>();

            var restaurants = await _context.Restaurants.ToListAsync();
            items.AddRange(restaurants);

            var menuItems = await _context.MenuItems.Include(m => m.Restaurant).ToListAsync();
            items.AddRange(menuItems);

            return items;
        }

        public async Task SaveAsync(List<IItemValidating> items)
        {
            // First, save restaurants to get their auto-generated IDs
            var restaurants = items.OfType<Restaurant>().ToList();
            foreach (var restaurant in restaurants)
            {
                _context.Restaurants.Add(restaurant);
            }
            
            // Save restaurants first to generate IDs
            if (restaurants.Any())
            {
                await _context.SaveChangesAsync();
            }

            // Now save menu items - they should already have Restaurant references set
            var menuItems = items.OfType<MenuItem>().ToList();
            foreach (var menuItem in menuItems)
            {
                // If the menuItem has a Restaurant reference, use its ID
                if (menuItem.Restaurant != null)
                {
                    menuItem.RestaurantId = menuItem.Restaurant.Id;
                }
                _context.MenuItems.Add(menuItem);
            }

            if (menuItems.Any())
            {
                await _context.SaveChangesAsync();
            }
        }

        public async Task ApproveAsync(List<int> itemIds)
        {
            var restaurants = await _context.Restaurants
                .Where(r => itemIds.Contains(r.Id))
                .ToListAsync();

            foreach (var restaurant in restaurants)
            {
                restaurant.Status = "Approved";
            }

            await _context.SaveChangesAsync();
        }
    }
}
