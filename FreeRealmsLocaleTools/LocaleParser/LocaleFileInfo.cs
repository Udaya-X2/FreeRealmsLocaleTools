using FreeRealmsLocaleTools.IdHashing;

namespace FreeRealmsLocaleTools.LocaleParser
{
    /// <summary>
    /// Provides properties and instance methods for obtaining information from Free Realms locale files.
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
        /// Gets the locations of each locale entry.
        /// </summary>
        public LocaleEntryLocation[] Locations { get; private init; }

        /// <summary>
        /// Gets the locale entries.
        /// </summary>
        public LocaleEntry[] Entries { get; private init; }

        private SortedDictionary<uint, LocaleEntry>? _idToEntry;
        private SortedDictionary<uint, List<LocaleEntry>>? _hashToEntry;
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
            Metadata = new LocaleMetadata().Populate(localeDatFile, Entries);
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
        /// Gets a sorted mapping from IDs to locale entries.
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public SortedDictionary<uint, LocaleEntry> IdToEntry
        {
            get
            {
                if (Metadata.IsTCG())
                {
                    throw new InvalidOperationException("Cannot create IDs for a TCG locale file.");
                }

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
                    _canAddEntries = true;
                }
                else
                {
                    _canAddEntries = false;
                }
            }
        }
    }
}
