namespace FreeRealmsLocaleTools.LocaleParser;

/// <summary>
/// Represents a locale entry location in a Free Realms .dir file.
/// </summary>
/// <param name="Hash">The 32-bit unsigned integer hash of the locale entry.</param>
/// <param name="Offset">The position of the locale entry in the .dat file.</param>
/// <param name="Size">The number of bytes in the locale entry.</param>
public record LocaleEntryLocation(uint Hash, int Offset, int Size)
{
    /// <summary>
    /// Initializes a new instance of <see cref="LocaleEntryLocation"/> by parsing the given .dir file line.
    /// </summary>
    /// <param name="line">The locale .dir file line to parse.</param>
    /// <returns>The locale entry location parsed from the contents of <paramref name="line"/>.</returns>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="FormatException"/>
    public static LocaleEntryLocation Parse(string line)
    {
        ArgumentNullException.ThrowIfNull(line, nameof(line));

        try
        {
            string[] components = line.Split('\t');
            uint hash = uint.Parse(components[0]);
            int offset = int.Parse(components[1]);
            int size = int.Parse(components[2]);
            return new LocaleEntryLocation(hash, offset, size);
        }
        catch (Exception ex)
        {
            throw new FormatException($"Invalid locale entry location: {line}", ex);
        }
    }

    /// <summary>
    /// Returns a string representation of this locale entry location.
    /// </summary>
    public override string ToString() => $"{Hash}\t{Offset}\t{Size}\td";
}
