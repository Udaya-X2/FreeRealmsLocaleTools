﻿using System.Data;
using System.Text;
using System.Text.RegularExpressions;

namespace FreeRealmsLocaleTools.LocaleParser;

/// <summary>
/// Provides static methods for reading and writing Free Realms locale files.
/// </summary>
public static partial class LocaleFile
{
    internal const int SkipTagChars = 6;         // Number of chars between the hash and locale text
    internal const string MetadataHeader = "##"; // Chars that appear at the start of .dir metadata lines
    internal const int PreambleSize = 6;         // Maximum preamble size

    internal static readonly byte[] UTF8Preamble1 = [0xEF, 0xBB, 0xBF];
    internal static readonly byte[] UTF8Preamble2 = [0xC3, 0xAF, 0xC2, 0xBB, 0xC2, 0xBF];

    [GeneratedRegex(@"^## (.*?):\t(.*)$")]
    private static partial Regex MetaRegex();

    /// <summary>
    /// Opens the locale file, reads all locale entries from the file, and then closes the file.
    /// </summary>
    /// <param name="localeDatPath">The path to the locale .dat file.</param>
    /// <returns>An array containing all locale entries from the specified file.</returns>
    /// <exception cref="ArgumentNullException"/>
    public static LocaleEntry[] ReadEntries(string localeDatPath)
    {
        ArgumentNullException.ThrowIfNull(localeDatPath, nameof(localeDatPath));

        using LocaleReader reader = new(localeDatPath);
        return [.. reader.ReadToEnd()];
    }

    /// <summary>
    /// Opens the locale files, reads all locale entries from the files, and then closes the files.
    /// </summary>
    /// <param name="localeDatPath">The path to the locale .dat file.</param>
    /// <param name="localeDirPath">The path to the locale .dir file.</param>
    /// <param name="options">Specifies how to parse locale entries from the .dat/.dir file.</param>
    /// <returns>An array containing all locale entries from the specified files.</returns>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="InvalidDataException"/>
    public static LocaleEntry[] ReadEntries(string localeDatPath,
                                            string localeDirPath,
                                            ParseOptions options = ParseOptions.Normal)
    {
        ArgumentNullException.ThrowIfNull(localeDatPath, nameof(localeDatPath));
        ArgumentNullException.ThrowIfNull(localeDirPath, nameof(localeDirPath));

        if (options == ParseOptions.Normal)
        {
            LocaleMetadata? metadata;

            try
            {
                metadata = ReadMetadata(localeDirPath);
            }
            catch
            {
                metadata = null;
            }

            // Simplified Chinese TCG locales have incorrect .dir files,
            // so use the .dat file exclusively to read entries for them.
            if (metadata?.Locale == Locale.zh_CN && metadata.IsTcg())
            {
                return ReadEntries(localeDatPath);
            }
        }

        try
        {
            // Read the location of each locale entry from the .dir file.
            return ReadEntries(localeDatPath, ReadEntryLocations(localeDirPath));
        }
        catch
        {
            // Upon failure, read each locale entry from the .dat file.
            if (options == ParseOptions.Lenient)
            {
                return ReadEntries(localeDatPath);
            }

            throw;
        }
    }

    /// <summary>
    /// Opens the locale file, reads all locale entries specified
    /// in <paramref name="locations"/>, and then closes the file.
    /// </summary>
    /// <param name="localeDatPath">The path to the locale .dat file.</param>
    /// <param name="locations">
    /// The collection of locale entry locations to read from <paramref name="localeDatPath"/>.
    /// </param>
    /// <returns>An array containing all locale entries specified in <paramref name="locations"/>.</returns>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="InvalidDataException"/>
    public static LocaleEntry[] ReadEntries(string localeDatPath, IEnumerable<LocaleEntryLocation> locations)
    {
        ArgumentNullException.ThrowIfNull(localeDatPath, nameof(localeDatPath));
        ArgumentNullException.ThrowIfNull(locations, nameof(locations));

        // Open the .dat file for reading.
        using FileStream stream = File.OpenRead(localeDatPath);
        byte[] buf = new byte[locations.MaxOrDefault(x => x.Size)];
        char[] cbuf = new char[Encoding.UTF8.GetMaxCharCount(buf.Length)];
        LocaleEntry[] entries = new LocaleEntry[locations.Count()];
        int entry = 0;

        // Look up the locale entry corresponding to each location.
        foreach (LocaleEntryLocation location in locations)
        {
            entries[entry++] = ReadEntry(stream, buf, cbuf, location);
        }

        return entries;
    }

    /// <summary>
    /// Reads the preamble bytes at the beginning of the specified locale .dat file.
    /// </summary>
    /// <param name="localeDatPath">The path to the locale .dat file.</param>
    /// <returns>The bytes at the beginning of the locale .dat file.</returns>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="InvalidDataException"/>
    public static ReadOnlySpan<byte> ReadPreamble(string localeDatPath)
    {
        ArgumentNullException.ThrowIfNull(localeDatPath, nameof(localeDatPath));

        using FileStream stream = new(localeDatPath, FileMode.Open, FileAccess.Read, FileShare.Read, PreambleSize);
        return ReadPreamble(stream);
    }

    /// <summary>
    /// Reads the preamble bytes at the beginning of the specified stream.
    /// </summary>
    /// <param name="stream">The stream to read.</param>
    /// <returns>The bytes at the beginning of the stream.</returns>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="InvalidDataException"/>
    public static ReadOnlySpan<byte> ReadPreamble(FileStream stream)
    {
        ArgumentNullException.ThrowIfNull(stream, nameof(stream));

        Span<byte> buffer = stackalloc byte[PreambleSize];
        stream.ReadExactly(buffer);

        if (buffer[..3].SequenceEqual(UTF8Preamble1))
        {
            stream.Seek(3, SeekOrigin.Begin);
            return UTF8Preamble1;
        }
        else if (buffer.SequenceEqual(UTF8Preamble2))
        {
            return UTF8Preamble2;
        }
        else
        {
            throw new InvalidDataException($"Unrecognized preamble bytes in file '{stream.Name}'");
        }
    }

    /// <summary>
    /// Reads the metadata lines from the specified .dir file.
    /// </summary>
    /// <param name="localeDirPath">The path to the locale .dir file.</param>
    /// <returns>The metadata from the specified .dir file.</returns>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="InvalidDataException"/>
    public static LocaleMetadata ReadMetadata(string localeDirPath)
    {
        ArgumentNullException.ThrowIfNull(localeDirPath, nameof(localeDirPath));

        LocaleMetadata metadata = new();

        foreach (string line in File.ReadLines(localeDirPath).TakeWhile(x => x.StartsWith(MetadataHeader)))
        {
            Match match = MetaRegex().Match(line);

            if (match.Success)
            {
                try
                {
                    string name = match.Groups[1].Value;
                    string value = match.Groups[2].Value;
                    metadata.SetProperty(name, value);
                }
                catch (Exception ex)
                {
                    throw new InvalidDataException($"Invalid metadata line: {line}", ex);
                }
            }
        }

        return metadata;
    }

    /// <summary>
    /// Reads the location of each locale entry from the specified .dir file, and returns them in an array.
    /// </summary>
    /// <param name="localeDirPath">The path to the locale .dir file.</param>
    /// <returns>An array of locale entry locations.</returns>
    /// <exception cref="ArgumentNullException"/>
    public static LocaleEntryLocation[] ReadEntryLocations(string localeDirPath)
        => [.. File.ReadLines(localeDirPath ?? throw new ArgumentNullException(nameof(localeDirPath)))
                   .SkipWhile(x => x.StartsWith(MetadataHeader))
                   .Select(LocaleEntryLocation.Parse)];

    /// <summary>
    /// Adds the specified collection of strings as locale entries to the given .dat file and .dir file.
    /// </summary>
    /// <param name="localeDatPath">The path to the locale .dat file.</param>
    /// <param name="localeDirPath">The path to the locale .dir file.</param>
    /// <param name="strings">The collection of strings to add as entries.</param>
    /// <returns>A <see cref="LocaleFileInfo"/> instance that wraps the locale files.</returns>
    /// <exception cref="ArgumentNullException"/>
    public static LocaleFileInfo AddEntries(string localeDatPath, string localeDirPath, IEnumerable<string> strings)
    {
        ArgumentNullException.ThrowIfNull(localeDatPath, nameof(localeDatPath));
        ArgumentNullException.ThrowIfNull(localeDirPath, nameof(localeDirPath));
        ArgumentNullException.ThrowIfNull(strings, nameof(strings));

        LocaleFileInfo localeFile = new(localeDatPath, localeDirPath);
        localeFile.AddEntries(strings);
        return localeFile.WriteEntries();
    }

    /// <summary>
    /// Replaces the text of all locale entries matching the predicate with
    /// the specified selector text from the given .dat file and .dir file.
    /// </summary>
    /// <param name="localeDatPath">The path to the locale .dat file.</param>
    /// <param name="localeDirPath">The path to the locale .dir file.</param>
    /// <param name="predicate">A function to test each locale entry for a condition.</param>
    /// <param name="selector">A transform function to apply to matching entries' text.</param>
    /// <returns>A <see cref="LocaleFileInfo"/> instance that wraps the locale files.</returns>
    /// <exception cref="ArgumentNullException"/>
    public static LocaleFileInfo UpdateEntries(string localeDatPath,
                                               string localeDirPath,
                                               Func<LocaleEntry, bool> predicate,
                                               Func<LocaleEntry, string> selector)
    {
        ArgumentNullException.ThrowIfNull(localeDatPath, nameof(localeDatPath));
        ArgumentNullException.ThrowIfNull(localeDirPath, nameof(localeDirPath));
        ArgumentNullException.ThrowIfNull(predicate, nameof(predicate));
        ArgumentNullException.ThrowIfNull(selector, nameof(selector));

        LocaleFileInfo localeFile = new(localeDatPath, localeDirPath);
        localeFile.UpdateEntries(predicate, selector);
        return localeFile.WriteEntries();
    }

    /// <summary>
    /// Removes all locale entries that match the specified predicate from the given .dat file and .dir file.
    /// </summary>
    /// <param name="localeDatPath">The path to the locale .dat file.</param>
    /// <param name="localeDirPath">The path to the locale .dir file.</param>
    /// <param name="predicate">A function to test each locale entry for a condition.</param>
    /// <returns>A <see cref="LocaleFileInfo"/> instance that wraps the locale files.</returns>
    /// <exception cref="ArgumentNullException"/>
    public static LocaleFileInfo RemoveEntries(string localeDatPath,
                                               string localeDirPath,
                                               Func<LocaleEntry, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(localeDatPath, nameof(localeDatPath));
        ArgumentNullException.ThrowIfNull(localeDirPath, nameof(localeDirPath));
        ArgumentNullException.ThrowIfNull(predicate, nameof(predicate));

        LocaleFileInfo localeFile = new(localeDatPath, localeDirPath);
        localeFile.RemoveEntries(predicate);
        return localeFile.WriteEntries();
    }

    /// <summary>
    /// Looks up the specified locale entry in the .dat file,
    /// reads its bytes/chars into the buffers, and returns it.
    /// </summary>
    /// <returns>The locale entry at the given location.</returns>
    /// <exception cref="InvalidDataException"/>
    private static LocaleEntry ReadEntry(FileStream stream, byte[] buf, char[] cbuf, LocaleEntryLocation location)
    {
        try
        {
            // Read the bytes at the offset, then decode them into chars.
            stream.Seek(location.Offset, SeekOrigin.Begin);
            stream.Read(buf, 0, location.Size);
            int charLen = Encoding.UTF8.GetChars(buf, 0, location.Size, cbuf, 0);

            // Parse the tag from the locale entry.
            int startIndex = GetDigitsLength(location.Hash) + SkipTagChars;
            LocaleTag tag = Enum.Parse<LocaleTag>(new string(cbuf, startIndex - 5, 4));

            // If the tag starts with 'm', read the leftover chars into the buffer.
            if (tag.IsMtag())
            {
                charLen = ReadLine(stream, cbuf, charLen);
            }

            return new LocaleEntry(location.Hash, tag, new(cbuf, startIndex, charLen - startIndex));
        }
        catch (Exception ex)
        {
            string locString = $"Hash = {location.Hash}, Offset = {location.Offset}, Size = {location.Size}";
            string message = $"Failed to read locale entry at {{{locString}}} in file '{stream.Name}'";
            throw new InvalidDataException(message, ex);
        }
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
