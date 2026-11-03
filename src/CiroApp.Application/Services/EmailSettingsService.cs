using CiroApp.Application.Abstractions;
using CiroApp.Application.Email;

namespace CiroApp.Application.Services;

/// <summary>E-posta hesabı ayarları: doğrulama kuralları burada, ekranlarda değil. Dönen metin kullanıcıya gösterilecek hatadır; null = başarılı.</summary>
public sealed class EmailSettingsService(ISettingsStore store, IEmailSender sender)
{
    public Task<SmtpSettings?> LoadAsync() => store.LoadAsync();

    public async Task<string?> SaveAsync(SmtpSettings settings)
    {
        var hata = Validate(settings);
        if (hata is not null)
        {
            return hata;
        }

        await store.SaveAsync(settings);
        return null;
    }

    public async Task<string?> SendTestAsync(SmtpSettings settings)
    {
        var hata = Validate(settings);
        if (hata is not null)
        {
            return hata;
        }

        await sender.SendTestAsync(settings);
        return null;
    }

    private static string? Validate(SmtpSettings s) =>
        string.IsNullOrWhiteSpace(s.Email) || string.IsNullOrWhiteSpace(s.Host)
            ? "E-posta adresi ve sunucu boş olamaz."
            : null;
}
