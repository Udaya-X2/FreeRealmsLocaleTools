using FreeRealmsLocaleTools.IdHashing;
using System.Text;

namespace FreeRealmsLocaleTools.LocaleParser
{
    /// <summary>
    /// Provides properties and instance methods for reading and writing Free Realms locale files.
    /// </summary>
    public class LocaleFileInfo
    {
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
        public byte[] Preamble { get; private init; }

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

        // Number of chars before the "key" section of a local entry with a tag starting with 'm'.
        private const int MtagPreKeyChars = 5;

        // Cache this number to avoid inaccurate write offsets for different operating systems.
        private readonly int NewLineLength = Environment.NewLine.Length;

        private SortedDictionary<int, LocaleEntry>? _idToEntry;
        private SortedDictionary<uint, List<LocaleEntry>>? _hashToEntry;
        private IEnumerator<int>? _unusedIds;
        private bool _canAddEntries;

        /// <summary>
        /// Initializes a new instance of <see cref="LocaleFileInfo"/>
        /// from the specified locale .dat file and optional settings.
        /// </summary>
        public LocaleFileInfo(string localeDatFile, bool canAddEntries = false)
        {
            LocaleDatFile = new(localeDatFile);
            LocaleDirFile = new(Path.ChangeExtension(localeDatFile, ".dir"));
            Preamble = LocaleFile.ReadPreamble(localeDatFile);
            Locations = Array.Empty<LocaleEntryLocation>();
            Entries = LocaleFile.ReadEntries(localeDatFile);
            Metadata = LocaleMetadata.Create(localeDatFile, Entries);
            CanAddEntries = canAddEntries;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="LocaleFileInfo"/> from the
        /// specified locale .dat file, locale .dir file, and optional settings.
        /// </summary>
        public LocaleFileInfo(string localeDatFile, string localeDirFile, bool canAddEntries = false)
        {
            LocaleDatFile = new(localeDatFile);
            LocaleDirFile = new(localeDirFile);
            Preamble = LocaleFile.ReadPreamble(localeDatFile);
            Metadata = LocaleFile.ReadMetadata(localeDirFile);
            Locations = LocaleFile.ReadEntryLocations(localeDirFile);

            // Some Simplified Chinese TCG locales have incorrect .dir files,
            // so use the .dat file exclusively to read entries for them.
            Entries = Metadata.IsTCG() && Metadata.Locale == Locale.zh_CN
                    ? LocaleFile.ReadEntries(localeDatFile)
                    : LocaleFile.ReadEntries(localeDatFile, localeDirFile);

            CanAddEntries = canAddEntries;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="LocaleFileInfo"/> from the specified parameters.
        /// </summary>
        private LocaleFileInfo(FileInfo localeDatFile, FileInfo localeDirFile, byte[] preamble, LocaleMetadata metadata,
                              LocaleEntryLocation[] locations, LocaleEntry[] entries)
        {
            LocaleDatFile = localeDatFile;
            LocaleDirFile = localeDirFile;
            Preamble = preamble;
            Metadata = metadata;
            Locations = locations;
            Entries = entries;
        }

        /// <summary>
        /// Gets a sorted mapping from IDs to locale entries.
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public SortedDictionary<int, LocaleEntry> IdToEntry
        {
            get
            {
                if (Metadata.IsTCG()) throw new InvalidOperationException("Cannot create IDs for a TCG locale file.");

                _idToEntry ??= Preimaging.CreateIdMapping(Entries);
                return _idToEntry;
            }
        }

        /// <summary>
        /// Gets a sorted mapping from hashes to locale entries.
        /// </summary>
        public SortedDictionary<uint, List<LocaleEntry>> HashToEntry
        {
            get
            {
                _hashToEntry ??= Preimaging.CreateHashMapping(Entries);
                return _hashToEntry;
            }
        }

        /// <summary>
        /// Gets or sets whether adding locale entries is supported.
        /// </summary>
        public bool CanAddEntries
        {
            get => _canAddEntries;
            set
            {
                // TCG locale files don't hash IDs the same way as game locale
                // files, so adding entries to TCG locale files is unsupported.
                if (value && !Metadata.IsTCG())
                {
                    _idToEntry ??= Preimaging.CreateIdMapping(Entries);
                    _hashToEntry ??= Preimaging.CreateHashMapping(Entries);
                    _unusedIds ??= Enumerable.Range(1, Preimaging.MaxId)
                                             .Where(x => !_idToEntry.ContainsKey(x))
                                             .GetEnumerator();
                    _canAddEntries = true;
                }
                else
                {
                    _canAddEntries = false;
                }
            }
        }

        /// <summary>
        /// Gets the next unused ID.
        /// </summary>
        private int NextId => _unusedIds!.MoveNext()
                            ? _unusedIds.Current
                            : throw new InvalidOperationException("No more IDs to add entries.");

        /// <summary>
        /// Adds a locale entry for each string in the specified collection to the ID/hash -> entry mappings.
        /// </summary>
        /// <remarks><inheritdoc cref="AddEntry(string)"/></remarks>
        public void AddEntries(IEnumerable<string> contents)
        {
            foreach (string text in contents)
            {
                AddEntry(text);
            }
        }

        /// <summary>
        /// Adds a locale entry with the specified text to the ID/hash -> entry mappings.
        /// </summary>
        /// <remarks>The stored entries can be written with any of the <c>WriteEntries()</c> methods.</remarks>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        public void AddEntry(string text)
        {
            if (!_canAddEntries) throw new InvalidOperationException("Adding entries is not supported.");
            if (text == null) throw new ArgumentNullException(nameof(text));

            // Generate an ID, hash, and tag for the new locale entry.
            int id = NextId;
            uint hash = Preimaging.GetHash(id);
            LocaleTag tag = text != "" ? LocaleTag.ucdt : LocaleTag.ucdn;
            LocaleEntry entry = new(hash, tag, text);

            // If the current ID's hash collides with an existing hash, find another ID.
            while (!_hashToEntry!.TryAdd(hash, new(1) { entry }))
            {
                id = NextId;
                hash = Preimaging.GetHash(id);
                entry = entry with { Hash = hash };
            }

            _idToEntry!.Add(id, entry);
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
        public LocaleFileInfo WriteEntries(string localeDatFile, string localeDirFile)
        {
            return WriteEntries(new FileInfo(localeDatFile), new FileInfo(localeDirFile));
        }

        /// <summary>
        /// Writes the stored locale entries to the specified .dat file and .dir file.
        /// </summary>
        /// <returns>
        /// A new instance of <see cref="LocaleFileInfo"/> for the specified .dat file and .dir file.
        /// </returns>
        public LocaleFileInfo WriteEntries(FileInfo localeDatFile, FileInfo localeDirFile)
        {
            LocaleEntry[] entries = HashToEntry.SelectMany(x => x.Value).ToArray();
            LocaleEntryLocation[] locations = new LocaleEntryLocation[entries.Length];

            using (StreamWriter localeDatWriter = new(localeDatFile.Open(FileMode.Create, FileAccess.Write)))
            {
                // Write the preamble bytes at the start of the .dat file.
                localeDatWriter.BaseStream.Write(Preamble);
                int offset = Preamble.Length;

                // Write each locale entry to the .dat file.
                for (int i = 0; i < entries.Length; i++)
                {
                    LocaleEntry entry = entries[i];
                    string entryString = entry.ToString();
                    int size = Encoding.UTF8.GetByteCount(entryString);

                    // Create a location for the entry.
                    locations[i] = entry.Tag switch
                    {
                        // If the tag starts with 'm', trim the size of the entry location to the last "display" byte.
                        LocaleTag.mcdt => new(entry.Hash, offset, GetMtagSize(entryString)),
                        LocaleTag.mcdn => new(entry.Hash, offset, GetMtagSize(entryString)),
                        LocaleTag.mgdt => new(entry.Hash, offset, GetMtagSize(entryString)),
                        _ => new(entry.Hash, offset, size)
                    };

                    localeDatWriter.WriteLine(entryString);
                    offset += size + NewLineLength;
                }
            }

            using (StreamWriter localeDirWriter = new(localeDirFile.Open(FileMode.Create, FileAccess.Write)))
            {
                // Update the metadata and write it at the start of the .dir file.
                LocaleMetadata metadata = Metadata.Update(localeDatFile.FullName, entries);
                localeDirWriter.Write(metadata);

                // Write each locale entry location to the .dir file.
                foreach (LocaleEntryLocation location in locations)
                {
                    localeDirWriter.WriteLine(location.ToString());
                }
            }

            return new(localeDatFile, localeDirFile, Preamble, Metadata, locations, entries);
        }

        /// <summary>
        /// Returns the size, in bytes, of the specified entry up to the pre-key section.
        /// </summary>
        /// <exception cref="FormatException"></exception>
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
}
