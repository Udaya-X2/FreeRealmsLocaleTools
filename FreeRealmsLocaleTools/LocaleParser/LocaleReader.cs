using System.Text;

namespace FreeRealmsLocaleTools.LocaleParser
{
    /// <summary>
    /// Represents a reader that can read locale entries from a Free Realms .dat file.
    /// </summary>
    public class LocaleReader : IDisposable
    {
        private const int BufferSize = 1024;
        private const string WindowsLineEnding = "\r\n";
        private const string UnixLineEnding = "\n";
        private const string MacLineEnding = "\r";

        private static readonly byte[] UTF8Preamble1 = new byte[3] { 0xEF, 0xBB, 0xBF };
        private static readonly byte[] UTF8Preamble2 = new byte[6] { 0xC3, 0xAF, 0xC2, 0xBB, 0xC2, 0xBF };

        private readonly FileStream _stream;
        private readonly Encoding _encoding;
        private readonly Decoder _decoder;
        private readonly byte[] _byteBuffer;
        private readonly char[] _charBuffer;

        private int _byteLen;
        private int _charPos;
        private int _charLen;
        private bool _disposed;
        private string? _prevLineEnding;
        private string? _currLineEnding;
        private LocaleEntry? _currEntry;

        /// <summary>
        /// Initializes a new instance of the <see cref="LocaleReader"/> class for the specified file name.
        /// </summary>
        public LocaleReader(string path)
        {
            _stream = File.OpenRead(path);
            _encoding = Encoding.UTF8;
            _decoder = _encoding.GetDecoder();
            _byteBuffer = new byte[BufferSize];
            _charBuffer = new char[_encoding.GetMaxCharCount(BufferSize)];
            Preamble = ReadPreamble();
            _currEntry = ParseEntry(ReadLine());
        }

        /// <summary>
        /// Gets the preamble bytes at the beginning of the file.
        /// </summary>
        public byte[] Preamble { get; private init; }

        /// <summary>
        /// Returns <see langword="true"/> if the reader can read a locale entry; otherwise <see langword="false"/>.
        /// </summary>
        public bool HasEntry
        {
            get
            {
                ThrowIfDisposed();
                return _currEntry != null;
            }
        }

        /// <summary>
        /// Gets or sets the line ending of the previous string read with <see cref="ReadLine"/>.
        /// </summary>
        private string LineEnding
        {
            get => _prevLineEnding ?? _currLineEnding ?? UnixLineEnding;
            set
            {
                _prevLineEnding = _currLineEnding;
                _currLineEnding = value;
            }
        }

        /// <summary>
        /// Reads the preamble bytes at the beginning of the file.
        /// </summary>
        /// <returns>The bytes at the beginning of the file.</returns>
        /// <exception cref="InvalidDataException"></exception>
        private byte[] ReadPreamble()
        {
            _stream.Read(_byteBuffer, 0, 6);

            if (_byteBuffer.Take(3).SequenceEqual(UTF8Preamble1))
            {
                _stream.Seek(3, SeekOrigin.Begin);
                return UTF8Preamble1;
            }
            else if (_byteBuffer.Take(6).SequenceEqual(UTF8Preamble2))
            {
                return UTF8Preamble2;
            }
            else
            {
                throw new InvalidDataException($"Unrecognized preamble bytes in file '{_stream.Name}'");
            }
        }

        /// <summary>
        /// Reads the next block of bytes from the stream into the buffers.
        /// </summary>
        /// <returns>The number of chars decoded from the block of bytes.</returns>
        private int ReadBuffer()
        {
            _charPos = 0;
            _byteLen = _stream.Read(_byteBuffer, 0, BufferSize);
            _charLen = _decoder.GetChars(_byteBuffer, 0, _byteLen, _charBuffer, 0);
            return _charLen;
        }

        /// <summary>
        /// Reads the next locale entry from the file.
        /// </summary>
        /// <returns>
        /// The next locale entry from the file, or <see langword="null"/> if the end of the file is reached.
        /// </returns>
        public LocaleEntry? ReadEntry()
        {
            if (!HasEntry) return null;

            LocaleEntry prevEntry = _currEntry!;
            string? currLine = ReadLine();

            if (!TryParseEntry(currLine, out _currEntry))
            {
                prevEntry = ReadMultiLineEntry(prevEntry, currLine);
            }

            return prevEntry;
        }

        /// <summary>
        /// Reads the next line(s) of the stream into the previous locale entry and returns it.
        /// </summary>
        /// <returns>The previous locale entry with the next line(s) appended to its text.</returns>
        private LocaleEntry ReadMultiLineEntry(LocaleEntry prevEntry, string? currLine)
        {
            string text = $"{prevEntry.Text}{LineEnding}{currLine}";
            StringBuilder sb = new(text, text.Length + 80);

            while (!TryParseEntry(currLine = ReadLine(), out _currEntry))
            {
                sb.Append(LineEnding).Append(currLine);
            }

            return prevEntry with { Text = sb.ToString() };
        }

        /// <summary>
        /// Reads a line of characters from the current stream and returns the data as a string.
        /// </summary>
        /// <returns>
        /// The next line from the input stream, or <see langword="null"/> if the end of the stream is reached.
        /// </returns>
        private string? ReadLine()
        {
            if (_charPos == _charLen && ReadBuffer() == 0) return null;

            StringBuilder? sb = null;

            do
            {
                int i = _charPos;

                do
                {
                    char ch = _charBuffer[i];

                    // Note the following common line feed chars:
                    // \r\n - Windows   \n - Unix   \r - Mac
                    if (ch is '\r' or '\n')
                    {
                        string s;

                        if (sb != null)
                        {
                            sb.Append(_charBuffer, _charPos, i - _charPos);
                            s = sb.ToString();
                        }
                        else
                        {
                            s = new string(_charBuffer, _charPos, i - _charPos);
                        }

                        _charPos = i + 1;

                        // Don't add the line feed chars to the string, but record them for future use.
                        if (ch == '\r')
                        {
                            if ((_charPos < _charLen || ReadBuffer() > 0) && _charBuffer[_charPos] == '\n')
                            {
                                _charPos++;
                                LineEnding = WindowsLineEnding;
                            }
                            else
                            {
                                LineEnding = MacLineEnding;
                            }
                        }
                        else
                        {
                            LineEnding = UnixLineEnding;
                        }

                        return s;
                    }
                } while (++i < _charLen);

                i = _charLen - _charPos;
                sb ??= new StringBuilder(i + 80);
                sb.Append(_charBuffer, _charPos, i);
            } while (ReadBuffer() > 0);

            return sb.ToString();
        }

        /// <summary>
        /// Converts the specified line to its locale entry equivalent.
        /// </summary>
        /// <returns>A locale entry equivalent to the contents in <paramref name="line"/>.</returns>
        private LocaleEntry? ParseEntry(string? line)
        {
            if (!TryParseEntry(line, out LocaleEntry? entry))
            {
                throw new InvalidDataException($"Invalid locale entry in '{_stream.Name}': {line}");
            }

            return entry;
        }

        /// <summary>
        /// Tries to convert the specified line to its locale entry equivalent.
        /// A return value indicates whether the conversion succeeded or failed.
        /// </summary>
        /// <returns><see langword="true"/> if the conversion succeeded; <see langword="false"/> otherwise.</returns>
        private static bool TryParseEntry(string? line, out LocaleEntry? entry)
        {
            // To avoid issues with multi-line entries, a null entry
            // is considered a successful conversion from a null line.
            if (line == null)
            {
                entry = null;
                return true;
            }

            int hashIndex = line.IndexOf('\t');

            if (hashIndex != -1
                && uint.TryParse(line.AsSpan(0, hashIndex), out uint hash)
                && hashIndex + 5 < line.Length
                && Enum.TryParse(line.AsSpan(hashIndex + 1, 4), out LocaleTag tag))
            {
                entry = new LocaleEntry(hash, tag, line[(hashIndex + 6)..]);
                return true;
            }

            entry = null;
            return false;
        }

        /// <summary>
        /// Throws an exception if this object has been disposed.
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;
            _stream.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
