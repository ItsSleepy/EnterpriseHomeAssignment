using EnterpriseHomeAssignment.Models;
using System.Text.Json;

namespace EnterpriseHomeAssignment.Factories
{
    public class ImportItemFactory
    {
        public List<IItemValidating> Create(string json)
        {
            var items = new List<IItemValidating>();
            var restaurantIdMap = new Dictionary<string, Restaurant>(); // Map string IDs to Restaurant objects

            try
            {
                var document = JsonDocument.Parse(json);
                var root = document.RootElement;

                // Support both formats: Appendix A (array) and old format (object with arrays)
                JsonElement itemsArray;
                
                if (root.ValueKind == JsonValueKind.Array)
                {
                    // Appendix A format: array of mixed items
                    itemsArray = root;
                }
                else
                {
                    // Old format: { "Restaurants": [...], "MenuItems": [...] }
                    return ParseOldFormat(root);
                }

                // First pass: Parse restaurants and create ID mapping
                foreach (var element in itemsArray.EnumerateArray())
                {
                    if (element.TryGetProperty("type", out var typeProperty))
                    {
                        var type = typeProperty.GetString()?.ToLower();
                        
                        if (type == "restaurant")
                        {
                            var restaurant = new Restaurant
                            {
                                Name = element.GetProperty("name").GetString() ?? string.Empty,
                                OwnerEmailAddress = element.GetProperty("ownerEmailAddress").GetString() ?? string.Empty,
                                Status = "Pending"
                            };
                            items.Add(restaurant);

                            // Map string ID to the Restaurant object
                            if (element.TryGetProperty("id", out var idProperty))
                            {
                                var stringId = idProperty.GetString();
                                if (!string.IsNullOrEmpty(stringId))
                                {
                                    restaurantIdMap[stringId] = restaurant;
                                }
                            }
                        }
                    }
                }

                // Second pass: Parse menu items and link to restaurants
                foreach (var element in itemsArray.EnumerateArray())
                {
                    if (element.TryGetProperty("type", out var typeProperty))
                    {
                        var type = typeProperty.GetString()?.ToLower();
                        
                        if (type == "menuitem")
                        {
                            var menuItem = new MenuItem
                            {
                                Title = element.GetProperty("title").GetString() ?? string.Empty,
                                Price = element.GetProperty("price").GetDecimal(),
                                Status = "Pending"
                            };

                            // Link to restaurant by reference
                            if (element.TryGetProperty("restaurantId", out var restaurantIdProperty))
                            {
                                var restaurantStringId = restaurantIdProperty.GetString();
                                if (!string.IsNullOrEmpty(restaurantStringId) && restaurantIdMap.TryGetValue(restaurantStringId, out var restaurant))
                                {
                                    // Set the Restaurant reference - RestaurantId will be set when saving to DB
                                    menuItem.Restaurant = restaurant;
                                }
                            }

                            items.Add(menuItem);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Invalid JSON format: {ex.Message}", ex);
            }

            return items;
        }

        private List<IItemValidating> ParseOldFormat(JsonElement root)
        {
            var items = new List<IItemValidating>();

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

            return items;
        }
    }
}
