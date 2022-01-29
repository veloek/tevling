using System;
using System.Collections.Generic;

namespace Spur.Model;

public class Athlete
{
    public int Id { get; set; }
    public int StravaId { get; set; }
    public DateTimeOffset Created { get; set; }

    public ICollection<Activity>? Activities { get; set; }
    public ICollection<Challenge>? Challenges { get; set; }
}
