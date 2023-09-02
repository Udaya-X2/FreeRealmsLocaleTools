using System.Data;
using System.Text;
using System.Text.RegularExpressions;

namespace FreeRealmsLocaleTools.LocaleParser
{
    /// <summary>
    /// Provides static methods for obtaining information from Free Realms locale files.
    /// </summary>
    public static class LocaleReader
    {
        private const string MetadataHeader = "##"; // Chars that appear at the start of .dir metadata lines
        private const int SkipTagChars = 6; // Number of chars between the hash and locale text

        private static readonly Decoder UTF8Decoder = Encoding.UTF8.GetDecoder();
        private static readonly Regex MetaRegex = new(@"^## (.*?):\t(.*)$");
        private static readonly Regex McdRegex = new(@"^\t0017\tGlobal\.Text\.(\d+)$");

        /// <summary>
        /// Opens the locale file, reads all locale entries from the file, and then closes the file.
        /// </summary>
        /// <returns>An array containing all locale entries from the file.</returns>
        public static LocaleEntry[] ReadEntries(string localeDatPath)
        {
            using StreamReader reader = new(localeDatPath);
            byte[] byteOrderMark = ReadByteOrderMark(reader.BaseStream);
            List<LocaleEntry> localeEntries = new();
            string? line;

            while ((line = reader.ReadLine()) != null)
            {
                if (TryParseEntry(line, out LocaleEntry entry))
                {
                    localeEntries.Add(entry);
                }
                else
                {
                    localeEntries[^1].Text += $"\n{line}";
                }
            }

            return localeEntries.ToArray();
        }

        /// <summary>
        /// Opens the locale files, reads all locale entries from the files, and then closes the files.
        /// </summary>
        /// <returns>An array containing all locale entries from the files.</returns>
        public static LocaleEntry[] ReadEntries(string localeDatPath, string localeDirPath)
        {
            // Read the location of each locale entry from the .dir file.
            List<LocaleEntryLocation> locations = ReadEntryLocations(localeDirPath);

            // Open the .dat file for reading.
            using FileStream stream = File.OpenRead(localeDatPath);
            byte[] buf = new byte[locations.Select(x => x.Size).Max()];
            char[] cbuf = new char[buf.Length];
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
        /// Reads the byte order mark from the start of the stream.
        /// </summary>
        /// <returns>A byte array containing the byte order mark.</returns>
        private static byte[] ReadByteOrderMark(Stream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);
            List<byte> byteOrderMark = new();
            int b;

            // Locale entries always start with hashes, so read all non-digit bytes.
            while ((b = stream.ReadByte()) is < '0' or > '9')
            {
                byteOrderMark.Add((byte)b);
            }

            // Rewind to the last non-digit byte.
            stream.Seek(-1, SeekOrigin.Current);
            return byteOrderMark.ToArray();
        }

        /// <summary>
        /// Tries to convert the specified line to its locale entry equivalent.
        /// A return value indicates whether the conversion succeeded or failed.
        /// </summary>
        /// <returns><see langword="true"/> if the conversion succeeded; <see langword="false"/> otherwise.</returns>
        private static bool TryParseEntry(string line, out LocaleEntry entry)
        {
            int hashIndex = line.IndexOf('\t');

            if (hashIndex != -1
                && uint.TryParse(line.AsSpan(0, hashIndex), out uint hash)
                && hashIndex + 5 < line.Length
                && Enum.TryParse(line.AsSpan(hashIndex + 1, 4), out LocaleTag tag))
            {
                entry = new LocaleEntry
                {
                    Hash = hash,
                    Tag = tag,
                    Text = line[(hashIndex + 6)..]
                };
                return true;
            }

            entry = null!;
            return false;
        }

        /// <summary>
        /// Reads the location of each locale entry from the specified .dir file, and returns them in an array.
        /// </summary>
        /// <returns>An array of locale entry locations.</returns>
        private static List<LocaleEntryLocation> ReadEntryLocations(string localeDirPath)
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
            int charLen = UTF8Decoder.GetChars(buf, 0, location.Size, cbuf, 0);

            // Skip the chars before the text portion of the locale entry.
            int startIndex = GetDigitsLength(location.Hash) + SkipTagChars;
            LocaleTag tag = Enum.Parse<LocaleTag>(new string(cbuf, startIndex - 5, 4));
            string text;
            int id;

            switch (tag)
            {
                // If the tag starts with "mcd", add the leftover chars to the text and initialize the ID.
                case LocaleTag.mcdt:
                case LocaleTag.mcdn:
                    text = ReadLine(stream, cbuf, startIndex, charLen);
                    startIndex = charLen - startIndex;
                    id = int.Parse(McdRegex.Match(text, startIndex, text.Length - startIndex).Groups[1].Value);
                    break;
                // If the tag starts with "m", add the leftover chars to the text.
                case LocaleTag.mgdt:
                    text = ReadLine(stream, cbuf, startIndex, charLen);
                    id = -1;
                    break;
                // For all other tags, use the buffered chars for the text.
                default:
                    text = new(cbuf, startIndex, charLen - startIndex);
                    id = -1;
                    break;
            }

            return new LocaleEntry { Id = id, Hash = location.Hash, Tag = tag, Text = text };
        }

        /// <summary>
        /// Reads a line of characters from the stream into the buffer, starting from <paramref name="pos"/>.
        /// </summary>
        /// <returns>The next line from the input stream, starting from <paramref name="startIndex"/>.</returns>
        private static string ReadLine(FileStream stream, char[] cbuf, int startIndex, int pos)
        {
            int b;

            while ((b = stream.ReadByte()) is not ('\r' or '\n'))
            {
                cbuf[pos++] = (char)b;
            }

            return new string(cbuf, startIndex, pos - startIndex);
        }

        /// <summary>
        /// Returns the number of digits in the given 32-bit unsigned integer.
        /// </summary>
        private static int GetDigitsLength(uint n) => (int)Math.Log10(Math.Max(n, 1u)) + 1;
    }
}
