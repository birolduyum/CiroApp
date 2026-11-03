using CiroApp.Domain.Models;

namespace CiroApp.Application.Backup;

public static class ImportPlanner
{
    public static ImportPlan Plan(IReadOnlyList<CiroDay> local, BackupFile file)
    {
        var localByDate = local.ToDictionary(d => d.Tarih);
        var toAdd = new List<CiroDay>();
        var conflicts = new List<ImportConflict>();

        foreach (var imported in file.Days)
        {
            if (!localByDate.TryGetValue(imported.Tarih, out var existing))
            {
                toAdd.Add(imported);
            }
            else if (!SameContent(existing, imported))
            {
                conflicts.Add(new ImportConflict(existing, imported));
            }
        }

        return new ImportPlan(toAdd, conflicts);
    }

    private static bool SameContent(CiroDay a, CiroDay b) =>
        a.Nakit == b.Nakit
        && a.KrediKarti == b.KrediKarti
        && Havaleler(a).SequenceEqual(Havaleler(b));

    private static IEnumerable<(decimal, string)> Havaleler(CiroDay d) =>
        d.Havaleler.Select(h => (h.Tutar, h.KisiAdi)).OrderBy(h => h.Tutar).ThenBy(h => h.KisiAdi, StringComparer.Ordinal);
}
