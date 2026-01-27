namespace MOE_System.Application.Common.Course;

public static class BillingPeriodResolver
{
    public static IReadOnlyList<(DateTime Start, DateTime End)> Resolve(
        DateTime courseStartDate,
        string billingCycle,
        DateTime courseEndDate
    )
    {
        var monthsPerCycle = billingCycle switch
        {
            "Monthly"    => 1,
            "Quarterly"  => 3,
            "BiAnnually" => 6,
            "Annually"   => 12,
            _ => throw new ArgumentException("Invalid billing cycle", nameof(billingCycle))
        };

        var periods = new List<(DateTime, DateTime)>();
        var cursor = courseStartDate;

        while (cursor < courseEndDate)
        {
            var next = cursor.AddMonths(monthsPerCycle);
            if (next > courseEndDate)
            {
                next = courseEndDate;
            }

            periods.Add((cursor, next));
            cursor = next;
        }

        return periods;
    }
}