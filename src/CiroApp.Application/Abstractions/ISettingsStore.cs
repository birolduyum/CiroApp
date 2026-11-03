using CiroApp.Application.Email;

namespace CiroApp.Application.Abstractions;

public interface ISettingsStore
{
    Task<SmtpSettings?> LoadAsync();
    Task SaveAsync(SmtpSettings settings);
}
