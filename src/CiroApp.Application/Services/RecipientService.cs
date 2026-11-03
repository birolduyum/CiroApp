using System.Net.Mail;
using CiroApp.Application.Abstractions;

namespace CiroApp.Application.Services;

public sealed class RecipientService(IRecipientRepository recipients)
{
    public Task<IReadOnlyList<string>> GetAllAsync() => recipients.GetAllAsync();

    public Task RemoveAsync(string address) => recipients.RemoveAsync(address);

    /// <summary>Adresi kırpar; "Ad &lt;a@x.com&gt;" gibi biçimleri ve geçersizleri reddeder.</summary>
    public static bool TryNormalize(string? input, out string address)
    {
        var trimmed = input?.Trim() ?? "";
        if (MailAddress.TryCreate(trimmed, out var parsed) && parsed.Address == trimmed)
        {
            address = trimmed;
            return true;
        }

        address = "";
        return false;
    }
}
