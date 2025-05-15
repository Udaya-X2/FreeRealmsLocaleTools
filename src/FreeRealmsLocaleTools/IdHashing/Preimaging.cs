using FreeRealmsLocaleTools.LocaleParser;
using System.Text.RegularExpressions;

namespace FreeRealmsLocaleTools.IdHashing;

/// <summary>
/// Provides static methods for obtaining the IDs of Free Realms locale hashes and vice versa.
/// </summary>
public static partial class Preimaging
{
    /// <summary>
    /// Maximum ID that can appear in a locale .dat file.
    /// </summary>
    public const int MaxId = 5103267;

    [GeneratedRegex(@"\t0017\tGlobal\.Text\.(\d+)$", RegexOptions.RightToLeft)]
    private static partial Regex IdRegex();

    /// <summary>
    /// Creates a sorted dictionary mapping hashes to locale entries from the specified collection.
    /// </summary>
    /// <returns>A sorted dictionary mapping hashes to locale entries.</returns>
    public static SortedDictionary<uint, List<LocaleEntry>> CreateHashMapping(IEnumerable<LocaleEntry> entries)
    {
        SortedDictionary<uint, List<LocaleEntry>> hashToEntry = [];

        foreach (LocaleEntry entry in entries)
        {
            // If the tag indicates a hash collision, add the entry to the existing mapping.
            if (entry.Tag.IsMtag() && hashToEntry.TryGetValue(entry.Hash, out List<LocaleEntry>? entryList))
            {
                entryList.Add(entry);
            }
            else
            {
                hashToEntry.Add(entry.Hash, [entry]);
            }
        }

        return hashToEntry;
    }

    /// <summary>
    /// Creates an ID for each hashable locale entry in the specified collection.
    /// </summary>
    /// <returns>A sorted dictionary mapping IDs to hashable locale entries.</returns>
    public static SortedDictionary<int, LocaleEntry> CreateIdMapping(IEnumerable<LocaleEntry> entries)
    {
        Dictionary<uint, LocaleEntry> hashToEntry = [];
        SortedDictionary<int, LocaleEntry> idToEntry = [];

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
                    int id = int.Parse(IdRegex().Match(entry.Text).Groups[1].Value);
                    idToEntry.Add(id, entry);
                    break;
            }
        }

        // Until all hashes have been processed, keep creating IDs and hashing them.
        for (int id = 0; hashToEntry.Count > 0 && id <= MaxId; id++)
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
    /// Generates an array of locale entries from the specified collection of strings.
    /// </summary>
    /// <returns>An array of locale entries with unique hashes, ordered by distinct ID number.</returns>
    public static LocaleEntry[] GenerateEntries(IEnumerable<string> strings)
    {
        LocaleEntry[] entries = new LocaleEntry[strings.Count()];
        HashSet<uint> hashes = [];
        int index = 0;
        int id = 1;

        if (entries.Length > MaxId)
        {
            throw new ArgumentException($"Collection size ({entries.Length}) exceeds maximum ID ({MaxId}).");
        }

        foreach (string text in strings)
        {
            LocaleEntry entry = GenerateEntry(id++, text);

            // If the current ID's hash collides with another entry, find another ID.
            while (!hashes.Add(entry.Hash))
            {
                entry = entry with { Hash = GetHash(id++) };
            }

            entries[index++] = entry;
        }

        return entries;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="LocaleEntry"/> from the specified ID and text.
    /// </summary>
    /// <returns>A locale entry with the specified text and hash generated from the ID.</returns>
    public static LocaleEntry GenerateEntry(int id, string text)
    {
        uint hash = GetHash(id);
        LocaleTag tag = text != "" ? LocaleTag.ucdt : LocaleTag.ucdn;
        return new(hash, tag, text);
    }

    /// <summary>
    /// Returns the locale hash of the specified ID.
    /// </summary>
    public static uint GetHash(int id) => JenkinsLookup2.Hash($"Global.Text.{id}");
}
