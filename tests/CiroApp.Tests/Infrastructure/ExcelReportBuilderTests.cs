using CiroApp.Domain.Models;
using CiroApp.Domain.Reports;
using CiroApp.Infrastructure.Reports;
using ClosedXML.Excel;

namespace CiroApp.Tests.Infrastructure;

public class ExcelReportBuilderTests
{
    private static readonly ReportRange Ekim = new(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31));

    private static CiroDay Gun(int gun, decimal nakit, decimal kart, params (decimal Tutar, string Ad)[] havaleler)
    {
        var d = new CiroDay { Tarih = new DateOnly(2026, 10, gun), Nakit = nakit, KrediKarti = kart };
        foreach (var h in havaleler) d.Havaleler.Add(new HavaleEntry { Tutar = h.Tutar, KisiAdi = h.Ad });
        return d;
    }

    private static XLWorkbook Uret(ReportRange range, params CiroDay[] gunler)
    {
        var bytes = new ExcelReportBuilder().Build(range, gunler);
        return new XLWorkbook(new MemoryStream(bytes));
    }

    [Fact]
    public void CiroSayfasi_OrnektekiDuzenVeToplamlariUretir()
    {
        using var wb = Uret(Ekim, Gun(1, 10, 10, (10, "Ahmet As")), Gun(2, 5, 12, (11, "Veli Ak")));
        var ws = wb.Worksheet("Ciro");

        Assert.Equal("2026 Ekim", ws.Cell("A1").GetString());
        Assert.Equal(["Tarih", "Nakit", "Kredi kartı", "Havale", "Günlük Toplam"],
            Enumerable.Range(1, 5).Select(c => ws.Cell(2, c).GetString()));
        Assert.Equal(new DateTime(2026, 10, 1), ws.Cell("A3").GetDateTime());
        Assert.Equal("d.M.yyyy", ws.Cell("A3").Style.DateFormat.Format);
        Assert.Equal(10, ws.Cell("B3").GetDouble());
        Assert.Equal(30, ws.Cell("E3").GetDouble());
        Assert.Equal(28, ws.Cell("E4").GetDouble());
        Assert.True(ws.Cell("B5").IsEmpty());                       // verisi olmayan gün: yalnız tarih
        Assert.Equal(new DateTime(2026, 10, 31), ws.Cell("A33").GetDateTime());
        Assert.Equal("Genel Toplam", ws.Cell("A34").GetString());
        Assert.Equal([15d, 22d, 21d, 58d], Enumerable.Range(2, 4).Select(c => ws.Cell(34, c).GetDouble()));
    }

    [Fact]
    public void HavaleSayfasi_GunBaslarinaGoreGruplarVeGunlukToplamYazar()
    {
        using var wb = Uret(Ekim,
            Gun(1, 0, 0, (10, "Ahmet As"), (12, "Veli Ak")),
            Gun(2, 0, 0, (10, "Veli Veri"), (12, "Saim Bir"), (12, "Veli Ak")),
            Gun(3, 0, 0, (15, "Ahmet As"), (26, "Veli Ak"), (36, "Saim Bir")));
        var ws = wb.Worksheet("Havale-EFT");

        Assert.Equal("2026 Ekim", ws.Cell("A1").GetString());
        Assert.Equal("Tarih", ws.Cell("A2").GetString());
        Assert.Equal("Havale", ws.Cell("B2").GetString());
        Assert.Equal("Müşteri", ws.Cell("C2").GetString());
        Assert.Equal(new DateTime(2026, 10, 1), ws.Cell("A3").GetDateTime());
        Assert.Equal(10, ws.Cell("B3").GetDouble());
        Assert.Equal("Ahmet As", ws.Cell("C3").GetString());
        Assert.True(ws.Cell("A4").IsEmpty());
        Assert.Equal("Veli Ak", ws.Cell("C4").GetString());
        Assert.Equal("Günlük Toplam", ws.Cell("C5").GetString());
        Assert.Equal(22, ws.Cell("D5").GetDouble());
        Assert.Equal(new DateTime(2026, 10, 2), ws.Cell("A6").GetDateTime());
        Assert.Equal(34, ws.Cell("D9").GetDouble());
        Assert.Equal(new DateTime(2026, 10, 3), ws.Cell("A10").GetDateTime());
        Assert.Equal(77, ws.Cell("D13").GetDouble());
        Assert.Equal(new DateTime(2026, 10, 4), ws.Cell("A14").GetDateTime());   // havalesiz gün: yalnız tarih
        Assert.True(ws.Cell("C14").IsEmpty());
        Assert.Equal("Genel Toplam", ws.Cell("A42").GetString());
        Assert.Equal(133, ws.Cell("B42").GetDouble());
    }

    [Theory]
    [InlineData("=1+1")]
    [InlineData("+cmd|' /C calc'!A0")]
    [InlineData("@SUM(1)")]
    public void KisiAdi_FormulOlarakYorumlanmazDuzMetinKalir(string ad)
    {
        using var wb = Uret(Ekim, Gun(1, 0, 0, (5, ad)));
        var hucre = wb.Worksheet("Havale-EFT").Cell("C3");

        Assert.False(hucre.HasFormula);
        Assert.Equal(XLDataType.Text, hucre.DataType);
        Assert.Equal(ad, hucre.GetString());
    }

    [Fact]
    public void VerisizAralik_IkiSayfaliGecerliDosyaUretir()
    {
        var kasim = new ReportRange(new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 30));
        using var wb = Uret(kasim);

        Assert.Equal(["Ciro", "Havale-EFT"], wb.Worksheets.Select(w => w.Name));
        Assert.Equal("Genel Toplam", wb.Worksheet("Ciro").Cell("A33").GetString());
        Assert.Equal(0, wb.Worksheet("Ciro").Cell("E33").GetDouble());
    }

    [Fact]
    public void YilGecenAralik_BasligaTarihAraligiYazarVeTumGunleriIcerir()
    {
        var aralik = new ReportRange(new DateOnly(2026, 12, 15), new DateOnly(2027, 1, 10));
        using var wb = Uret(aralik);
        var ws = wb.Worksheet("Ciro");

        Assert.Equal("15.12.2026 - 10.1.2027", ws.Cell("A1").GetString());
        Assert.Equal(new DateTime(2027, 1, 10), ws.Cell("A29").GetDateTime());   // 3 + 27 - 1
        Assert.Equal("Genel Toplam", ws.Cell("A30").GetString());
    }
}
