using System.Globalization;
using CiroApp.Application.Abstractions;
using CiroApp.Domain.Models;
using SQLite;

namespace CiroApp.Infrastructure.Data;

public sealed class CiroRepository : ICiroRepository, IAsyncDisposable
{
    private const string TarihBicimi = "yyyy-MM-dd";
    private readonly SQLiteAsyncConnection _db;

    public CiroRepository(string dbPath) => _db = CiroDatabase.Open(dbPath);

    public ValueTask DisposeAsync() => new(_db.CloseAsync());

    private static string Key(DateOnly t) => t.ToString(TarihBicimi, CultureInfo.InvariantCulture);

    public async Task<CiroDay?> GetAsync(DateOnly tarih)
    {
        var key = Key(tarih);
        var row = await _db.Table<DayRow>().Where(d => d.Tarih == key).FirstOrDefaultAsync();
        return row is null ? null : await ToDayAsync(row);
    }

    public async Task<IReadOnlyList<CiroDay>> GetRangeAsync(DateOnly from, DateOnly to)
    {
        // Yarı açık aralık: bitiş günü dahil olsun diye bitiş bir gün artırılır ve "<" ile karşılaştırılır.
        var (a, b) = (Key(from), Key(to.AddDays(1)));
        var rows = await _db.QueryAsync<DayRow>(
            "SELECT * FROM DayRow WHERE Tarih >= ? AND Tarih < ? ORDER BY Tarih", a, b);
        return await ToDaysAsync(rows);
    }

    public async Task<IReadOnlyList<CiroDay>> GetAllAsync()
    {
        var rows = await _db.QueryAsync<DayRow>("SELECT * FROM DayRow ORDER BY Tarih");
        return await ToDaysAsync(rows);
    }

    public Task SaveAsync(CiroDay day) => _db.RunInTransactionAsync(conn => Upsert(conn, day));

    public Task SaveManyAsync(IReadOnlyList<CiroDay> days) => _db.RunInTransactionAsync(conn =>
    {
        foreach (var day in days)
        {
            Upsert(conn, day);
        }
    });

    private static void Upsert(SQLiteConnection conn, CiroDay day)
    {
        var key = Key(day.Tarih);
        var row = conn.Table<DayRow>().Where(d => d.Tarih == key).FirstOrDefault();
        if (row is null)
        {
            row = new DayRow { Tarih = key };
            row.Nakit = day.Nakit;
            row.KrediKarti = day.KrediKarti;
            conn.Insert(row);
        }
        else
        {
            row.Nakit = day.Nakit;
            row.KrediKarti = day.KrediKarti;
            conn.Update(row);
            conn.Execute("DELETE FROM HavaleRow WHERE DayId = ?", row.Id);
        }

        foreach (var h in day.Havaleler)
        {
            conn.Insert(new HavaleRow { DayId = row.Id, Tutar = h.Tutar, KisiAdi = h.KisiAdi });
        }
    }

    public Task DeleteAsync(DateOnly tarih) => _db.RunInTransactionAsync(conn =>
    {
        var key = Key(tarih);
        var row = conn.Table<DayRow>().Where(d => d.Tarih == key).FirstOrDefault();
        if (row is null) return;
        conn.Execute("DELETE FROM HavaleRow WHERE DayId = ?", row.Id);
        conn.Delete(row);
    });

    private async Task<IReadOnlyList<CiroDay>> ToDaysAsync(List<DayRow> rows)
    {
        var days = new List<CiroDay>(rows.Count);
        foreach (var r in rows) days.Add(await ToDayAsync(r));
        return days;
    }

    private async Task<CiroDay> ToDayAsync(DayRow r)
    {
        var havaleler = await _db.Table<HavaleRow>().Where(h => h.DayId == r.Id).OrderBy(h => h.Id).ToListAsync();
        return new CiroDay
        {
            Id = r.Id,
            Tarih = DateOnly.ParseExact(r.Tarih, TarihBicimi, CultureInfo.InvariantCulture),
            Nakit = r.Nakit,
            KrediKarti = r.KrediKarti,
            Havaleler = havaleler.Select(h => new HavaleEntry { Id = h.Id, Tutar = h.Tutar, KisiAdi = h.KisiAdi }).ToList()
        };
    }
}
