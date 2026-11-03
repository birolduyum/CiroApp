using CiroApp.Application.Abstractions;
using CiroApp.Application.Backup;
using CiroApp.Application.Services;
using CiroApp.Infrastructure.Data;
using CiroApp.Infrastructure.Email;
using CiroApp.Infrastructure.Reports;
using CiroApp.Platform;
using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using MudBlazor.Services;

namespace CiroApp;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkit()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
			});

		builder.Services.AddMauiBlazorWebView();
		builder.Services.AddMudServices();

		// Infrastructure (portların gerçeklemeleri)
		var dbPath = Path.Combine(FileSystem.AppDataDirectory, "ciro.db3");
		builder.Services.AddSingleton<ICiroRepository>(_ => new CiroRepository(dbPath));
		builder.Services.AddSingleton<IRecipientRepository>(_ => new RecipientRepository(dbPath));
		builder.Services.AddSingleton<IReportBuilder, ExcelReportBuilder>();
		builder.Services.AddSingleton<IEmailSender, MailKitEmailSender>();

		// Platform
		builder.Services.AddSingleton<ISettingsStore, SecureSettingsStore>();
		builder.Services.AddSingleton<IFileService, FileService>();

		// Application (kullanım senaryoları)
		builder.Services.AddSingleton<DayService>();
		builder.Services.AddSingleton<ReportService>();
		builder.Services.AddSingleton<EmailReportService>();
		builder.Services.AddSingleton<BackupService>();
		builder.Services.AddSingleton<EmailSettingsService>();
		builder.Services.AddSingleton<RecipientService>();

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
