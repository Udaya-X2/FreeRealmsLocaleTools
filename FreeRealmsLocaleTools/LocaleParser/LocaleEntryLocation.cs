namespace FreeRealmsLocaleTools.LocaleParser
{
    /// <summary>
    /// Represents a locale entry location in a Free Realms .dir file.
    /// </summary>
    /// <param name="Hash">The 32-bit unsigned integer hash of the locale entry.</param>
    /// <param name="Offset">The position of the locale entry in the .dat file.</param>
    /// <param name="Size">The number of bytes in the locale entry.</param>
    public record LocaleEntryLocation(uint Hash, int Offset, int Size)
    {
        /// <summary>
        /// Returns a string representation of this locale entry location.
        /// </summary>
        public override string ToString() => $"{Hash}\t{Offset}\t{Size}\td";
    }
}
