using FreeRealmsLocaleTools.LocaleParser;

namespace FreeRealmsLocaleTools.IdHashing
{
    /// <summary>
    /// Provides static methods for obtaining the IDs of Free Realms locale hashes and vice versa.
    /// </summary>
    public static class Preimaging
    {
        /// <summary>
        /// Assigns an ID to each locale entry in the specified dictionary.
        /// </summary>
        /// <remarks><inheritdoc cref="ToLocaleEntryIdSet(Dictionary{uint, LocaleEntry[]})"/></remarks>
        /// <returns><inheritdoc cref="ToLocaleEntryIdSet(Dictionary{uint, LocaleEntry[]})"/></returns>
        public static SortedSet<LocaleEntry> CreateLocaleEntryIdSet(Dictionary<uint, LocaleEntry[]> hashToLocaleEntry)
        {
            return ToLocaleEntryIdSet(new(hashToLocaleEntry));
        }

        /// <summary>
        /// Assigns an ID to each locale entry in the specified dictionary, and returns the set of entries.
        /// This operation removes all elements from the dictionary.
        /// </summary>
        /// <remarks>
        /// This is essentially a conversion from the one-to-many relation, hash -> <see cref="LocaleEntry"/>,
        /// to the one-to-one relation, ID -> <see cref="LocaleEntry"/>.
        /// <para/>
        /// If the specified dictionary contains a hash that was not generated
        /// via the Jenkins lookup2 algorithm, this operation may hang.
        /// </remarks>
        /// <returns>A sorted set of locale entries, ordered by ID number.</returns>
        public static SortedSet<LocaleEntry> ToLocaleEntryIdSet(Dictionary<uint, LocaleEntry[]> hashToLocaleEntry)
        {
            SortedSet<LocaleEntry> localeEntries = new();

            // Until all hashes have been processed, keep creating IDs and hashing them.
            for (uint id = 0; hashToLocaleEntry.Count > 0; id++)
            {
                uint hash = GetHash(id);

                // If the hash maps to one or more locale entries, remove the hash from the dictionary.
                if (hashToLocaleEntry.Remove(hash, out LocaleEntry[]? entries))
                {
                    // Initialize each entry's ID and add it to the set.
                    foreach (LocaleEntry entry in entries)
                    {
                        entry.Id ??= id;
                        localeEntries.Add(entry);
                    }
                }
            }

            return localeEntries;
        }

        /// <summary>
        /// Returns the locale hash of the specified ID.
        /// </summary>
        public static uint GetHash(uint id) => JenkinsLookup2.Hash($"Global.Text.{id}");
    }
}
