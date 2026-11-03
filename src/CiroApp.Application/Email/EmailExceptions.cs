namespace CiroApp.Application.Email;

public sealed class EmailSendException(string message, Exception? inner = null) : Exception(message, inner);

public sealed class EmailSettingsMissingException()
    : Exception("Önce Ayarlar'dan e-posta hesabını girin.");
