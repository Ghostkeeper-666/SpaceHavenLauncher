using System;
using System.Security.Cryptography;
using System.Text;

namespace SH.Modding;

public static class ModAutoId
{
    /// <summary>
    /// This mod variable name is reserved and will be used as placeholder for MOD ID within the mod
    /// </summary>
    public static string IdVariable { get; } = "id";
    public static string BracedIdVariable { get; } = '{' + IdVariable + '}';

    /// <summary>
    /// Reserves the 4 least significant decimal digits for the mod (minor ID)
    /// </summary>
    public static int MinorIdDigits { get; } = 4;

    /// <summary>
    /// Lower bound (inclusive) for the auto-generated major ID
    /// </summary>
    public static int MinValue { get; } = 7;

    /// <summary>
    /// Range hash-derived IDs
    /// </summary>
    public static int Range { get; } = 214740;

    /// <summary>
    /// Upper bound (inclusive) for the auto-generated major ID
    /// </summary>
    public static int MaxValue => MinValue + Range;

    /// <summary>
    /// Computes the deterministic major ID number within the range [MinValue, MaxValue] from a mod's name
    /// </summary>
    /// <param name="modName">The unique mod name</param>
    /// <returns>The mod hash ID</returns>
    public static int ComputeMajorId(string modName)
    {
        // Compute hash using SHA256 for better distribution:
        byte[] data = SHA256.HashData(Encoding.UTF8.GetBytes(modName));

        // Combine into less bytes:
        uint value =
            (uint)(data[0] << 24) |
            (uint)(data[1] << 16) |
            (uint)(data[2] << 8) |
            data[3];

        // Restrict max generated hash value:
        int majorId = (int)(value % Range);

        // Reserves the first 70.000 IDs for BugByte:
        majorId += MinValue;

        // Done.
        return Math.Clamp(majorId, MinValue, MaxValue);
    }
}
