namespace FreeRealmsLocaleTools
{
    /// <summary>
    /// Specifies the types of 4-letter tags that can appear with locale text.
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
        /// Used for text of the form
        /// <code>&lt;text&gt;\t0017\tGlobal.Text.&lt;id&gt;</code>
        /// Where text is the locale text and id is a number such that <see cref="JenkinsLookup2.Hash(string)"/>
        /// returns the hash corresponding to this locale entry when "Global.Text.&lt;id&gt;" is given as the key.
        /// </summary>
        mcdt,
        /// <summary>
        /// Used for text of the form
        /// <code>\t0017\tGlobal.Text.&lt;id&gt;</code>
        /// Where id is a number such that <see cref="JenkinsLookup2.Hash(string)"/> returns the hash
        /// corresponding to this locale entry when "Global.Text.&lt;id&gt;" is given as the key.
        /// </summary>
        mcdn
    }
}
