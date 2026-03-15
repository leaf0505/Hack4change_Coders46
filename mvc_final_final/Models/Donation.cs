using System.ComponentModel.DataAnnotations;

namespace mvc_final_final.Models;

public class Donation
{
    public int Id { get; set; }

    public int NeedId { get; set; }
    public Need? Need { get; set; }

    public int GuestDonorId { get; set; }
    public GuestDonor? GuestDonor { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "La quantité doit être > 0.")]
    [Display(Name = "Quantité")]
    public int Quantity { get; set; }

    [MaxLength(500)]
    [Display(Name = "Message (optionnel)")]
    public string? Message { get; set; }

    public DateTime DonatedAt { get; set; } = DateTime.UtcNow;
}
