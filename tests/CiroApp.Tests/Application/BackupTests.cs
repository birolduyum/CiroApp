using CiroApp.Application.Backup;
using CiroApp.Domain.Models;
using CiroApp.Infrastructure.Data;

namespace CiroApp.Tests.Application;

public sealed class BackupTests : IAsyncLifetime
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"ciro-{Guid.NewGuid():N}.db3");
    private CiroRepository _repo = null!;
    private BackupService _service = null!;

    public Task InitializeAsync()
    {
        _repo = new CiroRepository(_dbPath);
        _service = new BackupService(_repo);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _repo.DisposeAsync();
        foreach (var f in new[] { _dbPath, _dbPath + "-wal", _dbPath + "-shm" })
        {
            if (File.Exists(f)) File.Delete(f);
        }
    }

    private static CiroDay Gun(int gun, decimal nakit, params (decimal, string)[] havaleler)
    {
        var d = new CiroDay { Tarih = new DateOnly(2026, 10, gun), Nakit = nakit, KrediKarti = 1 };
        foreach (var (t, a) in havaleler) d.Havaleler.Add(new HavaleEntry { Tutar = t, KisiAdi = a });
        return d;
    }

    [Fact]
    public async Task ExportThenParse_GunleriVeHavaleleriAynenGeriVerir()
    {
        await _repo.SaveAsync(Gun(1, 10, (10, "Ahmet As"), (12, "Veli Ak")));

        var data = await _service.ExportAsync();
        var file = BackupSerializer.Parse(data);

        var gun = Assert.Single(file.Days);
        Assert.Equal(new DateOnly(2026, 10, 1), gun.Tarih);
        Assert.Equal(10, gun.Nakit);
        Assert.Equal(["Ahmet As", "Veli Ak"], gun.Havaleler.Select(h => h.KisiAdi));
    }

    [Theory]
    [InlineData("rastgele metin")]
    [InlineData("{\"version\":99,\"days\":[]}")]
    [InlineData("{\"days\":[]}")]
    [InlineData("")]
    public void Parse_BozukVeyaBaskaSurumDosyayiReddeder(string icerik)
    {
        Assert.Throws<BackupFormatException>(() => BackupSerializer.Parse(System.Text.Encoding.UTF8.GetBytes(icerik)));
    }

    [Theory]
    [InlineData("{\"version\":1,\"days\":[null]}")]
    [InlineData("{\"version\":1,\"days\":[{\"tarih\":\"2026-10-01\",\"nakit\":1,\"krediKarti\":1,\"havaleler\":[null]}]}")]
    [InlineData("{\"version\":1,\"days\":[{\"tarih\":null,\"nakit\":1,\"krediKarti\":1,\"havaleler\":[]}]}")]
    public void Parse_IcindeNullOlanDosyayiCokmedenReddeder(string icerik)
    {
        Assert.Throws<BackupFormatException>(() => BackupSerializer.Parse(System.Text.Encoding.UTF8.GetBytes(icerik)));
    }

    [Theory]
    [InlineData("{\"version\":1,\"days\":[{\"tarih\":\"2026-10-01\",\"nakit\":-5,\"krediKarti\":1,\"havaleler\":[]}]}")]
    [InlineData("{\"version\":1,\"days\":[{\"tarih\":\"2026-10-01\",\"nakit\":1,\"krediKarti\":1,\"havaleler\":[{\"tutar\":-1,\"kisiAdi\":\"A\"}]}]}")]
    [InlineData("{\"version\":1,\"days\":[{\"tarih\":\"2026-10-01\",\"nakit\":1,\"krediKarti\":1,\"havaleler\":[]},{\"tarih\":\"2026-10-01\",\"nakit\":2,\"krediKarti\":2,\"havaleler\":[]}]}")]
    public void Parse_NegatifTutarVeyaAyniTarihiReddeder(string icerik)
    {
        Assert.Throws<BackupFormatException>(() => BackupSerializer.Parse(System.Text.Encoding.UTF8.GetBytes(icerik)));
    }

    [Fact]
    public async Task ApplyImport_ortadaHataOlursaYeniGunlerDeYazilmaz()
    {
        var bozuk = Gun(3, 3);
        bozuk.Havaleler.Add(null!);
        var plan = new ImportPlan([Gun(1, 1), Gun(2, 2), bozuk], []);

        await Assert.ThrowsAnyAsync<Exception>(() => _service.ApplyImportAsync(plan, _ => true));

        Assert.Empty(await _repo.GetAllAsync());
    }

    [Fact]
    public void Parse_RastgeleBaytlariReddeder()
    {
        Assert.Throws<BackupFormatException>(() => BackupSerializer.Parse([0xFF, 0x00, 0x13, 0x37]));
    }

    [Fact]
    public void Plan_FarkliNakitCakismaOlur_DosyadaOlupYerelde_OlmayanEklenir_AyniOlanSayilmaz()
    {
        var local = new[] { Gun(1, 10), Gun(2, 5, (3, "A"), (4, "B")) };
        var file = new BackupFile([Gun(1, 99), Gun(2, 5, (4, "B"), (3, "A")), Gun(3, 7)]);

        var plan = ImportPlanner.Plan(local, file);

        var cakisma = Assert.Single(plan.Conflicts);
        Assert.Equal(99, cakisma.Imported.Nakit);
        Assert.Equal(10, cakisma.Local.Nakit);
        Assert.Equal([new DateOnly(2026, 10, 3)], plan.ToAdd.Select(g => g.Tarih));
    }

    [Fact]
    public async Task ApplyImport_CakismadaKullaniciSecimineGoreDegistirirVeYerelOnlyGuneDokunmaz()
    {
        await _repo.SaveAsync(Gun(1, 10, (10, "Ahmet As")));
        await _repo.SaveAsync(Gun(5, 50));   // yalnız cihazda
        var file = new BackupFile([Gun(1, 99, (1, "Yeni Kisi")), Gun(3, 7)]);
        var plan = ImportPlanner.Plan(await _repo.GetAllAsync(), file);

        await _service.ApplyImportAsync(plan, _ => true);

        var g1 = (await _repo.GetAsync(new DateOnly(2026, 10, 1)))!;
        Assert.Equal(99, g1.Nakit);
        Assert.Equal(["Yeni Kisi"], g1.Havaleler.Select(h => h.KisiAdi));
        Assert.NotNull(await _repo.GetAsync(new DateOnly(2026, 10, 3)));
        Assert.Equal(50, (await _repo.GetAsync(new DateOnly(2026, 10, 5)))!.Nakit);
    }

    [Fact]
    public async Task ApplyImport_YerelKoruSecilirseGunDegismez()
    {
        await _repo.SaveAsync(Gun(1, 10, (10, "Ahmet As")));
        var plan = ImportPlanner.Plan(await _repo.GetAllAsync(), new BackupFile([Gun(1, 99)]));

        await _service.ApplyImportAsync(plan, _ => false);

        var g1 = (await _repo.GetAsync(new DateOnly(2026, 10, 1)))!;
        Assert.Equal(10, g1.Nakit);
        Assert.Equal(["Ahmet As"], g1.Havaleler.Select(h => h.KisiAdi));
    }
}
