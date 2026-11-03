namespace CiroApp.Domain.Models;

public sealed class HavaleEntry
{
    public int Id { get; set; }
    public decimal Tutar { get; set; }
    public string KisiAdi { get; set; } = string.Empty;
}
