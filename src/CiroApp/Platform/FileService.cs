using CiroApp.Application.Abstractions;
using CommunityToolkit.Maui.Storage;

namespace CiroApp.Platform;

public sealed class FileService : IFileService
{
    public bool CanShare => DeviceInfo.Platform == DevicePlatform.Android;

    public async Task<string?> SaveAsync(string suggestedName, byte[] data)
    {
        using var stream = new MemoryStream(data);
        var result = await FileSaver.Default.SaveAsync(suggestedName, stream, CancellationToken.None);
        if (result.IsSuccessful)
        {
            return result.FilePath;
        }

        // Kullanıcı vazgeçtiyse sessizce null; gerçek bir hata (izin, depolama) kullanıcıya bildirilsin.
        if (result.Exception is not null && result.Exception is not OperationCanceledException)
        {
            throw new IOException($"Dosya kaydedilemedi: {result.Exception.Message}", result.Exception);
        }

        return null;
    }

    public async Task<byte[]?> PickFileAsync()
    {
        var file = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = "Yedek dosyasını seçin" });
        if (file is null)
        {
            return null;
        }

        using var source = await file.OpenReadAsync();
        using var buffer = new MemoryStream();
        await source.CopyToAsync(buffer);
        return buffer.ToArray();
    }

    public async Task OpenAsync(string path)
    {
#if ANDROID
        // Android'de kaydetme sonucu çoğunlukla content:// adresidir; dosya yolu gibi açılamaz.
        if (path.StartsWith("content:", StringComparison.Ordinal))
        {
            var intent = new Android.Content.Intent(Android.Content.Intent.ActionView);
            intent.SetDataAndType(Android.Net.Uri.Parse(path)!, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            intent.AddFlags(Android.Content.ActivityFlags.GrantReadUriPermission | Android.Content.ActivityFlags.NewTask);
            Microsoft.Maui.ApplicationModel.Platform.AppContext.StartActivity(intent);
            return;
        }
#endif
        await Launcher.Default.OpenAsync(new OpenFileRequest(Path.GetFileName(path), new ReadOnlyFile(path)));
    }

    public async Task ShareAsync(string fileName, byte[] data)
    {
        var path = Path.Combine(FileSystem.CacheDirectory, fileName);
        await File.WriteAllBytesAsync(path, data);
        await Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = fileName,
            File = new ShareFile(path)
        });
    }
}
