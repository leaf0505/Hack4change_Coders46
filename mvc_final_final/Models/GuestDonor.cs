using System.ComponentModel.DataAnnotations;

namespace mvc_final_final.Models;

public class GuestDonor
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Name is required.")]
    [Display(Name = "Your name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address.")]
    [Display(Name = "Email address")]
    public string Email { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastDonationAt { get; set; } = DateTime.UtcNow;

    public List<Donation> Donations { get; set; } = new();
}
