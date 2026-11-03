using CiroApp.Domain.Models;
using CiroApp.Domain.Reports;

namespace CiroApp.Application.Abstractions;

public interface IReportBuilder
{
    byte[] Build(ReportRange range, IReadOnlyList<CiroDay> days);
}
