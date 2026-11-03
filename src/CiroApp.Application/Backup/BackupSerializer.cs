using System.Globalization;
using System.Text.Json;
using CiroApp.Domain.Models;

namespace CiroApp.Application.Backup;

public static class BackupSerializer
{
    private const int CurrentVersion = 1;
    private const string TarihBicimi = "yyyy-MM-dd";

    private sealed record HavaleDto(decimal Tutar, string KisiAdi);
    private sealed record DayDto(string Tarih, decimal Nakit, decimal KrediKarti, List<HavaleDto> Havaleler);
    private sealed record FileDto(int? Version, List<DayDto>? Days);

    public static byte[] Serialize(IReadOnlyList<CiroDay> days)
    {
        var dto = new FileDto(CurrentVersion, days.Select(d => new DayDto(
            d.Tarih.ToString(TarihBicimi, CultureInfo.InvariantCulture),
            d.Nakit,
            d.KrediKarti,
            d.Havaleler.Select(h => new HavaleDto(h.Tutar, h.KisiAdi)).ToList())).ToList());
        return JsonSerializer.SerializeToUtf8Bytes(dto, new JsonSerializerOptions { WriteIndented = true });
    }

    public static BackupFile Parse(byte[] data)
    {
        try
        {
            var dto = JsonSerializer.Deserialize<FileDto>(data, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (dto is null || dto.Version != CurrentVersion || dto.Days is null)
            {
                throw new BackupFormatException("Yedek dosyası tanınmadı veya farklı bir sürümden.");
            }

            var days = dto.Days.Select(d => new CiroDay
            {
                Tarih = DateOnly.ParseExact(d.Tarih, TarihBicimi, CultureInfo.InvariantCulture),
                Nakit = d.Nakit,
                KrediKarti = d.KrediKarti,
                Havaleler = (d.Havaleler ?? []).Select(h => new HavaleEntry { Tutar = h.Tutar, KisiAdi = h.KisiAdi ?? "" }).ToList()
            }).ToList();
            var gecersiz = days.Any(d => d.Nakit < 0 || d.KrediKarti < 0 || d.Havaleler.Any(h => h.Tutar < 0));
            if (gecersiz || days.Select(d => d.Tarih).Distinct().Count() != days.Count)
            {
                throw new BackupFormatException("Yedek dosyasında negatif tutar veya aynı güne ait birden fazla kayıt var.");
            }

            return new BackupFile(days);
        }
        catch (Exception ex) when (ex is JsonException or FormatException or ArgumentException or NullReferenceException or InvalidOperationException)
        {
            throw new BackupFormatException("Dosya okunamadı.", ex);
        }
    }
}
