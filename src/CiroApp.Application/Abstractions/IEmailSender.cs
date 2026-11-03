using CiroApp.Application.Email;

namespace CiroApp.Application.Abstractions;

public interface IEmailSender
{
    Task SendReportAsync(SmtpSettings settings, IReadOnlyList<string> to, string subject, string fileName, byte[] xlsx);
    Task SendTestAsync(SmtpSettings settings);
}
