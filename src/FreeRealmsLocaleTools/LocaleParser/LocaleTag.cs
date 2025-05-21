namespace FreeRealmsLocaleTools.LocaleParser;

/// <summary>
/// Specifies the types of 4-letter tags that can appear in locale entries.
/// </summary>
public enum LocaleTag
{
    /// <summary>
    /// Used for most text.
    /// </summary>
    ucdt,
    /// <summary>
    /// Used for most blank text.
    /// </summary>
    ucdn,
    /// <summary>
    /// Used for some text.
    /// </summary>
    ugdt,
    /// <summary>
    /// Used for some blank text.
    /// </summary>
    ugdn,
    /// <summary>
    /// Used for some text in non-English locales.
    /// </summary>
    utdt,
    /// <summary>
    /// Used for possessive pronoun macros in German locales.
    /// </summary>
    umdt,
    /// <summary>
    /// Used for the text "informal" in French and German locales.
    /// </summary>
    uidt,
    /// <summary>
    /// Used for text followed by <c>\t0017\tGlobal.Text.&lt;ID&gt;</c>, where ID is a number such
    /// that <see cref="IdHashing.JenkinsLookup2.Hash(string)"/> returns the hash corresponding
    /// to the text's locale entry when "Global.Text.&lt;ID&gt;" is given as the key.
    /// </summary>
    mcdt,
    /// <summary>
    /// Used for blank text followed by <c>\t0017\tGlobal.Text.&lt;ID&gt;</c>, where ID is a
    /// number such that <see cref="IdHashing.JenkinsLookup2.Hash(string)"/> returns the hash
    /// corresponding to the text's locale entry when "Global.Text.&lt;ID&gt;" is given as the key.
    /// </summary>
    mcdn,
    /// <summary>
    /// Used for older TCG locale text followed by <c>\t0006\t&lt;KEY&gt;</c>, where KEY is
    /// a string such that <see cref="IdHashing.JenkinsLookup2.Hash(string)"/> returns the
    /// hash corresponding to the text's locale entry when KEY is given as the key.
    /// </summary>
    mgdt
}
