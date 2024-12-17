namespace FreeRealmsLocaleTools.IdHashing;

/// <summary>
/// Port of Bob Jenkins' lookup2 algorithm. Source: <see href="https://burtleburtle.net/bob/c/lookup2.c"/>
/// </summary>
public static class JenkinsLookup2
{
    /// <summary>
    /// Hashes a variable-length key into a 32-bit value.
    /// </summary>
    public static uint Hash(string key)
    {
        uint length = (uint)key.Length;

        if (length == 0u) return 0u;

        uint a = 0x9e3779b9, b = 0x9e3779b9, c = 0u, len = length;
        int p = 0;

        while (len >= 12)
        {
            a += key[p + 0] + ((uint)key[p + 1] << 8) + ((uint)key[p + 2] << 16) + ((uint)key[p + 3] << 24);
            b += key[p + 4] + ((uint)key[p + 5] << 8) + ((uint)key[p + 6] << 16) + ((uint)key[p + 7] << 24);
            c += key[p + 8] + ((uint)key[p + 9] << 8) + ((uint)key[p + 10] << 16) + ((uint)key[p + 11] << 24);
            Mix(ref a, ref b, ref c);
            p += 12;
            len -= 12;
        }

        c += length;

        if (len >= 11) c += (uint)key[p + 10] << 24;
        if (len >= 10) c += (uint)key[p + 9] << 16;
        if (len >= 9) c += (uint)key[p + 8] << 8;
        if (len >= 8) b += (uint)key[p + 7] << 24;
        if (len >= 7) b += (uint)key[p + 6] << 16;
        if (len >= 6) b += (uint)key[p + 5] << 8;
        if (len >= 5) b += key[p + 4];
        if (len >= 4) a += (uint)key[p + 3] << 24;
        if (len >= 3) a += (uint)key[p + 2] << 16;
        if (len >= 2) a += (uint)key[p + 1] << 8;
        if (len >= 1) a += key[p];

        Mix(ref a, ref b, ref c);

        return c;
    }

    /// <summary>
    /// Mixes three 32-bit values reversibly.
    /// </summary>
    private static void Mix(ref uint a, ref uint b, ref uint c)
    {
        a -= b; a -= c; a ^= c >> 13;
        b -= c; b -= a; b ^= a << 8;
        c -= a; c -= b; c ^= b >> 13;
        a -= b; a -= c; a ^= c >> 12;
        b -= c; b -= a; b ^= a << 16;
        c -= a; c -= b; c ^= b >> 5;
        a -= b; a -= c; a ^= c >> 3;
        b -= c; b -= a; b ^= a << 10;
        c -= a; c -= b; c ^= b >> 15;
    }
}
