using System.Globalization;
using System.Text.RegularExpressions;

namespace CiroApp.Domain.Formatting;

public static partial class TrFormat
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

    private static readonly string[] AyAdlari =
    [
        "Ocak", "Şubat", "Mart", "Nisan", "Mayıs", "Haziran",
        "Temmuz", "Ağustos", "Eylül", "Ekim", "Kasım", "Aralık"
    ];

    public static string Tarih(DateOnly tarih) => tarih.ToString("d.M.yyyy", Tr);

    public static string AyAdi(int ay) => AyAdlari[ay - 1];

    public static string YilAy(DateOnly tarih) => $"{tarih.Year} {AyAdi(tarih.Month)}";

    // Kabul: 12 | 12,5 | 12,50 | 1.250,75 | 1250,75. Nokta yalnızca binlik ayracıdır;
    // "12.50" gibi belirsiz girişler 100 kat yanlış okunmasın diye reddedilir.
    public static bool TryParseTutar(string? girdi, out decimal tutar)
    {
        tutar = 0;
        if (string.IsNullOrWhiteSpace(girdi))
        {
            return false;
        }

        var metin = girdi.Trim();
        if (!TutarDeseni().IsMatch(metin))
        {
            return false;
        }

        return decimal.TryParse(metin, NumberStyles.AllowThousands | NumberStyles.AllowDecimalPoint, Tr, out tutar);
    }

    [GeneratedRegex(@"^(\d{1,3}(\.\d{3})+|\d+)(,\d{1,2})?$")]
    private static partial Regex TutarDeseni();
}
