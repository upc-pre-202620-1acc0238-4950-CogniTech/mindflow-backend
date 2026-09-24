using System.Security.Cryptography;
using System.Text;
using Mindflow_backend.Journal.Application.Services;

namespace Mindflow_backend.Journal.Infrastructure.Services;

/// <summary>
///     Computes a deterministic, keyed (HMAC-SHA256) hash for a single search token.
///     Keying it (vs. plain SHA-256) prevents an attacker with DB access from recovering
///     common words via a precomputed dictionary of hashes.
/// </summary>
public sealed class SearchTokenHasher : ISearchTokenHasher
{
    private readonly byte[] _key;

    public SearchTokenHasher(string base64Key)
    {
        _key = Convert.FromBase64String(base64Key);
        if (_key.Length < 32)
            throw new ArgumentException("Search index key must be at least 256 bits (32 bytes) encoded as Base64.");
    }

    public string Hash(string token)
    {
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = HMACSHA256.HashData(_key, bytes);
        return Convert.ToBase64String(hash);
    }

    public static string GenerateKey()
    {
        var key = new byte[32];
        RandomNumberGenerator.Fill(key);
        return Convert.ToBase64String(key);
    }
}
