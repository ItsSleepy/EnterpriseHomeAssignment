using System.ComponentModel.DataAnnotations;

namespace EnterpriseHomeAssignment.Models
{
    public class Restaurant : IItemValidating
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string OwnerEmailAddress { get; set; } = string.Empty;

        [Required]
        public string Status { get; set; } = "Pending";

        public List<string> GetValidatorEmails()
        {
            return new List<string> { "siteadmin@example.com" };
        }

        public string GetCardPartial()
        {
            return "_RestaurantCard";
        }
    }
}
