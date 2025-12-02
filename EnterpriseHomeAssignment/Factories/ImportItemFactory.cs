using EnterpriseHomeAssignment.Models;
using System.Text.Json;

namespace EnterpriseHomeAssignment.Factories
{
    public class ImportItemFactory
    {
        public List<IItemValidating> Create(string json)
        {
            var items = new List<IItemValidating>();

            try
            {
                var document = JsonDocument.Parse(json);
                var root = document.RootElement;

                // Parse Restaurants
                if (root.TryGetProperty("Restaurants", out var restaurantsElement))
                {
                    foreach (var restaurantElement in restaurantsElement.EnumerateArray())
                    {
                        var restaurant = new Restaurant
                        {
                            Name = restaurantElement.GetProperty("Name").GetString() ?? string.Empty,
                            OwnerEmailAddress = restaurantElement.GetProperty("OwnerEmailAddress").GetString() ?? string.Empty,
                            Status = "Pending"
                        };
                        items.Add(restaurant);
                    }
                }

                // Parse MenuItems
                if (root.TryGetProperty("MenuItems", out var menuItemsElement))
                {
                    foreach (var menuItemElement in menuItemsElement.EnumerateArray())
                    {
                        var menuItem = new MenuItem
                        {
                            Id = Guid.NewGuid(),
                            Title = menuItemElement.GetProperty("Title").GetString() ?? string.Empty,
                            Price = menuItemElement.GetProperty("Price").GetDecimal(),
                            RestaurantId = menuItemElement.GetProperty("RestaurantId").GetInt32(),
                            Status = "Pending"
                        };
                        items.Add(menuItem);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Invalid JSON format: {ex.Message}", ex);
            }

            return items;
        }
    }
}
