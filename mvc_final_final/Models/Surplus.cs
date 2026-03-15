namespace mvc_final_final.Models;

public class Surplus
{
    public int Id { get; set; }

    public int NeedId { get; set; }
    public Need? Need { get; set; }

    public int Quantity { get; set; }

    // Which org currently has the offer
    public int? OfferedToOrganisationId { get; set; }
    public Organisation? OfferedToOrganisation { get; set; }

    // Pending | Offered | Accepted | Declined | Redistributed | Exhausted
    public string Status { get; set; } = "Pending";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<TransferProposal> Proposals { get; set; } = new();
}
