namespace FreeRealmsLocaleTools.LocaleParser;

/// <summary>
/// Specifies the types of games that can appear in locale metadata.
/// </summary>
public enum Game
{
    /// <summary>
    /// Used for all locales except Simplified Chinese.
    /// </summary>
    FRLM,
    /// <summary>
    /// Used for the Simplified Chinese locale.
    /// </summary>
    FRLMCN,
    /// <summary>
    /// Used for 2009 locales.
    /// </summary>
    FRLMLV,
    /// <summary>
    /// Used for all TCG locales except Simplified Chinese.
    /// </summary>
    FRLMTCG,
    /// <summary>
    /// Used for the Simplified Chinese TCG locale.
    /// </summary>
    FRLMTCGCN,
    /// <summary>
    /// Used for the American English TCG locale.
    /// </summary>
    LON
}
