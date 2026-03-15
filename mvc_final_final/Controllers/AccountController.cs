using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using mvc_final_final.Data;
using mvc_final_final.Models;

namespace mvc_final_final.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<AppUser> _users;
    private readonly SignInManager<AppUser> _signIn;
    private readonly AppDbContext _db;

    public AccountController(UserManager<AppUser> users, SignInManager<AppUser> signIn, AppDbContext db)
    {
        _users = users; _signIn = signIn; _db = db;
    }

    [HttpGet]
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Dashboard", "Organisation");
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string email, string password)
    {
        var result = await _signIn.PasswordSignInAsync(email, password, false, false);
        if (!result.Succeeded)
        {
            ModelState.AddModelError("", "Invalid email or password.");
            return View();
        }
        return RedirectToAction("Dashboard", "Organisation");
    }

    [HttpGet]
    public IActionResult Register() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(string fullName, string email, string password,
        string orgName, string description, string phone)
    {
        if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(orgName))
        {
            ModelState.AddModelError("", "All required fields must be filled.");
            return View();
        }

        var user = new AppUser { UserName = email, Email = email, FullName = fullName, EmailConfirmed = true };
        var result = await _users.CreateAsync(user, password);

        if (!result.Succeeded)
        {
            foreach (var e in result.Errors) ModelState.AddModelError("", e.Description);
            return View();
        }

        _db.Organisations.Add(new Organisation
        {
            UserId = user.Id,
            Name = orgName,
            Description = description ?? "",
            Phone = phone ?? ""
        });
        await _db.SaveChangesAsync();
        await _signIn.SignInAsync(user, false);
        return RedirectToAction("Dashboard", "Organisation");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signIn.SignOutAsync();
        return RedirectToAction("Index", "Donor");
    }
}
