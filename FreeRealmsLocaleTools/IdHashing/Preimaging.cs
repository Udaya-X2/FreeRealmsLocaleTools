using FreeRealmsLocaleTools.LocaleParser;
using System.Text.RegularExpressions;

namespace FreeRealmsLocaleTools.IdHashing
{
    /// <summary>
    /// Provides static methods for obtaining the IDs of Free Realms locale hashes and vice versa.
    /// </summary>
    public static class Preimaging
    {
        private const int MaxId = 5103267;

        private static readonly Regex IdRegex = new(@"\t0017\tGlobal\.Text\.(\d+)$", RegexOptions.RightToLeft);

        /// <summary>
        /// Creates a sorted dictionary mapping hashes to locale entries from the specified collection.
        /// </summary>
        /// <returns>A sorted dictionary mapping hashes to locale entries.</returns>
        public static SortedDictionary<uint, List<LocaleEntry>> CreateHashMapping(IEnumerable<LocaleEntry> entries)
        {
            SortedDictionary<uint, List<LocaleEntry>> hashToEntry = new();

            foreach (LocaleEntry entry in entries)
            {
                if (hashToEntry.TryGetValue(entry.Hash, out List<LocaleEntry>? entryList))
                {
                    entryList.Add(entry);
                }
                else
                {
                    hashToEntry[entry.Hash] = new(1) { entry };
                }
            }

            return hashToEntry;
        }

        /// <summary>
        /// Creates an ID for each hashable locale entry in the specified collection.
        /// </summary>
        /// <returns>A sorted dictionary mapping IDs to hashable locale entries.</returns>
        public static SortedDictionary<uint, LocaleEntry> CreateIdMapping(IEnumerable<LocaleEntry> entries)
        {
            Dictionary<uint, LocaleEntry> hashToEntry = new();
            SortedDictionary<uint, LocaleEntry> idToEntry = new();

            // Create a mapping from hash to locale entry.
            foreach (LocaleEntry entry in entries)
            {
                switch (entry.Tag)
                {
                    // Add locale entries that can be hashed via ID to the hash dictionary.
                    case LocaleTag.ucdt:
                    case LocaleTag.ucdn:
                        hashToEntry.Add(entry.Hash, entry);
                        break;
                    // Add locale entries that already have IDs to the ID dictionary.
                    case LocaleTag.mcdt:
                    case LocaleTag.mcdn:
                        uint id = uint.Parse(IdRegex.Match(entry.Text).Groups[1].Value);
                        idToEntry.Add(id, entry);
                        break;
                }
            }

            // Until all hashes have been processed, keep creating IDs and hashing them.
            for (uint id = 0; hashToEntry.Count > 0 && id <= MaxId; id++)
            {
                uint hash = GetHash(id);

                // If the hash maps to a locale entry, remove the hash from the dictionary.
                if (hashToEntry.Remove(hash, out LocaleEntry? entry))
                {
                    idToEntry.Add(id, entry);
                }
            }

            return idToEntry;
        }

        /// <summary>
        /// Returns the locale hash of the specified ID.
        /// </summary>
        public static uint GetHash(uint id) => JenkinsLookup2.Hash($"Global.Text.{id}");
    }
}
