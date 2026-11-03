using CiroApp.Application.Abstractions;
using CiroApp.Application.Email;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace CiroApp.Infrastructure.Email;

public sealed class MailKitEmailSender : IEmailSender
{
    private const string XlsxMime = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public async Task SendReportAsync(SmtpSettings settings, IReadOnlyList<string> to, string subject, string fileName, byte[] xlsx)
    {
        if (to.Count == 0)
        {
            throw new ArgumentException("En az bir alıcı gerekir.", nameof(to));
        }

        var message = CreateOrThrow(settings, to, subject);
        var body = new BodyBuilder { TextBody = $"{subject} ekte.\n" };
        body.Attachments.Add(fileName, xlsx, ContentType.Parse(XlsxMime));
        message.Body = body.ToMessageBody();
        await SendAsync(settings, message);
    }

    public Task SendTestAsync(SmtpSettings settings)
    {
        var message = CreateOrThrow(settings, [settings.Email], "Ciro uygulaması test e-postası");
        message.Body = new TextPart("plain") { Text = "E-posta ayarları çalışıyor." };
        return SendAsync(settings, message);
    }

    private static MimeMessage CreateOrThrow(SmtpSettings s, IReadOnlyList<string> to, string subject)
    {
        try
        {
            return Create(s, to, subject);
        }
        catch (ParseException ex)
        {
            throw new EmailSendException("E-posta adresi geçersiz. Ayarlardaki gönderen adresini ve alıcıları kontrol edin.", ex);
        }
    }

    private static MimeMessage Create(SmtpSettings s, IReadOnlyList<string> to, string subject)
    {
        var message = new MimeMessage { Subject = subject };
        message.From.Add(MailboxAddress.Parse(s.Email));
        foreach (var address in to) message.To.Add(MailboxAddress.Parse(address));
        return message;
    }

    // Sertifika zinciri ve sunucu adı her durumda doğrulanır; yalnızca kullanıcı açıkça isterse iptal (OCSP/CRL)
    // sorgusu atlanır (Android'de "Chain_RevocationStatusUnknown" hatası için).
    internal static SmtpClient CreateClient(SmtpSettings s) =>
        new() { Timeout = 20_000, CheckCertificateRevocation = !s.SkipRevocationCheck };

    private static async Task SendAsync(SmtpSettings s, MimeMessage message)
    {
        if (string.IsNullOrWhiteSpace(s.Host))
        {
            throw new EmailSendException("Sunucu adresi boş. Ayarlar'dan e-posta sunucusunu girin.");
        }

        using var client = CreateClient(s);
        try
        {
            // STARTTLS zorunlu: sunucu sunmuyorsa parola düz metin gitmesin diye bağlantı reddedilir.
            var secure = s.UseSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;
            await client.ConnectAsync(s.Host, s.Port, secure);
            await client.AuthenticateAsync(s.Email, s.Password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
        catch (AuthenticationException ex)
        {
            var ipucu = s.Provider == "Gmail" ? " Gmail için normal parola yerine 'uygulama parolası' kullanmalısınız." : "";
            throw new EmailSendException("E-posta hesabına giriş yapılamadı: kullanıcı adı veya parola hatalı." + ipucu, ex);
        }
        catch (Exception ex) when (ex is not ArgumentException)
        {
            throw new EmailSendException($"E-posta gönderilemedi: {ex.Message}", ex);
        }
    }
}
