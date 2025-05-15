using FreeRealmsLocaleTools.IdHashing;
using System.Text;

namespace FreeRealmsLocaleTools.LocaleParser;

/// <summary>
/// Provides properties and instance methods for reading and writing Free Realms locale files.
/// </summary>
public class LocaleFileInfo
{
    // Number of chars before the "key" section of a local entry with a tag starting with 'm'.
    private const int MtagPreKeyChars = 5;

    // Cache this number to avoid inaccurate write offsets for different operating systems.
    private readonly int NewLineLength = Environment.NewLine.Length;

    private byte[] _preamble = null!;
    private SortedDictionary<int, LocaleEntry>? _idToEntry;
    private SortedDictionary<uint, List<LocaleEntry>>? _hashToEntry;
    private IEnumerator<int>? _unusedIds;

    /// <summary>
    /// Initializes a new instance of <see cref="LocaleFileInfo"/>
    /// from the specified locale .dat file and optional settings.
    /// </summary>
    public LocaleFileInfo(string localeDatPath)
    {
        LocaleDatFile = new(localeDatPath);
        LocaleDirFile = new(Path.ChangeExtension(localeDatPath, ".dir"));
        Preamble = LocaleFile.ReadPreamble(localeDatPath);
        Locations = [];
        Entries = LocaleFile.ReadEntries(localeDatPath);
        Metadata = LocaleMetadata.Create(localeDatPath, Entries);
    }

    /// <summary>
    /// Initializes a new instance of <see cref="LocaleFileInfo"/> from the
    /// specified locale .dat file, locale .dir file, and optional settings.
    /// </summary>
    public LocaleFileInfo(string localeDatPath, string localeDirPath)
    {
        LocaleDatFile = new(localeDatPath);
        LocaleDirFile = new(localeDirPath);
        Preamble = LocaleFile.ReadPreamble(localeDatPath);
        Metadata = LocaleFile.ReadMetadata(localeDirPath);
        Locations = LocaleFile.ReadEntryLocations(localeDirPath);

        // Some Simplified Chinese TCG locales have incorrect .dir files,
        // so use the .dat file exclusively to read entries for them.
        Entries = Metadata.IsTCG() && Metadata.Locale == Locale.zh_CN
                ? LocaleFile.ReadEntries(localeDatPath)
                : LocaleFile.ReadEntries(localeDatPath, localeDirPath);
    }

    /// <summary>
    /// Initializes a new instance of <see cref="LocaleFileInfo"/> from the specified parameters.
    /// </summary>
    private LocaleFileInfo(FileInfo localeDatFile, FileInfo localeDirFile, ReadOnlySpan<byte> preamble,
                           LocaleMetadata metadata, LocaleEntryLocation[] locations, LocaleEntry[] entries)
    {
        LocaleDatFile = localeDatFile;
        LocaleDirFile = localeDirFile;
        Preamble = preamble;
        Metadata = metadata;
        Locations = locations;
        Entries = entries;
    }

    /// <summary>
    /// Gets the locale .dat file.
    /// </summary>
    public FileInfo LocaleDatFile { get; private init; }

    /// <summary>
    /// Gets the locale .dir file, or the locale .dat file with its
    /// extension changed to ".dir" if no .dir file was not given.
    /// </summary>
    public FileInfo LocaleDirFile { get; private init; }

    /// <summary>
    /// Gets the preamble bytes at the beginning of the locale .dat file.
    /// </summary>
    public ReadOnlySpan<byte> Preamble
    {
        get => _preamble;
        private init => _preamble = value.ToArray();
    }

    /// <summary>
    /// Gets the locale metadata.
    /// </summary>
    public LocaleMetadata Metadata { get; private init; }

    /// <summary>
    /// Gets the locale entry locations read from the .dir file, or an empty array if no .dir file was given.
    /// </summary>
    public LocaleEntryLocation[] Locations { get; private init; }

    /// <summary>
    /// Gets the locale entries read from the .dat file.
    /// </summary>
    public LocaleEntry[] Entries { get; private init; }

    /// <summary>
    /// Gets whether adding locale entries is supported.
    /// </summary>
    public bool CanAddEntries => !Metadata.IsTCG();

    /// <summary>
    /// Gets a sorted mapping from IDs to locale entries.
    /// </summary>
    /// <exception cref="InvalidOperationException"/>
    public SortedDictionary<int, LocaleEntry> IdToEntry
    {
        get
        {
            // TCG locale files don't hash IDs the same way as game locale
            // files, so creating IDs for TCG locale files is unsupported.
            if (_idToEntry != null) return _idToEntry;
            else if (_hashToEntry != null) return _idToEntry = Preimaging.CreateIdMapping(StoredEntries);
            else if (CanAddEntries) return _idToEntry = Preimaging.CreateIdMapping(Entries);
            else throw new InvalidOperationException("Cannot create IDs for a TCG locale file.");
        }
    }

    /// <summary>
    /// Gets a sorted mapping from hashes to locale entries.
    /// </summary>
    public SortedDictionary<uint, List<LocaleEntry>> HashToEntry
    {
        get
        {
            return _hashToEntry ??= Preimaging.CreateHashMapping(Entries);
        }
    }

    /// <summary>
    /// Gets a view of the stored locale entries, including additions and removals, ordered by hash.
    /// </summary>
    public IEnumerable<LocaleEntry> StoredEntries => HashToEntry.SelectMany(x => x.Value);

    /// <summary>
    /// Gets an enumerator for unused IDs.
    /// </summary>
    private IEnumerator<int> UnusedIds => _unusedIds ??= Enumerable.Range(1, Preimaging.MaxId)
                                                                   .Where(x => !IdToEntry.ContainsKey(x))
                                                                   .GetEnumerator();

    /// <summary>
    /// Gets the current ID.
    /// </summary>
    private int CurrentId => UnusedIds.Current;

    /// <summary>
    /// Gets the next unused ID.
    /// </summary>
    private int NextId => UnusedIds.MoveNext()
                        ? UnusedIds.Current
                        : throw new InvalidOperationException("No more IDs to add entries.");

    /// <summary>
    /// Adds the specified collection of strings as locale entries to the ID/hash -> entry mappings.
    /// </summary>
    /// <remarks>The stored entries can be written with any of the <c>WriteEntries()</c> methods.</remarks>
    /// <returns>The IDs of the new locale entries.</returns>
    /// <exception cref="ArgumentNullException"/>
    public List<int> AddEntries(IEnumerable<string> contents)
    {
        ArgumentNullException.ThrowIfNull(contents, nameof(contents));
    
        List<int> ids = [];

        foreach (string text in contents)
        {
            ids.Add(AddEntry(text));
        }

        return ids;
    }

    /// <summary>
    /// Adds a locale entry with the specified text to the ID/hash -> entry mappings.
    /// </summary>
    /// <remarks><inheritdoc cref="AddEntries(IEnumerable{string})"/></remarks>
    /// <returns>The ID of the new locale entry.</returns>
    /// <exception cref="ArgumentNullException"/>
    public int AddEntry(string text)
    {
        ArgumentNullException.ThrowIfNull(text, nameof(text));

        // Generate an new locale entry from the next unused ID.
        LocaleEntry entry = Preimaging.GenerateEntry(NextId, text);

        // If the current ID's hash collides with another entry, find another ID.
        while (!HashToEntry.TryAdd(entry.Hash, [entry]))
        {
            entry = entry with { Hash = Preimaging.GetHash(NextId) };
        }

        int id = CurrentId;
        IdToEntry.Add(id, entry);
        return id;
    }

    /// <summary>
    /// Replaces the text from all entries that match the specified predicate with the specified text.
    /// </summary>
    /// <remarks><inheritdoc cref="AddEntries(IEnumerable{string})"/></remarks>
    /// <returns>The number of locale entries with text replaced.</returns>
    public int ReplaceEntries(Func<LocaleEntry, bool> predicate, string text)
    {
        HashSet<LocaleEntry> entries = [.. StoredEntries.Where(predicate)];
        int entriesReplaced = 0;

        // Replace the text from entries from the hash -> entry mapping.
        foreach (LocaleEntry entry in entries)
        {
            List<LocaleEntry> newEntries = [.. HashToEntry[entry.Hash].Select(x => x with { Text = text })];
            HashToEntry[entry.Hash] = newEntries;
            entriesReplaced += newEntries.Count;
        }

        // Replace the text from entries from the ID -> entry mapping, if one was created.
        foreach (var kvp in _idToEntry?.ToList() ?? Enumerable.Empty<KeyValuePair<int, LocaleEntry>>())
        {
            if (entries.Contains(kvp.Value))
            {
                IdToEntry[kvp.Key] = kvp.Value with { Text = text };
            }
        }

        return entriesReplaced;
    }

    /// <summary>
    /// Removes all entries that match the specified predicate from the ID/hash -> entry mappings.
    /// </summary>
    /// <remarks><inheritdoc cref="AddEntries(IEnumerable{string})"/></remarks>
    /// <returns>The number of locale entries removed.</returns>
    public int RemoveEntries(Func<LocaleEntry, bool> predicate)
    {
        HashSet<LocaleEntry> entries = [.. StoredEntries.Where(predicate)];
        int entriesRemoved = 0;

        // Remove entries from the hash -> entry mapping.
        foreach (LocaleEntry entry in entries)
        {
            // If the tag indicates a hash collision, remove identical entries from the bucket.
            if (entry.Tag.IsMtag())
            {
                List<LocaleEntry> mtagEntries = HashToEntry[entry.Hash];
                entriesRemoved += mtagEntries.RemoveAll(x => x == entry);

                // If the bucket is empty, remove the element from the dictionary.
                if (mtagEntries.Count == 0)
                {
                    HashToEntry.Remove(entry.Hash);
                }
            }
            else
            {
                HashToEntry.Remove(entry.Hash);
                entriesRemoved++;
            }
        }

        // Remove entries from the ID -> entry mapping, if one was created.
        foreach (var kvp in _idToEntry?.ToList() ?? Enumerable.Empty<KeyValuePair<int, LocaleEntry>>())
        {
            if (entries.Contains(kvp.Value))
            {
                IdToEntry.Remove(kvp.Key);
            }
        }

        return entriesRemoved;
    }

    /// <summary>
    /// Writes the stored locale entries to the .dat file and .dir file specified upon creation of this instance.
    /// </summary>
    /// <returns>
    /// A new instance of <see cref="LocaleFileInfo"/> with the updated entries, locations, and metadata.
    /// </returns>
    public LocaleFileInfo WriteEntries() => WriteEntries(LocaleDatFile, LocaleDirFile);

    /// <summary>
    /// Writes the stored locale entries to the specified .dat file and .dir file.
    /// </summary>
    /// <returns><inheritdoc cref="WriteEntries(FileInfo, FileInfo)"/></returns>
    public LocaleFileInfo WriteEntries(string localeDatPath, string localeDirPath)
    {
        return WriteEntries(new FileInfo(localeDatPath), new FileInfo(localeDirPath));
    }

    /// <summary>
    /// Writes the stored locale entries to the specified .dat file and .dir file.
    /// </summary>
    /// <returns>
    /// A new instance of <see cref="LocaleFileInfo"/> for the specified .dat file and .dir file.
    /// </returns>
    public LocaleFileInfo WriteEntries(FileInfo localeDatFile, FileInfo localeDirFile)
    {
        LocaleEntry[] entries = [.. StoredEntries];
        LocaleEntryLocation[] locations = new LocaleEntryLocation[entries.Length];

        using (StreamWriter localeDatWriter = new(localeDatFile.Open(FileMode.Create, FileAccess.Write)))
        {
            // Write the preamble bytes at the start of the .dat file.
            localeDatWriter.BaseStream.Write(Preamble);
            int offset = Preamble.Length;

            for (int i = 0; i < entries.Length; i++)
            {
                // Write each locale entry to the .dat file.
                LocaleEntry entry = entries[i];
                string entryString = entry.ToString();
                localeDatWriter.WriteLine(entryString);

                // Create an entry location based on the current offset, size, and tag of the entry.
                int size = Encoding.UTF8.GetByteCount(entryString);
                int dirSize = entry.Tag.IsMtag() ? GetMtagSize(entryString) : size;
                locations[i] = new(entry.Hash, offset, dirSize);
                offset += size + NewLineLength;
            }
        }

        // Update the metadata and write it at the start of the .dir file.
        using StreamWriter localeDirWriter = new(localeDirFile.Open(FileMode.Create, FileAccess.Write));
        LocaleMetadata metadata = Metadata.Update(localeDatFile.FullName, entries);
        localeDirWriter.Write(metadata);

        // Write each locale entry location to the .dir file.
        foreach (LocaleEntryLocation location in locations)
        {
            localeDirWriter.WriteLine(location.ToString());
        }

        return new(localeDatFile, localeDirFile, Preamble, metadata, locations, entries);
    }

    /// <summary>
    /// Returns the size, in bytes, of the specified entry up to the pre-key section.
    /// </summary>
    /// <exception cref="FormatException"/>
    private static int GetMtagSize(string entry)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(entry);
        int lastIndex = Array.LastIndexOf(bytes, (byte)'\t') - MtagPreKeyChars;
        int firstIndex = Array.IndexOf(bytes, (byte)'\t') + LocaleFile.SkipTagChars;

        if (lastIndex < firstIndex)
        {
            throw new FormatException($"Invalid locale entry: {{{entry}}}");
        }

        return lastIndex;
    }
}
