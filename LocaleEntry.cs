namespace FreeRealmsLocaleTools
{
    /// <summary>
    /// Represents a locale entry in a Free Realms .dir file.
    /// </summary>
    public record LocaleEntry(uint Hash, uint Offset, uint Size);
}
