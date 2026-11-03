using CiroApp.Application.Email;
using CiroApp.Infrastructure.Email;

namespace CiroApp.Tests.Infrastructure;

public class MailKitEmailSenderTests
{
    private static readonly SmtpSettings Gecersiz = new("Diğer", "gecersiz-host.invalid", 587, false, "a@x.com", "pw");

    [Fact]
    public void CreateClient_VarsayilanOlarakSertifikaIptalDenetimiAcik()
    {
        using var client = MailKitEmailSender.CreateClient(Gecersiz);

        Assert.True(client.CheckCertificateRevocation);
    }

    [Fact]
    public void CreateClient_YalnizcaAyarIsaretliyseIptalDenetimiKapanir()
    {
        using var client = MailKitEmailSender.CreateClient(Gecersiz with { SkipRevocationCheck = true });

        Assert.False(client.CheckCertificateRevocation);
    }

    [Fact]
    public async Task SendReportAsync_BosAliciListesiniReddeder()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            new MailKitEmailSender().SendReportAsync(Gecersiz, [], "konu", "d.xlsx", [1]));
    }

    [Fact]
    public async Task SendTestAsync_UlasilamayanSunucudaTurkceEmailSendExceptionVerir()
    {
        var ex = await Assert.ThrowsAsync<EmailSendException>(() => new MailKitEmailSender().SendTestAsync(Gecersiz));
        Assert.False(string.IsNullOrWhiteSpace(ex.Message));
    }

    [Fact]
    public async Task SendTestAsync_BosSunucuIcinEmailSendExceptionVerir()
    {
        var bos = Gecersiz with { Host = "" };
        await Assert.ThrowsAsync<EmailSendException>(() => new MailKitEmailSender().SendTestAsync(bos));
    }

    [Fact]
    public async Task SendTestAsync_BozukGonderenAdresiIcinEmailSendExceptionVerir()
    {
        var bozuk = Gecersiz with { Email = "gecersiz adres" };
        await Assert.ThrowsAsync<EmailSendException>(() => new MailKitEmailSender().SendTestAsync(bozuk));
    }
}
