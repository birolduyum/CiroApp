namespace CiroApp.Domain.Models;

public sealed class CiroDay
{
    public int Id { get; set; }
    public DateOnly Tarih { get; set; }
    public decimal Nakit { get; set; }
    public decimal KrediKarti { get; set; }
    public List<HavaleEntry> Havaleler { get; set; } = [];

    public decimal HavaleToplam => Havaleler.Sum(h => h.Tutar);
    public decimal GunlukToplam => Nakit + KrediKarti + HavaleToplam;
}
