using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mvc_final_final.Data;
using mvc_final_final.Models;
using mvc_final_final.Services;
using OfficeOpenXml;

namespace mvc_final_final.Controllers;

[Authorize]
public class OrganisationController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _users;
    private readonly SurplusService _surplus;
    private readonly ExcelImportService _excel;

    public OrganisationController(AppDbContext db, UserManager<AppUser> users,
        SurplusService surplus, ExcelImportService excel)
    {
        _db = db; _users = users; _surplus = surplus; _excel = excel;
    }

    private async Task<Organisation?> GetOrgAsync()
    {
        var user = await _users.GetUserAsync(User);
        if (user == null) return null;
        return await _db.Organisations.FirstOrDefaultAsync(o => o.UserId == user.Id);
    }

    // GET /Organisation/Dashboard
    public async Task<IActionResult> Dashboard()
    {
        var org = await GetOrgAsync();
        if (org == null) return RedirectToAction("Login", "Account");

        var needs = await _db.Needs
            .Where(n => n.OrganisationId == org.Id && n.IsActive)
            .OrderBy(n => (int)n.Priority)
            .ToListAsync();

        var proposals = await _db.TransferProposals
            .Include(p => p.Surplus).ThenInclude(s => s.Need).ThenInclude(n => n == null ? null : n.Organisation)
            .Where(p => p.ToOrganisationId == org.Id && p.Status == "Pending")
            .ToListAsync();

        var donations = await _db.Donations
            .Include(d => d.GuestDonor)
            .Include(d => d.Need)
            .Where(d => d.Need != null && d.Need.OrganisationId == org.Id)
            .OrderByDescending(d => d.DonatedAt)
            .Take(10)
            .ToListAsync();

        ViewBag.Org = org;
        ViewBag.Needs = needs;
        ViewBag.Proposals = proposals;
        ViewBag.Donations = donations;
        return View();
    }

    // GET /Organisation/CreateNeed
    [HttpGet]
    public IActionResult CreateNeed() => View();

    // POST /Organisation/CreateNeed
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateNeed(Need model)
    {
        var org = await GetOrgAsync();
        if (org == null) return RedirectToAction("Login", "Account");
        if (!ModelState.IsValid) return View(model);

        model.OrganisationId = org.Id;
        model.QuantityReceived = 0;
        model.IsActive = true;
        model.CreatedAt = DateTime.UtcNow;
        _db.Needs.Add(model);
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Need \"{model.ItemName}\" added successfully.";
        return RedirectToAction(nameof(Dashboard));
    }

    // POST /Organisation/DeleteNeed
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteNeed(int id)
    {
        var org = await GetOrgAsync();
        var need = await _db.Needs.FindAsync(id);
        if (need == null || need.OrganisationId != org?.Id) return Forbid();

        need.IsActive = false;
        await _db.SaveChangesAsync();
        TempData["Success"] = "Need archived.";
        return RedirectToAction(nameof(Dashboard));
    }

    // POST /Organisation/Accept
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Accept(int proposalId)
    {
        var org = await GetOrgAsync();
        var proposal = await _db.TransferProposals.FindAsync(proposalId);
        if (proposal == null || proposal.ToOrganisationId != org?.Id) return Forbid();

        await _surplus.AcceptAsync(proposalId);
        TempData["Success"] = "Surplus accepted and credited to your need.";
        return RedirectToAction(nameof(Dashboard));
    }

    // POST /Organisation/Decline
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Decline(int proposalId)
    {
        var org = await GetOrgAsync();
        var proposal = await _db.TransferProposals.FindAsync(proposalId);
        if (proposal == null || proposal.ToOrganisationId != org?.Id) return Forbid();

        await _surplus.DeclineAsync(proposalId);
        TempData["Info"] = "Surplus declined — offered to the next organisation.";
        return RedirectToAction(nameof(Dashboard));
    }

    // GET /Organisation/ImportExcel
    [HttpGet]
    public IActionResult ImportExcel() => View();

    // POST /Organisation/PreviewExcel
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult PreviewExcel(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            TempData["Error"] = "Please select an Excel file (.xlsx).";
            return RedirectToAction(nameof(ImportExcel));
        }

        if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "Only .xlsx files are supported.";
            return RedirectToAction(nameof(ImportExcel));
        }

        using var stream = file.OpenReadStream();
        var result = _excel.Parse(stream);

        if (result.HasErrors)
        {
            ViewBag.Errors = result.Errors;
            return View("ImportExcel");
        }

        HttpContext.Session.SetObjectAsJson("ExcelPreview", result.Rows);
        return View("PreviewExcel", result.Rows);
    }

    // POST /Organisation/ConfirmImport
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmImport()
    {
        var org = await GetOrgAsync();
        if (org == null) return RedirectToAction("Login", "Account");

        var rows = HttpContext.Session.GetObjectFromJson<List<NeedPreview>>("ExcelPreview");
        if (rows == null || !rows.Any())
        {
            TempData["Error"] = "Session expired. Please re-upload the file.";
            return RedirectToAction(nameof(ImportExcel));
        }

        var valid = rows.Where(r => r.IsValid).ToList();
        foreach (var row in valid)
        {
            _db.Needs.Add(new Need
            {
                OrganisationId = org.Id,
                ItemName = row.ItemName,
                Category = row.Category,
                QuantityNeeded = row.QuantityNeeded,
                Priority = row.Priority,
                QuantityReceived = 0,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync();
        HttpContext.Session.Remove("ExcelPreview");

        TempData["Success"] = $"{valid.Count} need(s) imported successfully from Excel.";
        return RedirectToAction(nameof(Dashboard));
    }

    // GET /Organisation/DownloadTemplate
    [HttpGet]
    public IActionResult DownloadTemplate()
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using var pkg = new ExcelPackage();
        var ws = pkg.Workbook.Worksheets.Add("Needs");

        ws.Cells[1, 1].Value = "ItemName";
        ws.Cells[1, 2].Value = "Category";
        ws.Cells[1, 3].Value = "QuantityNeeded";
        ws.Cells[1, 4].Value = "Priority";

        using (var hdr = ws.Cells[1, 1, 1, 4])
        {
            hdr.Style.Font.Bold = true;
            hdr.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            hdr.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(27, 110, 194));
            hdr.Style.Font.Color.SetColor(System.Drawing.Color.White);
        }

        ws.Cells[2, 1].Value = "Winter coats";  ws.Cells[2, 2].Value = "clothing"; ws.Cells[2, 3].Value = 30;  ws.Cells[2, 4].Value = "Critical";
        ws.Cells[3, 1].Value = "Canned goods";  ws.Cells[3, 2].Value = "food";     ws.Cells[3, 3].Value = 100; ws.Cells[3, 4].Value = "Normal";
        ws.Cells[4, 1].Value = "Soap";          ws.Cells[4, 2].Value = "hygiene";  ws.Cells[4, 3].Value = 50;  ws.Cells[4, 4].Value = "Low";

        ws.Cells.AutoFitColumns();

        var bytes = pkg.GetAsByteArray();
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "needs_template.xlsx");
    }
}
