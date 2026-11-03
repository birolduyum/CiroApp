using CiroApp.Application.Abstractions;
using CiroApp.Application.Email;
using CiroApp.Application.Services;
using CiroApp.Domain.Models;
using CiroApp.Domain.Reports;

namespace CiroApp.Tests.Application;

public class ServiceTests
{
    private sealed class FakeRepo : ICiroRepository
    {
        public List<CiroDay> Days { get; } = [];
        public Task<CiroDay?> GetAsync(DateOnly t) => Task.FromResult(Days.FirstOrDefault(d => d.Tarih == t));
        public Task<IReadOnlyList<CiroDay>> GetRangeAsync(DateOnly a, DateOnly b) =>
            Task.FromResult<IReadOnlyList<CiroDay>>(Days.Where(d => d.Tarih >= a && d.Tarih <= b).OrderBy(d => d.Tarih).ToList());
        public Task<IReadOnlyList<CiroDay>> GetAllAsync() => Task.FromResult<IReadOnlyList<CiroDay>>(Days);
        public Task SaveAsync(CiroDay day) { Days.RemoveAll(d => d.Tarih == day.Tarih); Days.Add(day); return Task.CompletedTask; }
        public async Task SaveManyAsync(IReadOnlyList<CiroDay> days) { foreach (var d in days) await SaveAsync(d); }
        public Task DeleteAsync(DateOnly t) { Days.RemoveAll(d => d.Tarih == t); return Task.CompletedTask; }
    }

    private sealed class FakeBuilder : IReportBuilder
    {
        public ReportRange? Son;
        public byte[] Build(ReportRange range, IReadOnlyList<CiroDay> days) { Son = range; return [1, 2, 3]; }
    }

    private sealed class FakeRecipients : IRecipientRepository
    {
        public List<string> Liste { get; } = [];
        public Task<IReadOnlyList<string>> GetAllAsync() => Task.FromResult<IReadOnlyList<string>>(Liste);
        public Task AddIfMissingAsync(string email) { if (!Liste.Contains(email, StringComparer.OrdinalIgnoreCase)) Liste.Add(email); return Task.CompletedTask; }
        public Task RemoveAsync(string email) { Liste.RemoveAll(e => e.Equals(email, StringComparison.OrdinalIgnoreCase)); return Task.CompletedTask; }
    }

    private sealed class FakeSettings(SmtpSettings? s) : ISettingsStore
    {
        public Task<SmtpSettings?> LoadAsync() => Task.FromResult(s);
        public Task SaveAsync(SmtpSettings settings) => Task.CompletedTask;
    }

    private sealed class FakeSender(bool fail = false) : IEmailSender
    {
        public List<(IReadOnlyList<string> To, string FileName, byte[] Data)> Gonderilen { get; } = [];
        public Task SendReportAsync(SmtpSettings s, IReadOnlyList<string> to, string subject, string fileName, byte[] xlsx)
        {
            if (fail) throw new EmailSendException("Gönderilemedi.");
            Gonderilen.Add((to, fileName, xlsx));
            return Task.CompletedTask;
        }
        public Task SendTestAsync(SmtpSettings s) => Task.CompletedTask;
    }

    private static readonly SmtpSettings Ayar = SmtpPresets.For("Gmail", "ben@gmail.com", "pw");
    private static readonly ReportRange Ekim = new(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31));

    [Fact]
    public void Presets_GmailVeOutlookSunucuBilgileriniDoldurur()
    {
        var g = SmtpPresets.For("Gmail", "a@gmail.com", "pw");
        Assert.Equal(("smtp.gmail.com", 465, true), (g.Host, g.Port, g.UseSsl));
        var o = SmtpPresets.For("Outlook", "a@outlook.com", "pw");
        Assert.Equal(("smtp-mail.outlook.com", 587, false), (o.Host, o.Port, o.UseSsl));
        Assert.Equal("", SmtpPresets.For("Diğer", "a@x.com", "pw").Host);
    }

    [Fact]
    public async Task DayService_AyiTarihSiralıGetirir()
    {
        var repo = new FakeRepo();
        var svc = new DayService(repo);
        await svc.SaveAsync(new CiroDay { Tarih = new DateOnly(2026, 10, 15) });
        await svc.SaveAsync(new CiroDay { Tarih = new DateOnly(2026, 11, 1) });
        await svc.SaveAsync(new CiroDay { Tarih = new DateOnly(2026, 10, 2) });

        var ay = await svc.GetMonthAsync(2026, 10);

        Assert.Equal([new DateOnly(2026, 10, 2), new DateOnly(2026, 10, 15)], ay.Select(d => d.Tarih));
    }

    [Fact]
    public async Task DayService_AraligiUclarDahilTarihSiralıGetirir()
    {
        var repo = new FakeRepo();
        var svc = new DayService(repo);
        foreach (var gun in new[] { 20, 5, 10, 25 })
        {
            await svc.SaveAsync(new CiroDay { Tarih = new DateOnly(2026, 10, gun) });
        }

        var sonuc = await svc.GetRangeAsync(new ReportRange(new DateOnly(2026, 10, 5), new DateOnly(2026, 10, 20)));

        Assert.Equal([5, 10, 20], sonuc.Select(d => d.Tarih.Day));
    }

    [Fact]
    public async Task ReportService_DosyaAdiVeIcerigiUretir()
    {
        var builder = new FakeBuilder();
        var dosya = await new ReportService(new FakeRepo(), builder).BuildAsync(Ekim);

        Assert.Equal("2026 Ekim Cirosu.xlsx", dosya.FileName);
        Assert.Equal([1, 2, 3], dosya.Content);
        Assert.Equal(Ekim, builder.Son);
    }

    [Fact]
    public async Task EmailReportService_AyarYoksaEmailSettingsMissingExceptionFirlatir()
    {
        var svc = new EmailReportService(new ReportService(new FakeRepo(), new FakeBuilder()), new FakeSender(), new FakeSettings(null), new FakeRecipients());
        await Assert.ThrowsAsync<EmailSettingsMissingException>(() => svc.SendAsync(Ekim, ["x@y.com"]));
    }

    [Fact]
    public async Task EmailReportService_BasariliGonderimdeYeniAliciyiKaydeder()
    {
        var sender = new FakeSender();
        var alicilar = new FakeRecipients();
        var svc = new EmailReportService(new ReportService(new FakeRepo(), new FakeBuilder()), sender, new FakeSettings(Ayar), alicilar);

        await svc.SendAsync(Ekim, ["a@x.com", "b@x.com"]);

        Assert.Equal("2026 Ekim Cirosu.xlsx", sender.Gonderilen.Single().FileName);
        Assert.Equal(["a@x.com", "b@x.com"], alicilar.Liste);
    }

    [Fact]
    public async Task EmailReportService_GonderimBasarisizsaAliciyiKaydetmez()
    {
        var alicilar = new FakeRecipients();
        var svc = new EmailReportService(new ReportService(new FakeRepo(), new FakeBuilder()), new FakeSender(fail: true), new FakeSettings(Ayar), alicilar);

        await Assert.ThrowsAsync<EmailSendException>(() => svc.SendAsync(Ekim, ["a@x.com"]));

        Assert.Empty(alicilar.Liste);
    }
}
