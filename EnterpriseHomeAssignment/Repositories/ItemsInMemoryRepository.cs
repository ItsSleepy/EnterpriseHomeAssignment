using EnterpriseHomeAssignment.Models;

namespace EnterpriseHomeAssignment.Repositories
{
    public class ItemsInMemoryRepository : IItemsRepository
    {
        private readonly List<IItemValidating> _items = new();

        public Task<List<IItemValidating>> GetAllAsync()
        {
            return Task.FromResult(_items.ToList());
        }

        public Task SaveAsync(List<IItemValidating> items)
        {
            _items.Clear();
            
            // Assign temporary IDs for preview
            int restaurantIdCounter = 1;
            foreach (var item in items)
            {
                if (item is Restaurant restaurant)
                {
                    restaurant.Id = restaurantIdCounter++;
                }
            }
            
            // Update menu items with restaurant IDs from references
            foreach (var item in items)
            {
                if (item is MenuItem menuItem && menuItem.Restaurant != null)
                {
                    menuItem.RestaurantId = menuItem.Restaurant.Id;
                }
            }
            
            _items.AddRange(items);
            return Task.CompletedTask;
        }

        public Task ApproveAsync(List<int> itemIds)
        {
            foreach (var item in _items)
            {
                if (item is Restaurant restaurant && itemIds.Contains(restaurant.Id))
                {
                    restaurant.Status = "Approved";
                }
            }
            return Task.CompletedTask;
        }
    }
}
