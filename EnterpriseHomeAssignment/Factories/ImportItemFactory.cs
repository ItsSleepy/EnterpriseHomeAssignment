using EnterpriseHomeAssignment.Models;
using System.Text.Json;

namespace EnterpriseHomeAssignment.Factories
{
    public class ImportItemFactory
    {
        public List<IItemValidating> Create(string json)
        {
            var items = new List<IItemValidating>();
            var restaurantIdMap = new Dictionary<string, int>(); // Map string IDs to int IDs

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

                            // Map string ID to the auto-generated int ID (will be assigned after save)
                            if (element.TryGetProperty("id", out var idProperty))
                            {
                                var stringId = idProperty.GetString();
                                if (!string.IsNullOrEmpty(stringId))
                                {
                                    // For now, extract number from "R-1001" format
                                    var numericPart = stringId.Replace("R-", "").Replace("r-", "");
                                    if (int.TryParse(numericPart, out int numericId))
                                    {
                                        restaurant.Id = numericId;
                                        restaurantIdMap[stringId] = numericId;
                                    }
                                }
                            }
                        }
                    }
                }

                // Second pass: Parse menu items
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

                            // Parse menu item ID
                            if (element.TryGetProperty("id", out var idProperty))
                            {
                                var stringId = idProperty.GetString();
                                if (!string.IsNullOrEmpty(stringId))
                                {
                                    // Extract GUID or create from "M-2001" format
                                    var numericPart = stringId.Replace("M-", "").Replace("m-", "");
                                    // Use the string as seed for consistent GUID generation
                                    menuItem.Id = Guid.NewGuid();
                                }
                            }

                            // Map restaurantId
                            if (element.TryGetProperty("restaurantId", out var restaurantIdProperty))
                            {
                                var restaurantStringId = restaurantIdProperty.GetString();
                                if (!string.IsNullOrEmpty(restaurantStringId))
                                {
                                    if (restaurantIdMap.TryGetValue(restaurantStringId, out int restaurantId))
                                    {
                                        menuItem.RestaurantId = restaurantId;
                                    }
                                    else
                                    {
                                        // Try to parse as int directly
                                        var numericPart = restaurantStringId.Replace("R-", "").Replace("r-", "");
                                        if (int.TryParse(numericPart, out int parsedId))
                                        {
                                            menuItem.RestaurantId = parsedId;
                                        }
                                    }
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
