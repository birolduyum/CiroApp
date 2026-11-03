using CiroApp.Domain.Models;

namespace CiroApp.Tests.Domain;

public class CiroDayTests
{
    [Fact]
    public void GunlukToplam_NakitKartVeHavaleToplaminiTopla()
    {
        var gun = new CiroDay
        {
            Tarih = new DateOnly(2026, 10, 1),
            Nakit = 10,
            KrediKarti = 10,
            Havaleler = { new HavaleEntry { Tutar = 10, KisiAdi = "Ahmet As" } }
        };

        Assert.Equal(10, gun.HavaleToplam);
        Assert.Equal(30, gun.GunlukToplam);
    }

    [Fact]
    public void HavaleToplam_HavaleYoksaSifirdir()
    {
        Assert.Equal(0, new CiroDay().HavaleToplam);
    }
}
