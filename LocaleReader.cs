namespace FreeRealmsLocaleTools
{
    /// <summary>
    /// Provides static methods for obtaining locale information in a Free Realms client directory.
    /// </summary>
    public static class LocaleReader
    {
        private const uint SkipTagChars = 6u; // Number of chars between the hash and locale text
        private const uint SkipMcdChars = 18u; // Number of chars between the locale text and ID

        /// <summary>
        /// Reads the client directory's locale files and returns a set of locale text entries with IDs initialized.
        /// </summary>
        /// <returns>A sorted set of locale text entries with IDs initialized.</returns>
        public static SortedSet<LocaleText> ReadLocaleIds(string localeDatPath, string localeDirPath)
        {
            return Preimaging.CreateLocaleTextIdsWith(ReadLocaleEntries(localeDatPath, localeDirPath));
        }

        /// <summary>
        /// Reads the client directory's locale files and returns a mapping from hash to locale text entries.
        /// </summary>
        /// <returns>A mapping from hashes to locale text entries.</returns>
        public static Dictionary<uint, LocaleText[]> ReadLocaleEntries(string localeDatPath, string localeDirPath)
        {
            // Read each locale entry from the .dir file.
            IEnumerable<string> localeDirLines = File.ReadLines(localeDirPath).SkipWhile(x => x.StartsWith("##"));
            LocaleEntry[] localeEntries = localeDirLines.Select(x => ParseLocaleEntry(x)).ToArray();

            // Open the .dat file for reading.
            using FileStream localeDatStream = File.OpenRead(localeDatPath);
            using BinaryReader datReader = new(localeDatStream);
            char[] buffer = new char[localeEntries.Select(x => x.Size).Max()];
            Dictionary<uint, LocaleText[]> hashToLocaleText = new();

            // Look up the locale text corresponding to each locale entry.
            foreach (LocaleEntry entry in localeEntries)
            {
                LocaleText localeText = LookupText(datReader, buffer, entry);

                switch (localeText.Tag)
                {
                    // If the locale tag cannot be hashed by "Global.Text.<id>", skip the locale entry.
                    case LocaleTag.ugdt:
                    case LocaleTag.ugdn:
                        break;
                    // If the locale tag indicates a hash collision, create a mapping from hash to locale text values.
                    case LocaleTag.mcdt:
                    case LocaleTag.mcdn:
                        LocaleText[] texts = hashToLocaleText.GetValueOrDefault(entry.Hash, Array.Empty<LocaleText>());
                        hashToLocaleText[entry.Hash] = texts.Append(localeText).ToArray();
                        localeText.Id = GetTextId(datReader, buffer);
                        break;
                    // Otherwise, create a mapping from hash to locale text.
                    default:
                        hashToLocaleText[entry.Hash] = new LocaleText[] { localeText };
                        break;
                }
            }

            return hashToLocaleText;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="LocaleEntry"/> by parsing the given .dir file line.
        /// </summary>
        private static LocaleEntry ParseLocaleEntry(string localeDirLine)
        {
            string[] components = localeDirLine.Split('\t');
            uint hash = uint.Parse(components[0]);
            uint offset = uint.Parse(components[1]);
            uint size = uint.Parse(components[2]);
            return new LocaleEntry(hash, offset, size);
        }

        /// <summary>
        /// Looks up the specified locale entry in the .dat file, reads
        /// its chars into the buffer, and returns the locale text.
        /// </summary>
        /// <returns>The text corresponding to the given locale entry.</returns>
        private static LocaleText LookupText(BinaryReader datReader, char[] buffer, LocaleEntry entry)
        {
            datReader.BaseStream.Seek(entry.Offset, SeekOrigin.Begin);
            datReader.Read(buffer, 0, (int)entry.Size);

            // Skip the chars before the text portion of the locale entry.
            uint startIndex = GetDigitsLength(entry.Hash) + SkipTagChars;
            LocaleTag tag = Enum.Parse<LocaleTag>(new string(buffer, (int)startIndex - 5, 4));
            string text = new(buffer, (int)startIndex, (int)(entry.Size - startIndex));
            return new LocaleText { Tag = tag, Text = text };
        }

        /// <summary>
        /// Returns the number of digits in the given 32-bit unsigned integer.
        /// </summary>
        private static uint GetDigitsLength(uint number) => (uint)Math.Log10(Math.Max(number, 1)) + 1u;

        /// <summary>
        /// Reads the ID of the current locale text from the .dat file into the buffer and returns its numerical value.
        /// <para/>
        /// The current locale text must have the tag <see cref="LocaleTag.mcdt"/> or <see cref="LocaleTag.mcdn"/>.
        /// </summary>
        /// <returns>The ID of the current locale text in the .dat file.</returns>
        private static uint GetTextId(BinaryReader datReader, char[] buffer)
        {
            datReader.BaseStream.Seek(SkipMcdChars, SeekOrigin.Current);
            int digits = 0;

            for (char c = datReader.ReadChar(); '0' <= c && c <= '9'; c = datReader.ReadChar())
            {
                buffer[digits++] = c;
            }

            return uint.Parse(buffer.AsSpan(0, digits));
        }
    }
}
