using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using mvc_final_final.Data;
using mvc_final_final.Models;
using mvc_final_final.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlite("Data Source=donations.db"));

builder.Services.AddIdentity<AppUser, IdentityRole<int>>(o =>
{
    o.Password.RequireDigit = false;
    o.Password.RequireUppercase = false;
    o.Password.RequireNonAlphanumeric = false;
    o.Password.RequiredLength = 6;
    o.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(o =>
{
    o.LoginPath = "/Account/Login";
    o.AccessDeniedPath = "/Account/Login";
});

// Session for Excel import preview
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(o =>
{
    o.IdleTimeout = TimeSpan.FromMinutes(20);
    o.Cookie.HttpOnly = true;
    o.Cookie.IsEssential = true;
});

builder.Services.AddScoped<SurplusService>();
builder.Services.AddScoped<ExcelImportService>();

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Donor}/{action=Index}/{id?}");

// Seed
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
    db.Database.Migrate();

    if (!db.Users.Any())
    {
        var user = new AppUser { UserName = "org@demo.com", Email = "org@demo.com", FullName = "Community Centre", EmailConfirmed = true };
        await userMgr.CreateAsync(user, "demo123");

        var org = new Organisation { UserId = user.Id, Name = "Community Centre", Description = "Helping families in need.", Phone = "506-555-0001" };
        db.Organisations.Add(org);
        await db.SaveChangesAsync();

        db.Needs.AddRange(
            new Need { OrganisationId = org.Id, ItemName = "Winter coats",     Category = "clothing", QuantityNeeded = 30,  QuantityReceived = 8,  Priority = Priority.Critical },
            new Need { OrganisationId = org.Id, ItemName = "Canned goods",     Category = "food",     QuantityNeeded = 100, QuantityReceived = 45, Priority = Priority.Normal   },
            new Need { OrganisationId = org.Id, ItemName = "Blankets",         Category = "bedding",  QuantityNeeded = 20,  QuantityReceived = 3,  Priority = Priority.Critical },
            new Need { OrganisationId = org.Id, ItemName = "Soap / shampoo",   Category = "hygiene",  QuantityNeeded = 50,  QuantityReceived = 20, Priority = Priority.Normal   }
        );
        await db.SaveChangesAsync();
    }
}

app.Run();
