using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace Kentico.Xperience.Labs.AutomationExtensions.Conditions.RandomSplit;

/// <summary>
/// Calculates stable contact buckets for random split conditions.
/// </summary>
internal static class RandomSplitBucket
{
    internal const int BUCKET_COUNT = 100;


    internal static void ValidateConfiguration(int percentage, string? splitKey)
    {
        if (percentage is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(percentage), percentage, "Percentage must be between 0 and 100, inclusive.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(splitKey);
    }


    internal static bool IsInYesBranch(Guid contactGuid, string? splitKey, int percentage)
    {
        ValidateConfiguration(percentage, splitKey);
        return GetBucket(contactGuid, splitKey) < percentage;
    }


    internal static int GetBucket(Guid contactGuid, string? splitKey)
    {
        if (contactGuid == Guid.Empty)
        {
            throw new ArgumentException("Contact GUID must not be empty.", nameof(contactGuid));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(splitKey);

        string canonicalInput = $"{contactGuid:D}:{splitKey}";
        byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes(canonicalInput));
        uint value = BinaryPrimitives.ReadUInt32BigEndian(digest);

        return (int)(value % BUCKET_COUNT);
    }
}
