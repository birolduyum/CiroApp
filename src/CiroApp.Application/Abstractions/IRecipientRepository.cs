namespace CiroApp.Application.Abstractions;

public interface IRecipientRepository
{
    Task<IReadOnlyList<string>> GetAllAsync();
    Task AddIfMissingAsync(string email);
    Task RemoveAsync(string email);
}
