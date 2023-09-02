using FreeRealmsLocaleTools.LocaleParser;

namespace FreeRealmsLocaleTools.IdHashing
{
    /// <summary>
    /// Provides static methods for obtaining the IDs of Free Realms locale hashes and vice versa.
    /// </summary>
    public static class Preimaging
    {
        /// <summary>
        /// Assigns an ID to each locale entry in the specified list, and returns the set of entries.
        /// </summary>
        /// <returns>A sorted set of locale entries, ordered by ID number.</returns>
        public static SortedSet<LocaleEntry> CreateEntryIdSet(IEnumerable<LocaleEntry> localeEntries)
        {
            Comparer<LocaleEntry> comparer = Comparer<LocaleEntry>.Create((a, b) => a.Id.CompareTo(b.Id));
            Dictionary<uint, LocaleEntry> hashToLocaleEntry = new();
            SortedSet<LocaleEntry> idEntries = new(comparer);

            // Create a mapping from hash to locale entry.
            foreach (LocaleEntry localeEntry in localeEntries)
            {
                switch (localeEntry.Tag)
                {
                    // Add locale entries that can be hashed via ID to the dictionary.
                    case LocaleTag.ucdt:
                    case LocaleTag.ucdn:
                        hashToLocaleEntry.Add(localeEntry.Hash, localeEntry);
                        break;
                    // Add locale entries that already have IDs to the ID set.
                    case LocaleTag.mcdt:
                    case LocaleTag.mcdn:
                        idEntries.Add(localeEntry);
                        break;
                }
            }

            // Until all hashes have been processed, keep creating IDs and hashing them.
            for (int id = 0; hashToLocaleEntry.Count > 0; id++)
            {
                uint hash = GetHash(id);

                // If the hash maps to a locale entry, remove the hash from the dictionary.
                if (hashToLocaleEntry.Remove(hash, out LocaleEntry? entry))
                {
                    entry.Id = id;
                    idEntries.Add(entry);
                }
            }

            return idEntries;
        }

        /// <summary>
        /// Returns the locale hash of the specified ID.
        /// </summary>
        public static uint GetHash(int id) => JenkinsLookup2.Hash($"Global.Text.{id}");
    }
}
