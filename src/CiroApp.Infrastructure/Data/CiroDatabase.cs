using SQLite;

namespace CiroApp.Infrastructure.Data;

internal static class CiroDatabase
{
    public static SQLiteAsyncConnection Open(string dbPath)
    {
        var db = new SQLiteAsyncConnection(dbPath);
        db.CreateTableAsync<DayRow>().GetAwaiter().GetResult();
        db.CreateTableAsync<HavaleRow>().GetAwaiter().GetResult();
        db.CreateTableAsync<RecipientRow>().GetAwaiter().GetResult();
        return db;
    }
}

internal sealed class DayRow
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    [Indexed(Unique = true)] public string Tarih { get; set; } = "";
    public decimal Nakit { get; set; }
    public decimal KrediKarti { get; set; }
}

internal sealed class HavaleRow
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    [Indexed] public int DayId { get; set; }
    public decimal Tutar { get; set; }
    public string KisiAdi { get; set; } = "";
}

internal sealed class RecipientRow
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    [Indexed(Unique = true)] public string EmailKey { get; set; } = "";
    public string Email { get; set; } = "";
}
