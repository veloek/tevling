namespace Tevling.Model;

public record ScoreBoard(IReadOnlyList<AthleteScore> Scores, bool Attribute = false);
