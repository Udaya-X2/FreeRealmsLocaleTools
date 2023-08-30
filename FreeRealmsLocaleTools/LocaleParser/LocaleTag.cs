namespace FreeRealmsLocaleTools.LocaleParser
{
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
        /// Used for names.
        /// </summary>
        ugdt,
        /// <summary>
        /// Used for some blank text.
        /// </summary>
        ugdn,
        /// <summary>
        /// Used for text followed by
        /// <code>\t0017\tGlobal.Text.&lt;id&gt;</code>
        /// Where id is a number such that <see cref="JenkinsLookup2.Hash(string)"/> returns the hash
        /// corresponding to the text's locale entry when "Global.Text.&lt;id&gt;" is given as the key.
        /// </summary>
        mcdt,
        /// <summary>
        /// Used for blank text followed by
        /// <code>\t0017\tGlobal.Text.&lt;id&gt;</code>
        /// Where id is a number such that <see cref="JenkinsLookup2.Hash(string)"/> returns the hash
        /// corresponding to the text's locale entry when "Global.Text.&lt;id&gt;" is given as the key.
        /// </summary>
        mcdn
    }
}
