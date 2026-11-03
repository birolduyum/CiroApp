using CiroApp.Application.Abstractions;
using CiroApp.Application.Email;
using CiroApp.Application.Services;

namespace CiroApp.Tests.Application;

public class SettingsAndRecipientServiceTests
{
    private sealed class MemSettings : ISettingsStore
    {
        public SmtpSettings? Kayitli;
        public Task<SmtpSettings?> LoadAsync() => Task.FromResult(Kayitli);
        public Task SaveAsync(SmtpSettings settings) { Kayitli = settings; return Task.CompletedTask; }
    }

    private sealed class CountingSender : IEmailSender
    {
        public int TestSayisi;
        public Task SendReportAsync(SmtpSettings s, IReadOnlyList<string> to, string subject, string fileName, byte[] xlsx) => Task.CompletedTask;
        public Task SendTestAsync(SmtpSettings s) { TestSayisi++; return Task.CompletedTask; }
    }

    private sealed class MemRecipients : IRecipientRepository
    {
        public List<string> Liste { get; } = ["a@x.com"];
        public Task<IReadOnlyList<string>> GetAllAsync() => Task.FromResult<IReadOnlyList<string>>(Liste);
        public Task AddIfMissingAsync(string email) { Liste.Add(email); return Task.CompletedTask; }
        public Task RemoveAsync(string email) { Liste.Remove(email); return Task.CompletedTask; }
    }

    private static readonly SmtpSettings Gecerli = SmtpPresets.For("Gmail", "ben@gmail.com", "pw");

    [Fact]
    public async Task Kaydet_GecerliAyariSaklarVeHataDonmez()
    {
        var store = new MemSettings();
        var hata = await new EmailSettingsService(store, new CountingSender()).SaveAsync(Gecerli);

        Assert.Null(hata);
        Assert.Equal(Gecerli, store.Kayitli);
    }

    [Theory]
    [InlineData("", "smtp.gmail.com")]
    [InlineData("ben@gmail.com", "")]
    public async Task Kaydet_BosAdresVeyaSunucuyuReddederVeSaklamaz(string email, string host)
    {
        var store = new MemSettings();
        var ayar = Gecerli with { Email = email, Host = host };

        var hata = await new EmailSettingsService(store, new CountingSender()).SaveAsync(ayar);

        Assert.NotNull(hata);
        Assert.Null(store.Kayitli);
    }

    [Fact]
    public async Task Test_GecersizAyardaGondermezHataDoner()
    {
        var sender = new CountingSender();
        var hata = await new EmailSettingsService(new MemSettings(), sender).SendTestAsync(Gecerli with { Host = "" });

        Assert.NotNull(hata);
        Assert.Equal(0, sender.TestSayisi);
    }

    [Fact]
    public async Task Test_GecerliAyardaGonderirHataDonmez()
    {
        var sender = new CountingSender();
        var hata = await new EmailSettingsService(new MemSettings(), sender).SendTestAsync(Gecerli);

        Assert.Null(hata);
        Assert.Equal(1, sender.TestSayisi);
    }

    [Theory]
    [InlineData("  a@x.com ", true, "a@x.com")]
    [InlineData("Ad <a@x.com>", false, "")]
    [InlineData("yanlis", false, "")]
    [InlineData("", false, "")]
    public void TryNormalize_AdresiDogrularVeKirpar(string girdi, bool beklenen, string adres)
    {
        Assert.Equal(beklenen, RecipientService.TryNormalize(girdi, out var sonuc));
        Assert.Equal(adres, sonuc);
    }

    [Fact]
    public async Task RecipientService_ListeleVeSiler()
    {
        var svc = new RecipientService(new MemRecipients());

        Assert.Equal(["a@x.com"], await svc.GetAllAsync());
        await svc.RemoveAsync("a@x.com");
        Assert.Empty(await svc.GetAllAsync());
    }
}
