using CiroApp.Application.Abstractions;
using CiroApp.Domain.Models;
using CiroApp.Domain.Reports;

namespace CiroApp.Application.Services;

public sealed class DayService(ICiroRepository repository)
{
    public Task<IReadOnlyList<CiroDay>> GetMonthAsync(int year, int month) =>
        repository.GetRangeAsync(new DateOnly(year, month, 1), new DateOnly(year, month, DateTime.DaysInMonth(year, month)));

    public Task<IReadOnlyList<CiroDay>> GetRangeAsync(ReportRange range) => repository.GetRangeAsync(range.From, range.To);

    public Task<CiroDay?> GetAsync(DateOnly tarih) => repository.GetAsync(tarih);

    public Task SaveAsync(CiroDay day) => repository.SaveAsync(day);

    public Task DeleteAsync(DateOnly tarih) => repository.DeleteAsync(tarih);
}
