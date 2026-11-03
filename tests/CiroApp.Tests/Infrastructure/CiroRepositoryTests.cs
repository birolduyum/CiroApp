using CiroApp.Domain.Models;
using CiroApp.Infrastructure.Data;

namespace CiroApp.Tests.Infrastructure;

public sealed class CiroRepositoryTests : IAsyncLifetime
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"ciro-{Guid.NewGuid():N}.db3");

    private readonly List<IAsyncDisposable> _acik = [];

    private T Ac<T>(T repo) where T : IAsyncDisposable
    {
        _acik.Add(repo);
        return repo;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        foreach (var r in _acik) await r.DisposeAsync();
        foreach (var f in new[] { _dbPath, _dbPath + "-wal", _dbPath + "-shm" })
        {
            if (File.Exists(f)) File.Delete(f);
        }
    }

    private static CiroDay Gun(DateOnly t, decimal nakit, params (decimal, string)[] havaleler)
    {
        var d = new CiroDay { Tarih = t, Nakit = nakit, KrediKarti = 1 };
        foreach (var (tutar, ad) in havaleler) d.Havaleler.Add(new HavaleEntry { Tutar = tutar, KisiAdi = ad });
        return d;
    }

    [Fact]
    public async Task SaveAsync_AyniTarihIkinciKezKaydedilinceTekSatirKalirVeHavaleleriDegisir()
    {
        var repo = Ac(new CiroRepository(_dbPath));
        var t = new DateOnly(2026, 10, 1);

        await repo.SaveAsync(Gun(t, 10, (10, "Ahmet As")));
        await repo.SaveAsync(Gun(t, 20, (5, "Veli Ak"), (6, "Saim Bir")));

        var hepsi = await repo.GetAllAsync();
        var gun = Assert.Single(hepsi);
        Assert.Equal(20, gun.Nakit);
        Assert.Equal(["Veli Ak", "Saim Bir"], gun.Havaleler.Select(h => h.KisiAdi));
        Assert.Equal(11, gun.HavaleToplam);
    }

    [Fact]
    public async Task SaveManyAsync_HepsiniKaydeder()
    {
        var repo = Ac(new CiroRepository(_dbPath));

        await repo.SaveManyAsync([Gun(new DateOnly(2026, 10, 1), 1, (5, "A")), Gun(new DateOnly(2026, 10, 2), 2)]);

        Assert.Equal([new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 2)], (await repo.GetAllAsync()).Select(g => g.Tarih));
    }

    [Fact]
    public async Task SaveManyAsync_ortadaHataOlursaHicbirSeyYazmazVeEskiVeriKorunur()
    {
        var repo = Ac(new CiroRepository(_dbPath));
        var mevcut = new DateOnly(2026, 10, 1);
        await repo.SaveAsync(Gun(mevcut, 10, (10, "Eski")));
        var bozuk = Gun(new DateOnly(2026, 10, 3), 3);
        bozuk.Havaleler.Add(null!);

        await Assert.ThrowsAnyAsync<Exception>(() => repo.SaveManyAsync(
            [Gun(mevcut, 99), Gun(new DateOnly(2026, 10, 2), 2), bozuk]));

        var hepsi = await repo.GetAllAsync();
        var gun = Assert.Single(hepsi);
        Assert.Equal(10, gun.Nakit);
        Assert.Equal(["Eski"], gun.Havaleler.Select(h => h.KisiAdi));
    }

    [Fact]
    public async Task DeleteAsync_GunuVeHavalelerini_Siler()
    {
        var repo = Ac(new CiroRepository(_dbPath));
        var t = new DateOnly(2026, 10, 1);
        await repo.SaveAsync(Gun(t, 10, (10, "Ahmet As")));

        await repo.DeleteAsync(t);

        Assert.Null(await repo.GetAsync(t));
        Assert.Empty(await repo.GetAllAsync());
        // Yeniden aynı tarih eklenince eski havale gelmemeli
        await repo.SaveAsync(Gun(t, 1));
        Assert.Empty((await repo.GetAsync(t))!.Havaleler);
    }

    [Fact]
    public async Task GetRangeAsync_AralikDisindakileriGetirmezVeTariheGoreSiralar()
    {
        var repo = Ac(new CiroRepository(_dbPath));
        await repo.SaveAsync(Gun(new DateOnly(2026, 11, 1), 1));
        await repo.SaveAsync(Gun(new DateOnly(2026, 10, 15), 2));
        await repo.SaveAsync(Gun(new DateOnly(2026, 10, 1), 3));
        await repo.SaveAsync(Gun(new DateOnly(2026, 9, 30), 4));

        var gunler = await repo.GetRangeAsync(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31));

        Assert.Equal([new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 15)], gunler.Select(g => g.Tarih));
    }

    [Fact]
    public async Task GetRangeAsync_BaslangicVeBitisAyniGunseSadeceOGunuGetirir()
    {
        var repo = Ac(new CiroRepository(_dbPath));
        await repo.SaveAsync(Gun(new DateOnly(2026, 10, 31), 1));
        await repo.SaveAsync(Gun(new DateOnly(2026, 11, 1), 1));
        await repo.SaveAsync(Gun(new DateOnly(2026, 11, 2), 1));

        var sonuc = await repo.GetRangeAsync(new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 1));

        Assert.Equal([new DateOnly(2026, 11, 1)], sonuc.Select(g => g.Tarih));
    }

    [Fact]
    public async Task GetRangeAsync_AyinSonGunuSecilinceAyinTamaminiGetirirSonrakiAyiGetirmez()
    {
        var repo = Ac(new CiroRepository(_dbPath));
        await repo.SaveAsync(Gun(new DateOnly(2026, 11, 1), 1));
        await repo.SaveAsync(Gun(new DateOnly(2026, 11, 30), 2));
        await repo.SaveAsync(Gun(new DateOnly(2026, 12, 1), 3));

        var sonuc = await repo.GetRangeAsync(new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 30));

        Assert.Equal([new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 30)], sonuc.Select(g => g.Tarih));
    }

    [Fact]
    public async Task RecipientRepository_AyniAdresiBuyukKucukHarfDuyarsizTekilleştirir()
    {
        var repo = Ac(new RecipientRepository(_dbPath));

        await repo.AddIfMissingAsync("A@x.com");
        await repo.AddIfMissingAsync("a@x.com");
        await repo.AddIfMissingAsync("b@x.com");

        Assert.Equal(["A@x.com", "b@x.com"], await repo.GetAllAsync());
        await repo.RemoveAsync("a@X.com");
        Assert.Equal(["b@x.com"], await repo.GetAllAsync());
    }
}
