using System.Text;

namespace FreeRealmsLocaleTools.LocaleParser
{
    /// <summary>
    /// Implements a <see cref="TextReader"/> that reads characters from a file stream in UTF-8 encoding.
    /// </summary>
    /// <remarks>
    /// This class is similar to <see cref="StreamReader"/>, but keeps information on line endings and preamble data.
    /// </remarks>
    internal class FileReader : TextReader
    {
        private const int BufferSize = 1024;
        private const string DosLineEnding = "\r\n";
        private const string MacLineEnding = "\r";
        private const string UnixLineEnding = "\n";

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
        private string? _currentLineEnding;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileReader"/> class for the specified file name.
        /// </summary>
        public FileReader(string path)
        {
            _stream = File.OpenRead(path);
            _encoding = Encoding.UTF8;
            _decoder = _encoding.GetDecoder();
            _byteBuffer = new byte[BufferSize];
            _charBuffer = new char[_encoding.GetMaxCharCount(BufferSize)];
            _currentLineEnding = UnixLineEnding;
            Preamble = ReadPreamble();
        }

        /// <summary>
        /// The preamble bytes at the beginning of the file.
        /// </summary>
        public byte[] Preamble { get; init; }

        /// <summary>
        /// The line ending of the previous string read with <see cref="ReadLine"/>,
        /// or <see langword="null"/> if two or more lines haven't been read yet.
        /// </summary>
        public string? PreviousLineEnding { get; private set; }

        /// <summary>
        /// The line ending of the previous string read with <see cref="ReadLine"/>,
        /// or <see langword="null"/> if no lines have been read yet.
        /// </summary>
        public string? CurrentLineEnding
        {
            get => _currentLineEnding;
            private set
            {
                PreviousLineEnding = _currentLineEnding;
                _currentLineEnding = value;
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
        /// <returns>The number of chars read.</returns>
        private int ReadBuffer()
        {
            _charPos = 0;
            _byteLen = _stream.Read(_byteBuffer, 0, BufferSize);
            _charLen = _decoder.GetChars(_byteBuffer, 0, _byteLen, _charBuffer, 0);
            return _charLen;
        }

        public override string? ReadLine()
        {
            ThrowIfDisposed();

            if (_charPos == _charLen && ReadBuffer() == 0)
            {
                return null;
            }

            StringBuilder? sb = null;
            do
            {
                int i = _charPos;

                do
                {
                    char ch = _charBuffer[i];

                    // Note the following common line feed chars:
                    // \n - UNIX   \r\n - DOS   \r - Mac
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

                        if (ch == '\r')
                        {
                            if ((_charPos < _charLen || ReadBuffer() > 0) && _charBuffer[_charPos] == '\n')
                            {
                                _charPos++;
                                CurrentLineEnding = DosLineEnding;
                            }
                            else
                            {
                                CurrentLineEnding = MacLineEnding;
                            }
                        }
                        else
                        {
                            CurrentLineEnding = UnixLineEnding;
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

        public override int Peek()
        {
            ThrowIfDisposed();

            if (_charPos == _charLen && ReadBuffer() == 0)
            {
                return -1;
            }

            return _charBuffer[_charPos];
        }

        public override int Read()
        {
            ThrowIfDisposed();

            if (_charPos == _charLen && ReadBuffer() == 0)
            {
                return -1;
            }

            return _charBuffer[_charPos++];
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

        protected override void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _stream.Dispose();
        }
    }
}
