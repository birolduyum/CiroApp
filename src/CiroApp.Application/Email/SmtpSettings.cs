namespace CiroApp.Application.Email;

/// <param name="SkipRevocationCheck">Sertifika iptal sorgusunu atlar; varsayılan kapalı (denetim açık). Yalnızca cihaz iptal sunucusuna ulaşamayıp bağlantıyı reddediyorsa kullanıcı açar.</param>
public sealed record SmtpSettings(string Provider, string Host, int Port, bool UseSsl, string Email, string Password, bool SkipRevocationCheck = false);

public static class SmtpPresets
{
    public static readonly string[] Providers = ["Gmail", "Outlook", "Diğer"];

    public static SmtpSettings For(string provider, string email, string password) => provider switch
    {
        "Gmail" => new SmtpSettings(provider, "smtp.gmail.com", 465, true, email, password),
        "Outlook" => new SmtpSettings(provider, "smtp-mail.outlook.com", 587, false, email, password),
        _ => new SmtpSettings("Diğer", "", 587, false, email, password)
    };
}
