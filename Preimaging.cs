namespace FreeRealmsLocaleTools
{
    /// <summary>
    /// Provides static methods for obtaining the IDs of Free Realms locale hashes and vice versa.
    /// </summary>
    public static class Preimaging
    {
        /// <summary>
        /// Creates a set of locale texts with IDs initialized by preimaging each hash from the specified dictionary.
        /// </summary>
        /// <remarks><inheritdoc cref="CreateLocaleTextIdsWith(Dictionary{uint, LocaleText[]})"/></remarks>
        /// <returns><inheritdoc cref="CreateLocaleTextIdsWith(Dictionary{uint, LocaleText[]})"/></returns>
        public static SortedSet<LocaleText> CreateLocaleTextIds(Dictionary<uint, LocaleText[]> hashToLocaleText)
        {
            return CreateLocaleTextIdsWith(new(hashToLocaleText));
        }

        /// <summary>
        /// Creates a set of locale texts with IDs initialized by preimaging each hash from the
        /// specified dictionary. This operation removes all elements from the dictionary.
        /// </summary>
        /// <remarks>
        /// This is essentially a conversion from the one-to-many relation, hash -> <see cref="LocaleText"/>,
        /// to the one-to-one relation, ID -> <see cref="LocaleText"/>.
        /// <para/>
        /// If the specified dictionary contains a hash that was not generated
        /// via the Jenkins lookup2 algorithm, this operation may hang.
        /// </remarks>
        /// <returns>A sorted set of locale texts, ordered by their initialized IDs.</returns>
        public static SortedSet<LocaleText> CreateLocaleTextIdsWith(Dictionary<uint, LocaleText[]> hashToLocaleText)
        {
            SortedSet<LocaleText> localeTextIds = new();

            // Until all hashes have been processed, keep creating IDs and hashing them.
            for (uint id = 0; hashToLocaleText.Count > 0; id++)
            {
                uint hash = GetIdHash(id);

                // If the hash maps to one or more locale texts, remove the hash from the dictionary.
                if (hashToLocaleText.Remove(hash, out LocaleText[]? localeTexts))
                {
                    // Initialize each locale text's ID and add it to the set.
                    foreach (LocaleText localeText in localeTexts)
                    {
                        localeText.Id ??= id;
                        localeTextIds.Add(localeText);
                    }
                }
            }

            return localeTextIds;
        }

        /// <summary>
        /// Returns the locale hash of the specified ID.
        /// </summary>
        public static uint GetIdHash(uint id) => JenkinsLookup2.Hash($"Global.Text.{id}");
    }
}
