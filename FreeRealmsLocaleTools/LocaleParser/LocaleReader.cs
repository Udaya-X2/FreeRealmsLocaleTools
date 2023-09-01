using System.Data;
using System.Text.RegularExpressions;

namespace FreeRealmsLocaleTools.LocaleParser
{
    /// <summary>
    /// Provides static methods for obtaining information from Free Realms locale files.
    /// </summary>
    public static class LocaleReader
    {
        private const string MetadataHeader = "##"; // Chars that appear at the start of .dir metadata lines
        private const uint SkipTagChars = 6u; // Number of chars between the hash and locale text
        private const uint SkipMcdChars = 18u; // Number of chars between the locale text and ID

        /// <summary>
        /// Opens the locale files, reads all locale entries from the files, and then closes the files.
        /// </summary>
        /// <returns>An array containing all locale entries from the files.</returns>
        public static LocaleEntry[] ReadEntries(string localeDatPath, string localeDirPath)
        {
            // Read the location of each locale entry from the .dir file.
            List<LocaleEntryLocation> locations = ReadEntryLocations(localeDirPath);

            // Open the .dat file for reading.
            using FileStream localeDatStream = File.OpenRead(localeDatPath);
            using BinaryReader datReader = new(localeDatStream);
            char[] buffer = new char[locations.Select(x => x.Size).Max()];
            LocaleEntry[] localeEntries = new LocaleEntry[locations.Count];

            // Look up the locale entry corresponding to each location.
            for (int i = 0; i < locations.Count; i++)
            {
                LocaleEntryLocation location = locations[i];
                localeEntries[i] = LookupLocaleEntry(datReader, buffer, location);
            }

            return localeEntries;
        }

        /// <summary>
        /// Reads the metadata lines from the specified .dir file, and returns it as a serialized object.
        /// </summary>
        /// <returns>The metadata from the specified .dir file.</returns>
        public static LocaleMetadata ReadMetadata(string localeDirPath)
        {
            LocaleMetadata metadata = new();
            Regex metaRegex = new(@"^## (.*?):\t(.*)$");

            foreach (string line in File.ReadLines(localeDirPath).TakeWhile(x => x.StartsWith(MetadataHeader)))
            {
                Match match = metaRegex.Match(line);

                if (match.Success)
                {
                    string name = match.Groups[1].Value;
                    string value = match.Groups[2].Value;
                    metadata.SetProperty(name, value);
                }
            }

            return metadata;
        }

        /// <summary>
        /// Reads the location of each locale entry from the specified .dir file, and returns them in an array.
        /// </summary>
        /// <returns>An array of locale entry locations.</returns>
        private static List<LocaleEntryLocation> ReadEntryLocations(string localeDirPath)
        {
            return File.ReadLines(localeDirPath)
                       .SkipWhile(x => x.StartsWith(MetadataHeader))
                       .Select(x => ReadLocaleEntryLocation(x))
                       .ToList();
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

            // Initialize the ID if the locale tag starts with mcd.
            return new LocaleEntry
            {
                Id = tag is LocaleTag.mcdt or LocaleTag.mcdn ? ReadMcdEntryId(reader, buffer) : null,
                Hash = location.Hash,
                Tag = tag,
                Text = text
            };
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
