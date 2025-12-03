using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EnterpriseHomeAssignment.Models
{
    public class MenuItem : IItemValidating
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public string Title { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Required]
        public int RestaurantId { get; set; }

        [ForeignKey(nameof(RestaurantId))]
        public Restaurant? Restaurant { get; set; }

        [Required]
        public string Status { get; set; } = "Pending";

        public string? ImagePath { get; set; }

        public List<string> GetValidatorEmails()
        {
            if (Restaurant != null)
            {
                return new List<string> { Restaurant.OwnerEmailAddress };
            }
            return new List<string>();
        }

        public string GetCardPartial()
        {
            return "_MenuItemCard";
        }
    }
}
