namespace FreeRealmsLocaleTools.LocaleParser;

/// <summary>
/// Represents a locale entry in a Free Realms .dat file.
/// </summary>
/// <param name="Hash">The 32-bit unsigned integer hash of the locale entry.</param>
/// <param name="Tag">The 4-letter tag that accompanies the locale entry.</param>
/// <param name="Text">The text stored in the locale entry.</param>
public record LocaleEntry(uint Hash, LocaleTag Tag, string Text)
{
    /// <summary>
    /// Initializes a new instance of <see cref="LocaleEntry"/> by parsing the given .dat file line.
    /// </summary>
    /// <returns>The locale entry parsed from the contents of <paramref name="line"/>.</returns>
    /// <exception cref="FormatException"/>
    public static LocaleEntry Parse(string line)
    {
        try
        {
            int hashIndex = line.IndexOf('\t');
            uint hash = uint.Parse(line.AsSpan(0, hashIndex));
            LocaleTag tag = Enum.Parse<LocaleTag>(line.AsSpan(hashIndex + 1, 4));
            string text = line[(hashIndex + 6)..];
            return new LocaleEntry(hash, tag, text);
        }
        catch (Exception ex)
        {
            throw new FormatException($"Invalid locale entry: {line}", ex);
        }
    }

    /// <summary>
    /// Returns a string representation of this locale entry.
    /// </summary>
    public override string ToString() => $"{Hash}\t{Tag}\t{Text}";
}
