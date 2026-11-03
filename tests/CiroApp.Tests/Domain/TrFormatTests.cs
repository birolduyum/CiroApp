using CiroApp.Domain.Formatting;

namespace CiroApp.Tests.Domain;

public class TrFormatTests
{
    [Fact]
    public void Tarih_GunAyYilBicimindeYazilir()
    {
        Assert.Equal("1.10.2026", TrFormat.Tarih(new DateOnly(2026, 10, 1)));
    }

    [Fact]
    public void YilAy_TurkceAyAdiVerir()
    {
        Assert.Equal("2026 Ekim", TrFormat.YilAy(new DateOnly(2026, 10, 15)));
    }

    [Theory]
    [InlineData("12,50", 12.50)]
    [InlineData("1.250,75", 1250.75)]
    [InlineData("10", 10)]
    [InlineData("0", 0)]
    public void TryParseTutar_GecerliGirisleriOkur(string girdi, double beklenen)
    {
        Assert.True(TrFormat.TryParseTutar(girdi, out var tutar));
        Assert.Equal((decimal)beklenen, tutar);
    }

    [Theory]
    [InlineData("12.50")]
    [InlineData("-5")]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("1,234")]
    public void TryParseTutar_BelirsizVeyaGecersizGirisiReddeder(string girdi)
    {
        Assert.False(TrFormat.TryParseTutar(girdi, out _));
    }
}
