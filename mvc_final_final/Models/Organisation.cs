using System.ComponentModel.DataAnnotations;

namespace mvc_final_final.Models;

public class Organisation
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public AppUser? User { get; set; }

    [Required(ErrorMessage = "Organisation name is required.")]
    [Display(Name = "Organisation name")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Description")]
    public string Description { get; set; } = string.Empty;

    [Phone]
    [Display(Name = "Phone")]
    public string Phone { get; set; } = string.Empty;

    public List<Need> Needs { get; set; } = new();
}
