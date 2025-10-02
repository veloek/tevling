using Bogus;
using Tevling.Strava;

namespace Tevling.Bogus;

public class DetailedActivityFaker : Faker<DetailedActivity>
{
    public DetailedActivityFaker(string name, long stravaId)
    {
        RuleFor(da => da.Id, f => stravaId);
        RuleFor(da => da.Name, f => name);
        RuleFor(da => da.Description, f => f.Lorem.Sentence());
        RuleFor(da => da.Distance, f => f.Random.Float(1000, 10000));
        RuleFor(da => da.MovingTime, f => f.Random.Int(1000, 10000));
        RuleFor(da => da.ElapsedTime, f => f.Random.Int(1000, 10000));
        RuleFor(da => da.TotalElevationGain, f => f.Random.Float(1000, 10000));
        RuleFor(da => da.Calories, f => f.Random.Float(1000, 10000));
        RuleFor(da => da.Type, f => f.PickRandom<ActivityType>());
        RuleFor(da => da.StartDate, f => f.Date.Past(yearsToGoBack: 1));
        RuleFor(da => da.Manual, f => f.Random.Bool());
    }
}
