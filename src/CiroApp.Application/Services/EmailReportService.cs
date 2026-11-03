using CiroApp.Application.Abstractions;
using CiroApp.Application.Email;
using CiroApp.Domain.Reports;

namespace CiroApp.Application.Services;

public sealed class EmailReportService(
    ReportService reports,
    IEmailSender sender,
    ISettingsStore settings,
    IRecipientRepository recipients)
{
    public async Task SendAsync(ReportRange range, IReadOnlyList<string> to)
    {
        var smtp = await settings.LoadAsync() ?? throw new EmailSettingsMissingException();
        var file = await reports.BuildAsync(range);

        await sender.SendReportAsync(smtp, to, ReportNaming.Title(range) + " Cirosu", file.FileName, file.Content);

        foreach (var address in to)
        {
            await recipients.AddIfMissingAsync(address);
        }
    }
}
