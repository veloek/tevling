using Bogus;
using Tevling.Strava;

namespace Tevling.Bogus;

public sealed class ChallengeFormModelFaker : Faker<ChallengeFormModel>
{
    public ChallengeFormModelFaker(int createdBy, bool isPrivate)
    {
        RuleFor(cfm => cfm.Title, f => "Challenge" + f.UniqueIndex);
        RuleFor(cfm => cfm.Description, f => f.Lorem.Sentence());
        RuleFor(cfm => cfm.Start, f => DateTimeOffset.Now);
        RuleFor(cfm => cfm.End, f => DateTimeOffset.Now.AddMonths(1));
        RuleFor(cfm => cfm.Measurement, f => f.PickRandom<ChallengeMeasurement>());
        RuleFor(cfm => cfm.IndividualGoal, f => f.Random.Int(5, 100));
        RuleFor(
            cfm => cfm.ActivityTypes,
            f =>
            {
                List<ActivityType> values = [.. Enum.GetValues<ActivityType>()];
                int count = f.Random.Int(1, 5);
                return [.. Enumerable.Range(0, count).Select(_=> f.PickRandom(values)).Distinct()];
            }
        );
        RuleFor(cfm => cfm.IsPrivate, isPrivate);
        RuleFor(cfm => cfm.CreatedBy, createdBy);
    }
}
