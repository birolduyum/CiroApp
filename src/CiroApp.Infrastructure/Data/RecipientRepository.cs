using CiroApp.Application.Abstractions;
using SQLite;

namespace CiroApp.Infrastructure.Data;

public sealed class RecipientRepository : IRecipientRepository, IAsyncDisposable
{
    private readonly SQLiteAsyncConnection _db;

    public RecipientRepository(string dbPath) => _db = CiroDatabase.Open(dbPath);

    public ValueTask DisposeAsync() => new(_db.CloseAsync());

    private static string Key(string email) => email.Trim().ToLowerInvariant();

    public async Task<IReadOnlyList<string>> GetAllAsync() =>
        (await _db.Table<RecipientRow>().OrderBy(r => r.Id).ToListAsync()).Select(r => r.Email).ToList();

    public async Task AddIfMissingAsync(string email)
    {
        var key = Key(email);
        if (await _db.Table<RecipientRow>().Where(r => r.EmailKey == key).CountAsync() > 0) return;
        await _db.InsertAsync(new RecipientRow { EmailKey = key, Email = email.Trim() });
    }

    public Task RemoveAsync(string email) =>
        _db.ExecuteAsync("DELETE FROM RecipientRow WHERE EmailKey = ?", Key(email));
}
