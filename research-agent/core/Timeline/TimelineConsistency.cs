namespace ResearchAgent.Core.Timeline;

public enum LifeEventType { Birth, Residence, Census, Marriage, ChildBirth, Military, Occupation, Death, Burial, Other }

public sealed record LifeEvent(Guid Id, Guid PersonId, LifeEventType Type, DateOnly? Date,
    DateOnly? EndDate, string? Place, Guid? SourceId = null, string? Description = null);

public sealed record TimelineWarning(string Code, string Message, Guid? EventA = null, Guid? EventB = null);

public static class TimelineConsistency
{
    public static IReadOnlyList<TimelineWarning> Check(IEnumerable<LifeEvent> events)
    {
        var list = events.Where(e => e.Date is not null).OrderBy(e => e.Date).ToList();
        var warnings = new List<TimelineWarning>();
        var birth = list.FirstOrDefault(e => e.Type == LifeEventType.Birth);
        var death = list.FirstOrDefault(e => e.Type == LifeEventType.Death);

        if (birth?.Date is DateOnly b && death?.Date is DateOnly d && d < b)
            warnings.Add(new("DEATH_BEFORE_BIRTH", "Death occurs before birth.", birth.Id, death.Id));

        if (birth?.Date is DateOnly born)
        {
            foreach (var e in list.Where(e => e.Type == LifeEventType.Marriage && e.Date is not null))
            {
                var age = YearsBetween(born, e.Date!.Value);
                if (age < 12) warnings.Add(new("MARRIAGE_AGE", $"Marriage age is approximately {age}.", birth.Id, e.Id));
            }
            foreach (var e in list.Where(e => e.Type == LifeEventType.ChildBirth && e.Date is not null))
            {
                var age = YearsBetween(born, e.Date!.Value);
                if (age < 10 || age > 85) warnings.Add(new("PARENT_AGE", $"Parent age at child birth is approximately {age}.", birth.Id, e.Id));
            }
        }

        if (death?.Date is DateOnly died)
        {
            foreach (var e in list.Where(e => e.Date > died && e.Type is not LifeEventType.Burial))
                warnings.Add(new("EVENT_AFTER_DEATH", $"{e.Type} occurs after recorded death.", death.Id, e.Id));
        }

        return warnings;
    }

    private static int YearsBetween(DateOnly start, DateOnly end)
    {
        var years = end.Year - start.Year;
        if (end < start.AddYears(years)) years--;
        return years;
    }
}
