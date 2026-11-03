using CiroApp.Domain.Models;

namespace CiroApp.Application.Backup;

public sealed record BackupFile(IReadOnlyList<CiroDay> Days);

public sealed record ImportConflict(CiroDay Local, CiroDay Imported);

public sealed record ImportPlan(IReadOnlyList<CiroDay> ToAdd, IReadOnlyList<ImportConflict> Conflicts);

public sealed class BackupFormatException(string message, Exception? inner = null) : Exception(message, inner);
