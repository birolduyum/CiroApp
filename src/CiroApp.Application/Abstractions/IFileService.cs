namespace CiroApp.Application.Abstractions;

public interface IFileService
{
    bool CanShare { get; }
    Task<string?> SaveAsync(string suggestedName, byte[] data);
    Task<byte[]?> PickFileAsync();
    Task ShareAsync(string fileName, byte[] data);

    /// <summary>Kaydedilmiş dosyayı cihazdaki ilişkili uygulamayla (Excel gibi) açar.</summary>
    Task OpenAsync(string path);
}
