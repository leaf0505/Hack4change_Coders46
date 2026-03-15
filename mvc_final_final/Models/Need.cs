using System.ComponentModel.DataAnnotations;

namespace mvc_final_final.Models;

public enum Priority { Critical = 0, Normal = 1, Low = 2 }

public class Need
{
    public int Id { get; set; }

    public int OrganisationId { get; set; }
    public Organisation? Organisation { get; set; }

    [Required(ErrorMessage = "Item name is required.")]
    [Display(Name = "Item")]
    public string ItemName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Category")]
    public string Category { get; set; } = "food";

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 0.")]
    [Display(Name = "Quantity needed")]
    public int QuantityNeeded { get; set; }

    [Display(Name = "Quantity received")]
    public int QuantityReceived { get; set; } = 0;

    [Display(Name = "Priority")]
    public Priority Priority { get; set; } = Priority.Normal;

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Computed — safe in Razor, never use in EF queries
    public bool IsComplete => QuantityReceived >= QuantityNeeded;
    public int QuantityRemaining => Math.Max(0, QuantityNeeded - QuantityReceived);
    public int CompletionPercent => QuantityNeeded == 0 ? 0
        : (int)Math.Min(100, (double)QuantityReceived / QuantityNeeded * 100);

    public List<Donation> Donations { get; set; } = new();
    public List<Surplus> Surpluses { get; set; } = new();
}
