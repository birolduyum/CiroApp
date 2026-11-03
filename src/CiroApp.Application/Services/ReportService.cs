using CiroApp.Application.Abstractions;
using CiroApp.Domain.Reports;

namespace CiroApp.Application.Services;

public sealed record ReportFile(string FileName, byte[] Content);

public sealed class ReportService(ICiroRepository repository, IReportBuilder builder)
{
    public async Task<ReportFile> BuildAsync(ReportRange range)
    {
        var days = await repository.GetRangeAsync(range.From, range.To);
        return new ReportFile(ReportNaming.FileName(range), builder.Build(range, days));
    }
}
