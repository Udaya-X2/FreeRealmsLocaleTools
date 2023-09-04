using System.Data;
using System.Text;
using System.Text.RegularExpressions;

namespace FreeRealmsLocaleTools.LocaleParser
{
    /// <summary>
    /// Provides static methods for obtaining information from Free Realms locale files.
    /// </summary>
    public static class LocaleFile
    {
        private const string MetadataHeader = "##"; // Chars that appear at the start of .dir metadata lines
        private const int SkipTagChars = 6; // Number of chars between the hash and locale text

        private static readonly Regex MetaRegex = new(@"^## (.*?):\t(.*)$");

        /// <summary>
        /// Opens the locale file, reads all locale entries from the file, and then closes the file.
        /// </summary>
        /// <returns>An array containing all locale entries from the specified file.</returns>
        public static LocaleEntry[] ReadEntries(string localeDatPath)
        {
            using LocaleReader reader = new(localeDatPath);
            List<LocaleEntry> localeEntries = new();
            LocaleEntry? entry;

            while ((entry = reader.ReadEntry()) != null)
            {
                localeEntries.Add(entry);
            }

            return localeEntries.ToArray();
        }

        /// <summary>
        /// Opens the locale files, reads all locale entries from the files, and then closes the files.
        /// </summary>
        /// <returns>An array containing all locale entries from the specified files.</returns>
        public static LocaleEntry[] ReadEntries(string localeDatPath, string localeDirPath)
        {
            // Read the location of each locale entry from the .dir file.
            List<LocaleEntryLocation> locations = ReadEntryLocations(localeDirPath);

            // Open the .dat file for reading.
            using FileStream stream = File.OpenRead(localeDatPath);
            byte[] buf = new byte[locations.Select(x => x.Size).Max()];
            char[] cbuf = new char[Encoding.UTF8.GetMaxCharCount(buf.Length)];
            LocaleEntry[] localeEntries = new LocaleEntry[locations.Count];

            // Look up the locale entry corresponding to each location.
            for (int i = 0; i < locations.Count; i++)
            {
                LocaleEntryLocation location = locations[i];
                localeEntries[i] = ReadEntry(stream, buf, cbuf, location);
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

            foreach (string line in File.ReadLines(localeDirPath).TakeWhile(x => x.StartsWith(MetadataHeader)))
            {
                Match match = MetaRegex.Match(line);

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
        public static List<LocaleEntryLocation> ReadEntryLocations(string localeDirPath)
        {
            return File.ReadLines(localeDirPath)
                       .SkipWhile(x => x.StartsWith(MetadataHeader))
                       .Select(x => ReadEntryLocation(x))
                       .ToList();
        }

        /// <summary>
        /// Initializes a new instance of <see cref="LocaleEntryLocation"/> by parsing the given .dir file line.
        /// </summary>
        private static LocaleEntryLocation ReadEntryLocation(string localeDirLine)
        {
            string[] components = localeDirLine.Split('\t');
            uint hash = uint.Parse(components[0]);
            int offset = int.Parse(components[1]);
            int size = int.Parse(components[2]);
            return new LocaleEntryLocation(hash, offset, size);
        }

        /// <summary>
        /// Looks up the specified locale entry in the .dat file,
        /// reads its bytes/chars into the buffers, and returns it.
        /// </summary>
        /// <returns>The locale entry at the given location.</returns>
        private static LocaleEntry ReadEntry(FileStream stream, byte[] buf, char[] cbuf, LocaleEntryLocation location)
        {
            // Read the bytes at the offset, then decode them into chars.
            stream.Seek(location.Offset, SeekOrigin.Begin);
            stream.Read(buf, 0, location.Size);
            int charLen = Encoding.UTF8.GetChars(buf, 0, location.Size, cbuf, 0);

            // Parse the tag from the locale entry.
            int startIndex = GetDigitsLength(location.Hash) + SkipTagChars;
            LocaleTag tag = Enum.Parse<LocaleTag>(new string(cbuf, startIndex - 5, 4));

            // If the tag starts with 'm', read the leftover chars into the buffer.
            if (tag is LocaleTag.mcdt or LocaleTag.mcdn or LocaleTag.mgdt)
            {
                charLen = ReadLine(stream, cbuf, charLen);
            }

            return new LocaleEntry(location.Hash, tag, new(cbuf, startIndex, charLen - startIndex));
        }

        /// <summary>
        /// Returns the number of digits in the given 32-bit unsigned integer.
        /// </summary>
        private static int GetDigitsLength(uint n) => (int)Math.Log10(Math.Max(n, 1u)) + 1;

        /// <summary>
        /// Reads a line of characters from the stream into the buffer, starting from <paramref name="pos"/>.
        /// </summary>
        /// <returns>The new buffer position.</returns>
        private static int ReadLine(FileStream stream, char[] cbuf, int pos)
        {
            int b;

            while ((b = stream.ReadByte()) is not ('\r' or '\n'))
            {
                cbuf[pos++] = (char)b;
            }

            return pos;
        }
    }
}
