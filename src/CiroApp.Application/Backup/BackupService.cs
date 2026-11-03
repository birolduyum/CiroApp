using CiroApp.Application.Abstractions;

namespace CiroApp.Application.Backup;

public sealed class BackupService(ICiroRepository repository)
{
    public async Task<byte[]> ExportAsync() => BackupSerializer.Serialize(await repository.GetAllAsync());

    public async Task<ImportPlan> PlanImportAsync(byte[] data) =>
        ImportPlanner.Plan(await repository.GetAllAsync(), BackupSerializer.Parse(data));

    // Tek işlem: yarıda hata olursa hiçbir gün yazılmaz.
    public Task ApplyImportAsync(ImportPlan plan, Func<ImportConflict, bool> useImported) =>
        repository.SaveManyAsync(plan.ToAdd.Concat(plan.Conflicts.Where(useImported).Select(c => c.Imported)).ToList());
}
