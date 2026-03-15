namespace mvc_final_final.Models;

public class TransferProposal
{
    public int Id { get; set; }

    public int SurplusId { get; set; }
    public Surplus? Surplus { get; set; }

    public int ToOrganisationId { get; set; }
    public Organisation? ToOrganisation { get; set; }

    // Pending | Accepted | Declined
    public string Status { get; set; } = "Pending";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
