namespace FreeRealmsLocaleTools.LocaleParser;

/// <summary>
/// Specifies how to parse locale entries.
/// </summary>
public enum ParseOptions
{
    /// <summary>
    /// Parse locale entries from the .dat/.dir files only.
    /// </summary>
    Strict,
    /// <summary>
    /// Parse locale entries from the .dat/.dir files for standard locale files.
    /// For Simplified Chinese TCG locale files, parse entries using only the .dat file.
    /// </summary>
    Normal,
    /// <summary>
    /// Attempt to parse locale entries from the .dat/.dir files.
    /// Upon failure, parse entries using only the .dat file.
    /// </summary>
    Lenient
}
