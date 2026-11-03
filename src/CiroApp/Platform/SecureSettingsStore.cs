using CiroApp.Application.Abstractions;
using CiroApp.Application.Email;

namespace CiroApp.Platform;

/// <summary>SMTP ayarları: parola SecureStorage'da, diğer alanlar Preferences'ta tutulur.</summary>
public sealed class SecureSettingsStore : ISettingsStore
{
    private const string PasswordKey = "smtp.password";

    public async Task<SmtpSettings?> LoadAsync()
    {
        var email = Preferences.Default.Get("smtp.email", "");
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        string password;
        try
        {
            password = await SecureStorage.Default.GetAsync(PasswordKey) ?? "";
        }
        catch (Exception)
        {
            // Telefon yedeğinden geri yükleme sonrası anahtar kaybolabilir; parola yeniden girilsin.
            password = "";
        }

        return new SmtpSettings(
            Preferences.Default.Get("smtp.provider", "Gmail"),
            Preferences.Default.Get("smtp.host", ""),
            Preferences.Default.Get("smtp.port", 465),
            Preferences.Default.Get("smtp.ssl", true),
            email,
            password,
            Preferences.Default.Get("smtp.skiprevocation", false));
    }

    public async Task SaveAsync(SmtpSettings settings)
    {
        Preferences.Default.Set("smtp.provider", settings.Provider);
        Preferences.Default.Set("smtp.host", settings.Host);
        Preferences.Default.Set("smtp.port", settings.Port);
        Preferences.Default.Set("smtp.ssl", settings.UseSsl);
        Preferences.Default.Set("smtp.email", settings.Email);
        Preferences.Default.Set("smtp.skiprevocation", settings.SkipRevocationCheck);
        await SecureStorage.Default.SetAsync(PasswordKey, settings.Password);
    }
}
