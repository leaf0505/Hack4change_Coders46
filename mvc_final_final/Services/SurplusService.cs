using Microsoft.EntityFrameworkCore;
using mvc_final_final.Data;
using mvc_final_final.Models;

namespace mvc_final_final.Services;

/// <summary>
/// Redistribution MVP :
/// 1. Si don > besoin → crée un Surplus
/// 2. Propose aux orgs qui ont un besoin critique du même article, dans l'ordre de priorité puis de manque
/// 3. Si refusé → propose au suivant
/// 4. Si plus personne → statut Exhausted
/// </summary>
public class SurplusService
{
    private readonly AppDbContext _db;

    public SurplusService(AppDbContext db)
    {
        _db = db;
    }

    public async Task ProcessAsync(Need need)
    {
        int extra = need.QuantityReceived - need.QuantityNeeded;
        if (extra <= 0) return;

        // Cap at needed
        need.QuantityReceived = need.QuantityNeeded;

        var surplus = new Surplus
        {
            NeedId = need.Id,
            Quantity = extra,
            Status = "Pending"
        };
        _db.Surpluses.Add(surplus);
        await _db.SaveChangesAsync();

        await ProposeNextAsync(surplus, need);
    }

    public async Task ProposeNextAsync(Surplus surplus, Need? need = null)
    {
        need ??= await _db.Needs.FindAsync(surplus.NeedId);
        if (need == null) return;

        // Already-declined org IDs for this surplus
        var declined = await _db.TransferProposals
            .Where(p => p.SurplusId == surplus.Id && p.Status == "Declined")
            .Select(p => p.ToOrganisationId)
            .ToListAsync();

        // Find next candidate:
        // Same category, active, incomplete, different org, not already declined
        // Order: Critical first, then most quantity needed (most urgent)
        var candidates = await _db.Needs
            .Where(n =>
                n.IsActive &&
                n.QuantityReceived < n.QuantityNeeded &&
                n.Category == need.Category &&
                n.OrganisationId != need.OrganisationId &&
                !declined.Contains(n.OrganisationId))
            .OrderBy(n => (int)n.Priority)
            .ThenByDescending(n => n.QuantityNeeded - n.QuantityReceived)
            .ToListAsync();

        var candidate = candidates.FirstOrDefault();

        if (candidate == null)
        {
            surplus.Status = "Exhausted";
            surplus.OfferedToOrganisationId = null;
            await _db.SaveChangesAsync();
            return;
        }

        surplus.Status = "Offered";
        surplus.OfferedToOrganisationId = candidate.OrganisationId;

        var proposal = new TransferProposal
        {
            SurplusId = surplus.Id,
            ToOrganisationId = candidate.OrganisationId,
            Status = "Pending"
        };
        _db.TransferProposals.Add(proposal);
        await _db.SaveChangesAsync();
    }

    public async Task AcceptAsync(int proposalId)
    {
        var proposal = await _db.TransferProposals
            .Include(p => p.Surplus).ThenInclude(s => s.Need)
            .FirstOrDefaultAsync(p => p.Id == proposalId);

        if (proposal == null) return;

        proposal.Status = "Accepted";
        if (proposal.Surplus == null) return;
        proposal.Surplus.Status = "Redistributed";

        // Credit the receiving org's matching need
        var receivingNeed = await _db.Needs
            .Where(n =>
                n.OrganisationId == proposal.ToOrganisationId &&
                n.Category == (proposal.Surplus.Need != null ? proposal.Surplus.Need.Category : "") &&
                n.IsActive &&
                n.QuantityReceived < n.QuantityNeeded)
            .OrderBy(n => (int)n.Priority)
            .FirstOrDefaultAsync();

        if (receivingNeed != null)
            receivingNeed.QuantityReceived = Math.Min(
                receivingNeed.QuantityNeeded,
                receivingNeed.QuantityReceived + proposal.Surplus.Quantity);

        await _db.SaveChangesAsync();
    }

    public async Task DeclineAsync(int proposalId)
    {
        var proposal = await _db.TransferProposals
            .Include(p => p.Surplus).ThenInclude(s => s.Need)
            .FirstOrDefaultAsync(p => p.Id == proposalId);

        if (proposal == null) return;

        proposal.Status = "Declined";
        if (proposal.Surplus != null)
        {
            proposal.Surplus.Status = "Pending";
            proposal.Surplus.OfferedToOrganisationId = null;
        }
        await _db.SaveChangesAsync();

        // Auto-propose to next in line
        await ProposeNextAsync(proposal.Surplus);
    }
}
