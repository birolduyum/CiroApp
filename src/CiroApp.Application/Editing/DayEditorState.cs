using CiroApp.Domain.Models;

namespace CiroApp.Application.Editing;

/// <summary>Günlük giriş ve havale formlarının ortak taslağı; Kaydet'e kadar veritabanına dokunmaz.</summary>
public sealed class DayEditorState
{
    private readonly int _id;
    private readonly List<HavaleEntry> _havaleler;

    public DayEditorState(DateOnly tarih, CiroDay? existing)
    {
        Tarih = tarih;
        _id = existing?.Id ?? 0;
        Nakit = existing?.Nakit ?? 0;
        KrediKarti = existing?.KrediKarti ?? 0;
        _havaleler = existing?.Havaleler
            .Select(h => new HavaleEntry { Id = h.Id, Tutar = h.Tutar, KisiAdi = h.KisiAdi })
            .ToList() ?? [];
    }

    public DateOnly Tarih { get; }
    public decimal Nakit { get; set; }
    public decimal KrediKarti { get; set; }
    public IReadOnlyList<HavaleEntry> Havaleler => _havaleler;
    public decimal HavaleToplam => _havaleler.Sum(h => h.Tutar);
    public decimal GunlukToplam => Nakit + KrediKarti + HavaleToplam;

    public void AddHavale(decimal tutar, string kisiAdi) =>
        _havaleler.Add(new HavaleEntry { Tutar = tutar, KisiAdi = kisiAdi.Trim() });

    public void UpdateHavale(int index, decimal tutar, string kisiAdi)
    {
        _havaleler[index].Tutar = tutar;
        _havaleler[index].KisiAdi = kisiAdi.Trim();
    }

    public void RemoveHavale(int index) => _havaleler.RemoveAt(index);

    public IReadOnlyList<string> Validate()
    {
        var hatalar = new List<string>();
        if (Nakit < 0) hatalar.Add("Nakit tutarı negatif olamaz.");
        if (KrediKarti < 0) hatalar.Add("Kredi kartı tutarı negatif olamaz.");
        for (var i = 0; i < _havaleler.Count; i++)
        {
            if (_havaleler[i].Tutar < 0) hatalar.Add($"{i + 1}. havale tutarı negatif olamaz.");
            if (string.IsNullOrWhiteSpace(_havaleler[i].KisiAdi)) hatalar.Add($"{i + 1}. havale için kişi adı girilmelidir.");
        }

        return hatalar;
    }

    public CiroDay ToDay() => new()
    {
        Id = _id,
        Tarih = Tarih,
        Nakit = Nakit,
        KrediKarti = KrediKarti,
        Havaleler = _havaleler.Select(h => new HavaleEntry { Id = h.Id, Tutar = h.Tutar, KisiAdi = h.KisiAdi }).ToList()
    };
}
