namespace FreeRealmsLocaleTools.LocaleParser
{
    /// <summary>
    /// Represents a locale entry location in a Free Realms .dir file.
    /// </summary>
    internal record LocaleEntryLocation(uint Hash, int Offset, int Size)
    {
        /// <summary>
        /// Returns a string representation of this locale entry location.
        /// </summary>
        public override string ToString() => $"{Hash}\t{Offset}\t{Size}\td";
    }
}
