using CiroApp.Domain.Formatting;

namespace CiroApp.Domain.Reports;

public static class ReportNaming
{
    public static string Title(ReportRange range) =>
        range.IsFullMonth
            ? TrFormat.YilAy(range.From)
            : $"{TrFormat.Tarih(range.From)} - {TrFormat.Tarih(range.To)}";

    public static string FileName(ReportRange range) => $"{Title(range)} Cirosu.xlsx";
}
