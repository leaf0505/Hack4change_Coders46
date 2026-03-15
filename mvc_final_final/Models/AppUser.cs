using Microsoft.AspNetCore.Identity;

namespace mvc_final_final.Models;

public class AppUser : IdentityUser<int>
{
    public string FullName { get; set; } = string.Empty;
}
