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
    /// Initializes an empty instance of <see cref="LocaleFileInfo"/>.
    /// </summary>
    public LocaleFileInfo()
    {
        LocaleDatFile = new("en_us_data.dat");
        LocaleDirFile = new("en_us_data.dir");
        Preamble = LocaleFile.UTF8Preamble1;
        Metadata = new();
        Locations = [];
        Entries = [];
        _idToEntry = [];
        _hashToEntry = [];
    }

    /// <summary>
    /// Initializes a new instance of <see cref="LocaleFileInfo"/> from the specified locale .dat file.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    public LocaleFileInfo(string localeDatPath)
    {
        LocaleDatFile = new(localeDatPath ?? throw new ArgumentNullException(nameof(localeDatPath)));
        LocaleDirFile = new(Path.ChangeExtension(localeDatPath, ".dir"));
        Preamble = LocaleFile.ReadPreamble(localeDatPath);
        Locations = [];
        Entries = LocaleFile.ReadEntries(localeDatPath);
        Metadata = LocaleMetadata.Create(localeDatPath, Entries);
    }

    /// <summary>
    /// Initializes a new instance of <see cref="LocaleFileInfo"/> from the specified locale .dat and .dir file.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    public LocaleFileInfo(string localeDatPath, string localeDirPath)
    {
        LocaleDatFile = new(localeDatPath ?? throw new ArgumentNullException(localeDatPath));
        LocaleDirFile = new(localeDirPath ?? throw new ArgumentNullException(localeDirPath));
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
    /// <exception cref="ArgumentNullException"/>/// <exception cref="ArgumentNullException"/>
    private LocaleFileInfo(FileInfo localeDatFile, FileInfo localeDirFile, ReadOnlySpan<byte> preamble,
                           LocaleMetadata metadata, LocaleEntryLocation[] locations, LocaleEntry[] entries)
    {
        LocaleDatFile = localeDatFile ?? throw new ArgumentNullException(nameof(localeDatFile));
        LocaleDirFile = localeDirFile ?? throw new ArgumentNullException(nameof(localeDirFile));
        Preamble = preamble;
        Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
        Locations = locations ?? throw new ArgumentNullException(nameof(locations));
        Entries = entries ?? throw new ArgumentNullException(nameof(entries));
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
            else if (!CanAddEntries) throw new InvalidOperationException("Cannot create IDs for a TCG locale file.");
            else if (_hashToEntry != null) return _idToEntry = Preimaging.CreateIdMapping(StoredEntries);
            else return _idToEntry = Preimaging.CreateIdMapping(Entries);
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
    /// Adds the specified collection of strings as locale entries.
    /// </summary>
    /// <remarks>The stored entries can be written with any of the <c>WriteEntries()</c> methods.</remarks>
    /// <returns>The IDs of the new locale entries.</returns>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="InvalidOperationException"/>
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
    /// Adds a locale entry with the specified text.
    /// </summary>
    /// <remarks><inheritdoc cref="AddEntries(IEnumerable{string})"/></remarks>
    /// <returns>The ID of the new locale entry.</returns>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="InvalidOperationException"/>
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
    /// Replaces the text of all entries that have the specified old text with the new text.
    /// </summary>
    /// <remarks><inheritdoc cref="AddEntries(IEnumerable{string})"/></remarks>
    /// <returns>The number of locale entries with text replaced.</returns>
    /// <exception cref="ArgumentNullException"/>
    public int ReplaceEntries(string oldText, string newText) => ReplaceEntries(x => x.Text == oldText, newText);

    /// <summary>
    /// Replaces the text from all entries that match the specified predicate with the specified text.
    /// </summary>
    /// <remarks><inheritdoc cref="AddEntries(IEnumerable{string})"/></remarks>
    /// <returns>The number of locale entries with text replaced.</returns>
    /// <exception cref="ArgumentNullException"/>
    public int ReplaceEntries(Func<LocaleEntry, bool> predicate, string text)
    {
        ArgumentNullException.ThrowIfNull(predicate, nameof(predicate));
        ArgumentNullException.ThrowIfNull(text, nameof(text));

        HashSet<LocaleEntry> entries = [.. StoredEntries.Where(predicate)];
        int entriesReplaced = 0;

        // Replace the text from entries from the hash -> entry mapping.
        foreach (LocaleEntry entry in entries)
        {
            List<LocaleEntry> hashEntries = HashToEntry[entry.Hash];
            LocaleEntry updatedEntry = entry with { Text = text };

            // If the tag indicates a hash collision, replace text from matching entries in the bucket.
            if (entry.Tag.IsMtag())
            {
                for (int i = 0; i < hashEntries.Count; i++)
                {
                    if (hashEntries[i] == entry)
                    {
                        hashEntries[i] = updatedEntry;
                        entriesReplaced++;
                    }
                }
            }
            else
            {
                hashEntries[0] = updatedEntry;
                entriesReplaced++;
            }
        }

        // Replace the text from entries from the ID -> entry mapping, if one was created.
        int idsLeft = entriesReplaced;

        if (idsLeft > 0)
        {
            foreach (var kvp in _idToEntry?.ToList() ?? [])
            {
                if (idsLeft == 0) break;

                if (entries.Contains(kvp.Value))
                {
                    IdToEntry[kvp.Key] = kvp.Value with { Text = text };
                    idsLeft--;
                }
            }
        }

        return entriesReplaced;
    }

    /// <summary>
    /// Replaces the text of entries matching the first sequence with the corresponding text from the second sequence.
    /// </summary>
    /// <remarks><inheritdoc cref="AddEntries(IEnumerable{string})"/></remarks>
    /// <returns>The number of locale entries with text replaced.</returns>
    /// <exception cref="ArgumentNullException"/>
    public int ReplaceEntries(IEnumerable<string> first, IEnumerable<string> second)
        => ReplaceEntries(Enumerable.Zip(first, second));

    /// <summary>
    /// Replaces the text of entries matching the first item with the second item in the sequence.
    /// </summary>
    /// <remarks><inheritdoc cref="AddEntries(IEnumerable{string})"/></remarks>
    /// <returns>The number of locale entries with text replaced.</returns>
    /// <exception cref="ArgumentNullException"/>
    public int ReplaceEntries(IEnumerable<(string, string)> items)
    {
        Dictionary<string, string> replacements = [];

        foreach ((string oldText, string newText) in items)
        {
            replacements[oldText] = newText;
        }

        return ReplaceEntries(replacements);
    }

    /// <summary>
    /// Replaces the text of all entries matching keys in the dictionary with the corresponding values.
    /// </summary>
    /// <remarks><inheritdoc cref="AddEntries(IEnumerable{string})"/></remarks>
    /// <returns>The number of locale entries with text replaced.</returns>
    /// <exception cref="ArgumentNullException"/>
    public int ReplaceEntries(Dictionary<string, string> replacements)
    {
        ArgumentNullException.ThrowIfNull(replacements, nameof(replacements));

        Dictionary<LocaleEntry, string> entryToText = [];

        // Map entries to replacement text.
        foreach (LocaleEntry entry in StoredEntries)
        {
            if (replacements.TryGetValue(entry.Text, out string? text))
            {
                entryToText[entry] = text ?? throw new ArgumentException("Text replacement cannot be null.");
            }
        }

        int entriesReplaced = 0;

        // Replace the text from entries from the hash -> entry mapping.
        foreach (var kvp in entryToText)
        {
            LocaleEntry entry = kvp.Key;
            string text = kvp.Value;
            List<LocaleEntry> hashEntries = HashToEntry[entry.Hash];
            LocaleEntry updatedEntry = entry with { Text = text };

            // If the tag indicates a hash collision, replace text from matching entries in the bucket.
            if (entry.Tag.IsMtag())
            {
                for (int i = 0; i < hashEntries.Count; i++)
                {
                    if (hashEntries[i] == entry)
                    {
                        hashEntries[i] = updatedEntry;
                        entriesReplaced++;
                    }
                }
            }
            else
            {
                hashEntries[0] = updatedEntry;
                entriesReplaced++;
            }
        }

        // Replace the text from entries from the ID -> entry mapping, if one was created.
        int idsLeft = entriesReplaced;

        if (idsLeft > 0)
        {
            foreach (var kvp in _idToEntry?.ToList() ?? [])
            {
                if (idsLeft == 0) break;

                if (entryToText.TryGetValue(kvp.Value, out string? text))
                {
                    IdToEntry[kvp.Key] = kvp.Value with { Text = text };
                    idsLeft--;
                }
            }
        }

        return entriesReplaced;
    }

    /// <summary>
    /// Replaces the text of the entry with the given ID with the specified value.
    /// </summary>
    /// <remarks><inheritdoc cref="AddEntries(IEnumerable{string})"/></remarks>
    /// <returns><see langword="true"/> if the text was replaced; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="InvalidOperationException"/>
    public bool ReplaceEntry(int id, string text)
    {
        ArgumentNullException.ThrowIfNull(text, nameof(text));

        if (IdToEntry.TryGetValue(id, out LocaleEntry? entry))
        {
            LocaleEntry updatedEntry = entry with { Text = text };
            IdToEntry[id] = updatedEntry;
            List<LocaleEntry> hashEntries = HashToEntry[Preimaging.GetHash(id)];

            // If the tag indicates a hash collision, replace text from matching entries in the bucket.
            if (entry.Tag.IsMtag())
            {
                for (int i = 0; i < hashEntries.Count; i++)
                {
                    if (hashEntries[i] == entry)
                    {
                        hashEntries[i] = updatedEntry;
                    }
                }
            }
            else
            {
                hashEntries[0] = updatedEntry;
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// Removes all entries with the specified text.
    /// </summary>
    /// <remarks><inheritdoc cref="AddEntries(IEnumerable{string})"/></remarks>
    /// <returns>The number of locale entries removed.</returns>
    public int RemoveEntries(string text) => RemoveEntries(x => x.Text == text);

    /// <summary>
    /// Removes all entries that match the specified predicate.
    /// </summary>
    /// <remarks><inheritdoc cref="AddEntries(IEnumerable{string})"/></remarks>
    /// <returns>The number of locale entries removed.</returns>
    /// <exception cref="ArgumentNullException"/>
    public int RemoveEntries(Func<LocaleEntry, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate, nameof(predicate));

        HashSet<LocaleEntry> entries = [.. StoredEntries.Where(predicate)];
        int entriesRemoved = 0;

        // Remove entries from the hash -> entry mapping.
        foreach (LocaleEntry entry in entries)
        {
            // If the tag indicates a hash collision, remove matching entries from the bucket.
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
        int idsLeft = entriesRemoved;

        if (idsLeft > 0)
        {
            foreach (var kvp in _idToEntry?.ToList() ?? [])
            {
                if (idsLeft == 0) break;

                if (entries.Contains(kvp.Value))
                {
                    IdToEntry.Remove(kvp.Key);
                    idsLeft--;
                }
            }
        }

        return entriesRemoved;
    }

    /// <summary>
    /// Removes the entry with the specified ID.
    /// </summary>
    /// <remarks><inheritdoc cref="AddEntries(IEnumerable{string})"/></remarks>
    /// <returns><see langword="true"/> if the entry was removed; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="InvalidOperationException"/>
    public bool RemoveEntry(int id)
    {
        if (IdToEntry.Remove(id, out LocaleEntry? entry))
        {
            // If the tag indicates a hash collision, remove matching entries from the bucket.
            if (entry.Tag.IsMtag())
            {
                List<LocaleEntry> mtagEntries = HashToEntry[Preimaging.GetHash(id)];
                mtagEntries.RemoveAll(x => x == entry);

                // If the bucket is empty, remove the element from the dictionary.
                if (mtagEntries.Count == 0)
                {
                    HashToEntry.Remove(entry.Hash);
                }
            }
            else
            {
                HashToEntry.Remove(entry.Hash);
            }

            return true;
        }

        return false;
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
    /// <exception cref="ArgumentNullException"/>
    public LocaleFileInfo WriteEntries(string localeDatPath, string localeDirPath)
        => WriteEntries(new FileInfo(localeDatPath ?? throw new ArgumentNullException(nameof(localeDatPath))),
                        new FileInfo(localeDirPath ?? throw new ArgumentNullException(nameof(localeDirPath))));

    /// <summary>
    /// Writes the stored locale entries to the specified .dat file and .dir file.
    /// </summary>
    /// <returns>
    /// A new instance of <see cref="LocaleFileInfo"/> for the specified .dat file and .dir file.
    /// </returns>
    /// <exception cref="ArgumentNullException"/>
    public LocaleFileInfo WriteEntries(FileInfo localeDatFile, FileInfo localeDirFile)
    {
        ArgumentNullException.ThrowIfNull(localeDatFile, nameof(localeDatFile));
        ArgumentNullException.ThrowIfNull(localeDirFile, nameof(localeDirFile));

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
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="FormatException"/>
    private static int GetMtagSize(string entry)
    {
        ArgumentNullException.ThrowIfNull(entry, nameof(entry));

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
