namespace FreeRealmsLocaleTools.LocaleParser
{
    /// <summary>
    /// Represents a locale entry in a Free Realms .dat file.
    /// </summary>
    /// <param name="Hash">The 32-bit unsigned integer hash of the locale entry.</param>
    /// <param name="Tag">The 4-letter tag that accompanies the locale entry.</param>
    /// <param name="Text">The text stored in the locale entry.</param>
    public record LocaleEntry(uint Hash, LocaleTag Tag, string Text)
    {
        /// <summary>
        /// Returns a string representation of this locale entry.
        /// </summary>
        public override string ToString() => $"{Hash}\t{Tag}\t{Text}";
    }
}
