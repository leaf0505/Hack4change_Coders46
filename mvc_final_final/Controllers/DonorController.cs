using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mvc_final_final.Data;
using mvc_final_final.Models;
using mvc_final_final.Services;

namespace mvc_final_final.Controllers;

public class DonorController : Controller
{
    private readonly AppDbContext _db;
    private readonly SurplusService _surplus;

    private static readonly List<string> Categories =
        new() { "food", "clothing", "hygiene", "bedding", "beds", "other" };

    public DonorController(AppDbContext db, SurplusService surplus)
    {
        _db = db; _surplus = surplus;
    }

    // GET / — public homepage
    public async Task<IActionResult> Index(string? category = null)
    {
        var query = _db.Needs
            .Include(n => n.Organisation)
            .Where(n => n.IsActive && n.QuantityReceived < n.QuantityNeeded);

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(n => n.Category == category);

        var needs = await query.ToListAsync();
        var sorted = needs
            .OrderBy(n => (int)n.Priority)
            .ThenByDescending(n => n.QuantityNeeded - n.QuantityReceived)
            .ToList();

        ViewBag.Categories = Categories;
        ViewBag.SelectedCategory = category;
        ViewBag.SavedEmail = Request.Cookies["donor_email"] ?? "";
        ViewBag.SavedName  = Request.Cookies["donor_name"]  ?? "";
        return View(sorted);
    }

    // GET /Donor/Donate/5
    [HttpGet]
    public async Task<IActionResult> Donate(int id)
    {
        var need = await _db.Needs
            .Include(n => n.Organisation)
            .FirstOrDefaultAsync(n => n.Id == id && n.IsActive && n.QuantityReceived < n.QuantityNeeded);

        if (need == null) return NotFound();
        ViewBag.SavedEmail = Request.Cookies["donor_email"] ?? "";
        ViewBag.SavedName  = Request.Cookies["donor_name"]  ?? "";
        return View(need);
    }

    // POST /Donor/Donate
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Donate(int needId, string donorName, string donorEmail,
        int quantity, string? message)
    {
        if (string.IsNullOrWhiteSpace(donorName) || string.IsNullOrWhiteSpace(donorEmail) || quantity <= 0)
        {
            TempData["Error"] = "Please fill in all required fields.";
            return RedirectToAction(nameof(Donate), new { id = needId });
        }

        var need = await _db.Needs.Include(n => n.Organisation)
            .FirstOrDefaultAsync(n => n.Id == needId && n.IsActive);
        if (need == null) return NotFound();

        var guest = await _db.GuestDonors.FirstOrDefaultAsync(g => g.Email == donorEmail);
        if (guest == null)
        {
            guest = new GuestDonor { FullName = donorName, Email = donorEmail };
            _db.GuestDonors.Add(guest);
            await _db.SaveChangesAsync();
        }
        else
        {
            guest.FullName = donorName;
            guest.LastDonationAt = DateTime.UtcNow;
        }

        _db.Donations.Add(new Donation
        {
            NeedId = needId,
            GuestDonorId = guest.Id,
            Quantity = quantity,
            Message = message,
            DonatedAt = DateTime.UtcNow
        });
        need.QuantityReceived += quantity;
        await _db.SaveChangesAsync();

        await _surplus.ProcessAsync(need);

        var opts = new CookieOptions { Expires = DateTime.Now.AddDays(30), HttpOnly = true };
        Response.Cookies.Append("donor_email", donorEmail, opts);
        Response.Cookies.Append("donor_name",  donorName,  opts);

        TempData["Success"] = $"Thank you, {donorName}! Your donation of {quantity} × \"{need.ItemName}\" has been recorded.";
        return RedirectToAction(nameof(Index));
    }
}
