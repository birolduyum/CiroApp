namespace CiroApp.Domain.Reports;

public sealed record ReportRange
{
    public DateOnly From { get; }
    public DateOnly To { get; }

    public ReportRange(DateOnly from, DateOnly to)
    {
        if (from > to)
        {
            throw new ArgumentException("Başlangıç tarihi bitiş tarihinden sonra olamaz.");
        }

        From = from;
        To = to;
    }

    public bool IsFullMonth =>
        From.Day == 1
        && From.Year == To.Year
        && From.Month == To.Month
        && To.Day == DateTime.DaysInMonth(To.Year, To.Month);

    public IEnumerable<DateOnly> Days()
    {
        for (var d = From; d <= To; d = d.AddDays(1))
        {
            yield return d;
        }
    }
}
