using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using mvc_final_final.Models;

namespace mvc_final_final.Data;

public class AppDbContext : IdentityDbContext<AppUser, IdentityRole<int>, int>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // Organisation side (requires login)
    public DbSet<Organisation> Organisations { get; set; }
    public DbSet<Need> Needs { get; set; }
    public DbSet<Surplus> Surpluses { get; set; }
    public DbSet<TransferProposal> TransferProposals { get; set; }

    // Public donor side (no account)
    public DbSet<GuestDonor> GuestDonors { get; set; }
    public DbSet<Donation> Donations { get; set; }

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        b.Entity<Organisation>()
            .HasOne(o => o.User).WithOne()
            .HasForeignKey<Organisation>(o => o.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<Need>()
            .HasOne(n => n.Organisation).WithMany(o => o.Needs)
            .HasForeignKey(n => n.OrganisationId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<Donation>()
            .HasOne(d => d.Need).WithMany(n => n.Donations)
            .HasForeignKey(d => d.NeedId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<Donation>()
            .HasOne(d => d.GuestDonor).WithMany(g => g.Donations)
            .HasForeignKey(d => d.GuestDonorId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Entity<GuestDonor>()
            .HasIndex(g => g.Email).IsUnique();

        b.Entity<Surplus>()
            .HasOne(s => s.Need).WithMany(n => n.Surpluses)
            .HasForeignKey(s => s.NeedId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<Surplus>()
            .HasOne(s => s.OfferedToOrganisation).WithMany()
            .HasForeignKey(s => s.OfferedToOrganisationId)
            .OnDelete(DeleteBehavior.SetNull);

        b.Entity<TransferProposal>()
            .HasOne(p => p.Surplus).WithMany(s => s.Proposals)
            .HasForeignKey(p => p.SurplusId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<TransferProposal>()
            .HasOne(p => p.ToOrganisation).WithMany()
            .HasForeignKey(p => p.ToOrganisationId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<Need>()
            .Property(n => n.Priority).HasConversion<int>();
    }
}
