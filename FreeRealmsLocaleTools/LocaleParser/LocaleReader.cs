using FreeRealmsLocaleTools.IdHashing;

namespace FreeRealmsLocaleTools.LocaleParser
{
    /// <summary>
    /// Provides static methods for obtaining information from Free Realms locale files.
    /// </summary>
    public static class LocaleReader
    {
        private const uint SkipTagChars = 6u; // Number of chars between the hash and locale text
        private const uint SkipMcdChars = 18u; // Number of chars between the locale text and ID

        /// <summary>
        /// Reads the specified locale files, assigns an ID to each locale entry, and returns the set of entries.
        /// </summary>
        /// <returns>A sorted set of locale entries, ordered by ID number.</returns>
        public static SortedSet<LocaleEntry> ReadEntries(string localeDatPath, string localeDirPath)
        {
            return Preimaging.ToLocaleEntryIdSet(ReadMappedEntries(localeDatPath, localeDirPath));
        }

        /// <summary>
        /// Reads the specified locale files and returns a mapping from hash to locale entry.
        /// </summary>
        /// <returns>A mapping from hashes to locale entries.</returns>
        public static Dictionary<uint, LocaleEntry[]> ReadMappedEntries(string localeDatPath, string localeDirPath)
        {
            // Read the location of each locale entry from the .dir file.
            IEnumerable<string> localeDirLines = File.ReadLines(localeDirPath).SkipWhile(x => x.StartsWith("##"));
            LocaleEntryLocation[] locations = localeDirLines.Select(x => ReadLocaleEntryLocation(x)).ToArray();

            // Open the .dat file for reading.
            using FileStream localeDatStream = File.OpenRead(localeDatPath);
            using BinaryReader datReader = new(localeDatStream);
            char[] buffer = new char[locations.Select(x => x.Size).Max()];
            Dictionary<uint, LocaleEntry[]> hashToLocaleEntry = new();

            // Look up the locale entry corresponding to each location.
            foreach (LocaleEntryLocation location in locations)
            {
                LocaleEntry localeEntry = LookupLocaleEntry(datReader, buffer, location);

                switch (localeEntry.Tag)
                {
                    // If the locale tag cannot be hashed by "Global.Text.<id>", skip the locale entry.
                    case LocaleTag.ugdt:
                    case LocaleTag.ugdn:
                        break;
                    // If the locale tag indicates a hash collision, map the hash to the locale entries.
                    case LocaleTag.mcdt:
                    case LocaleTag.mcdn:
                        localeEntry.Id = ReadMcdEntryId(datReader, buffer);

                        // Hash collisions are small and infrequent, so array resizing is feasible here.
                        if (hashToLocaleEntry.TryGetValue(location.Hash, out LocaleEntry[]? entries))
                        {
                            Array.Resize(ref entries, entries.Length + 1);
                            entries[^1] = localeEntry;
                            hashToLocaleEntry[location.Hash] = entries;
                            break;
                        }

                        goto default;
                    // Otherwise, map the hash to the locale entry.
                    default:
                        hashToLocaleEntry[location.Hash] = new LocaleEntry[] { localeEntry };
                        break;
                }
            }

            return hashToLocaleEntry;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="LocaleEntryLocation"/> by parsing the given .dir file line.
        /// </summary>
        private static LocaleEntryLocation ReadLocaleEntryLocation(string localeDirLine)
        {
            string[] components = localeDirLine.Split('\t');
            uint hash = uint.Parse(components[0]);
            uint offset = uint.Parse(components[1]);
            uint size = uint.Parse(components[2]);
            return new LocaleEntryLocation(hash, offset, size);
        }

        /// <summary>
        /// Looks up the specified locale entry in the .dat file, reads its chars into the buffer, and returns it.
        /// </summary>
        /// <returns>The locale entry at the given location.</returns>
        private static LocaleEntry LookupLocaleEntry(BinaryReader reader, char[] buffer, LocaleEntryLocation location)
        {
            reader.BaseStream.Seek(location.Offset, SeekOrigin.Begin);
            reader.Read(buffer, 0, (int)location.Size);

            // Skip the chars before the text portion of the locale entry.
            uint startIndex = GetDigitsLength(location.Hash) + SkipTagChars;
            LocaleTag tag = Enum.Parse<LocaleTag>(new string(buffer, (int)startIndex - 5, 4));
            string text = new(buffer, (int)startIndex, (int)(location.Size - startIndex));
            return new LocaleEntry { Tag = tag, Text = text };
        }

        /// <summary>
        /// Returns the number of digits in the given 32-bit unsigned integer.
        /// </summary>
        private static uint GetDigitsLength(uint number) => (uint)Math.Log10(Math.Max(number, 1)) + 1u;

        /// <summary>
        /// Reads the ID of the current locale entry into the buffer and returns its numerical value.
        /// <para/>
        /// The current locale entry must have the tag <see cref="LocaleTag.mcdt"/> or <see cref="LocaleTag.mcdn"/>.
        /// </summary>
        /// <returns>The ID of the current locale entry in the .dat file.</returns>
        private static uint ReadMcdEntryId(BinaryReader reader, char[] buffer)
        {
            reader.BaseStream.Seek(SkipMcdChars, SeekOrigin.Current);
            int digits = 0;

            for (char c = reader.ReadChar(); '0' <= c && c <= '9'; c = reader.ReadChar())
            {
                buffer[digits++] = c;
            }

            return uint.Parse(buffer.AsSpan(0, digits));
        }
    }
}
