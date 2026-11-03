using CiroApp.Domain.Models;

namespace CiroApp.Application.Abstractions;

public interface ICiroRepository
{
    Task<CiroDay?> GetAsync(DateOnly tarih);
    Task<IReadOnlyList<CiroDay>> GetRangeAsync(DateOnly from, DateOnly to);
    Task<IReadOnlyList<CiroDay>> GetAllAsync();
    Task SaveAsync(CiroDay day);

    /// <summary>Hepsini tek işlemde kaydeder; biri başarısız olursa hiçbiri yazılmaz.</summary>
    Task SaveManyAsync(IReadOnlyList<CiroDay> days);
    Task DeleteAsync(DateOnly tarih);
}
