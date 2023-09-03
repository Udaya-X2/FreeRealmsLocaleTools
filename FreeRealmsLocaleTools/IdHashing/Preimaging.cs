using FreeRealmsLocaleTools.LocaleParser;
using System.Text.RegularExpressions;

namespace FreeRealmsLocaleTools.IdHashing
{
    /// <summary>
    /// Provides static methods for obtaining the IDs of Free Realms locale hashes and vice versa.
    /// </summary>
    public static class Preimaging
    {
        private static readonly Regex IdRegex = new(@"\t0017\tGlobal\.Text\.(\d+)$", RegexOptions.RightToLeft);

        /// <summary>
        /// Creates an ID for each hashable locale entry in the specified collection.
        /// </summary>
        /// <returns>A sorted dictionary mapping IDs to hashable locale entries.</returns>
        public static SortedDictionary<uint, LocaleEntry> CreateIdMapping(IEnumerable<LocaleEntry> localeEntries)
        {
            Dictionary<uint, LocaleEntry> hashToLocaleEntry = new();
            SortedDictionary<uint, LocaleEntry> idToLocaleEntry = new();
            
            // Create a mapping from hash to locale entry.
            foreach (LocaleEntry localeEntry in localeEntries)
            {
                switch (localeEntry.Tag)
                {
                    // Add locale entries that can be hashed via ID to the hash dictionary.
                    case LocaleTag.ucdt:
                    case LocaleTag.ucdn:
                        hashToLocaleEntry.Add(localeEntry.Hash, localeEntry);
                        break;
                    // Add locale entries that already have IDs to the ID dictionary.
                    case LocaleTag.mcdt:
                    case LocaleTag.mcdn:
                        uint id = uint.Parse(IdRegex.Match(localeEntry.Text).Groups[1].Value);
                        idToLocaleEntry.Add(id, localeEntry);
                        break;
                }
            }

            // Until all hashes have been processed, keep creating IDs and hashing them.
            for (uint id = 0; hashToLocaleEntry.Count > 0; id++)
            {
                uint hash = GetHash(id);

                // If the hash maps to a locale entry, remove the hash from the dictionary.
                if (hashToLocaleEntry.Remove(hash, out LocaleEntry? entry))
                {
                    idToLocaleEntry.Add(id, entry);
                }
            }

            return idToLocaleEntry;
        }

        /// <summary>
        /// Returns the locale hash of the specified ID.
        /// </summary>
        public static uint GetHash(uint id) => JenkinsLookup2.Hash($"Global.Text.{id}");
    }
}
