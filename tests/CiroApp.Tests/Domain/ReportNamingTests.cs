using CiroApp.Domain.Reports;

namespace CiroApp.Tests.Domain;

public class ReportNamingTests
{
    private static ReportRange Aralik(int y1, int m1, int d1, int y2, int m2, int d2) =>
        new(new DateOnly(y1, m1, d1), new DateOnly(y2, m2, d2));

    [Fact]
    public void TamAy_BaslikVeDosyaAdiAyAdiylaOlusur()
    {
        var r = Aralik(2026, 10, 1, 2026, 10, 31);

        Assert.True(r.IsFullMonth);
        Assert.Equal("2026 Ekim", ReportNaming.Title(r));
        Assert.Equal("2026 Ekim Cirosu.xlsx", ReportNaming.FileName(r));
    }

    [Fact]
    public void TamAyDegilse_BaslikVeDosyaAdiTarihAraligiOlur()
    {
        var r = Aralik(2026, 10, 15, 2026, 11, 10);

        Assert.False(r.IsFullMonth);
        Assert.Equal("15.10.2026 - 10.11.2026", ReportNaming.Title(r));
        Assert.Equal("15.10.2026 - 10.11.2026 Cirosu.xlsx", ReportNaming.FileName(r));
    }

    [Fact]
    public void Subat2028_ArtikYildaYirmiDokuzGunVerir()
    {
        Assert.Equal(29, Aralik(2028, 2, 1, 2028, 2, 29).Days().Count());
    }

    [Fact]
    public void YilGecisi_DogruGunSayisiniVerir()
    {
        Assert.Equal(27, Aralik(2026, 12, 15, 2027, 1, 10).Days().Count());
    }

    [Fact]
    public void BaslangicBitistenSonraysa_Reddedilir()
    {
        Assert.Throws<ArgumentException>(() => Aralik(2026, 10, 10, 2026, 10, 9));
    }
}
