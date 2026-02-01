namespace Tevling.Model;

public record AthleteTickets(int AthleteId, string Name, int BaseTickets, int BonusTickets)
{
    public int TotalTickets => BaseTickets + BonusTickets;
}
