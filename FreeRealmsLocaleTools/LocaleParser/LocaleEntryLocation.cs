namespace FreeRealmsLocaleTools.LocaleParser
{
    /// <summary>
    /// Represents a locale entry location in a Free Realms .dir file.
    /// </summary>
    internal record LocaleEntryLocation(uint Hash, uint Offset, uint Size);
}
