using CiroApp.Application.Editing;
using CiroApp.Domain.Models;

namespace CiroApp.Tests.Application;

public class DayEditorStateTests
{
    private static readonly DateOnly Gun1 = new(2026, 10, 1);

    private static CiroDay Mevcut()
    {
        var d = new CiroDay { Id = 7, Tarih = Gun1, Nakit = 10, KrediKarti = 10 };
        d.Havaleler.Add(new HavaleEntry { Id = 1, Tutar = 10, KisiAdi = "Ahmet As" });
        return d;
    }

    [Fact]
    public void MevcutGunKopyalanir_YeniGunBostanBaslar()
    {
        var state = new DayEditorState(Gun1, Mevcut());
        Assert.Equal(10, state.Nakit);
        Assert.Equal(10, state.HavaleToplam);
        Assert.Equal(30, state.GunlukToplam);

        var yeni = new DayEditorState(Gun1, null);
        Assert.Equal(0, yeni.GunlukToplam);
        Assert.Empty(yeni.Havaleler);
    }

    [Fact]
    public void HavaleEklemeVeSilme_HavaleToplaminiGuncellerVeMevcutNesneyeDokunmaz()
    {
        var mevcut = Mevcut();
        var state = new DayEditorState(Gun1, mevcut);

        state.AddHavale(12, "Veli Ak");
        Assert.Equal(22, state.HavaleToplam);
        state.UpdateHavale(0, 20, "Ahmet As");
        Assert.Equal(32, state.HavaleToplam);
        state.RemoveHavale(0);
        Assert.Equal(12, state.HavaleToplam);

        Assert.Single(mevcut.Havaleler);                 // İptal = taslağı atmak: orijinal değişmedi
        Assert.Equal(10, mevcut.Havaleler[0].Tutar);
    }

    [Fact]
    public void Validate_NegatifTutarVeBosKisiAdiIcinTurkceHataVerir()
    {
        var state = new DayEditorState(Gun1, null) { Nakit = -1 };
        state.AddHavale(5, "  ");

        var hatalar = state.Validate();

        Assert.Contains(hatalar, h => h.Contains("Nakit"));
        Assert.Contains(hatalar, h => h.Contains("kişi adı"));
    }

    [Fact]
    public void Validate_GecerliGunHataVermez_ToDayIdyiKorur()
    {
        var state = new DayEditorState(Gun1, Mevcut());
        state.KrediKarti = 5;

        Assert.Empty(state.Validate());
        var gun = state.ToDay();
        Assert.Equal(7, gun.Id);
        Assert.Equal(Gun1, gun.Tarih);
        Assert.Equal(5, gun.KrediKarti);
        Assert.Equal(["Ahmet As"], gun.Havaleler.Select(h => h.KisiAdi));
    }

    [Fact]
    public void AddHavale_KisiAdiniKirpar()
    {
        var state = new DayEditorState(Gun1, null);
        state.AddHavale(5, "  Ahmet As ");
        Assert.Equal("Ahmet As", state.Havaleler[0].KisiAdi);
    }
}
