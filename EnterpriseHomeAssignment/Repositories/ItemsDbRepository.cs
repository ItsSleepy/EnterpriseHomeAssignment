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
            foreach (var item in items)
            {
                if (item is Restaurant restaurant)
                {
                    _context.Restaurants.Add(restaurant);
                }
                else if (item is MenuItem menuItem)
                {
                    _context.MenuItems.Add(menuItem);
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}
