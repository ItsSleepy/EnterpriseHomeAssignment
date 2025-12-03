using EnterpriseHomeAssignment.Data;
using EnterpriseHomeAssignment.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseHomeAssignment.Filters
{
    public class ValidatorAuthorizationFilter : IAsyncActionFilter
    {
        private readonly ApplicationDbContext _context;

        public ValidatorAuthorizationFilter(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var user = context.HttpContext.User;
            var userEmail = user.Identity?.Name;

            if (string.IsNullOrEmpty(userEmail))
            {
                context.Result = new ForbidResult();
                return;
            }

            // Get item IDs from route or form data
            var itemIdsParam = context.ActionArguments.FirstOrDefault(a => a.Key == "itemIds").Value;
            
            if (itemIdsParam is List<int> itemIds && itemIds.Any())
            {
                // Check if user is authorized to approve these items
                var restaurants = await _context.Restaurants
                    .Where(r => itemIds.Contains(r.Id))
                    .ToListAsync();

                foreach (var restaurant in restaurants)
                {
                    var validators = restaurant.GetValidatorEmails();
                    if (!validators.Contains(userEmail))
                    {
                        context.Result = new StatusCodeResult(403); // Forbidden
                        return;
                    }
                }
            }

            await next();
        }
    }
}
