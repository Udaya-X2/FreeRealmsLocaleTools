using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace FreeRealmsLocaleTools.LocaleParser;

/// <summary>
/// Provides properties and instance methods to store metadata from a Free Realms .dir file.
/// </summary>
public record LocaleMetadata()
{
    private const string LocaleDateFormat = "ddd MMM dd HH:mm:ss '{0}' yyyy";
    private const int LocaleDateLength = 28;
    private const int LocaleFileNameLength = 14;
    private const int LocaleCodeLength = 5;

    /// <summary>
    /// Gets or sets the CID length.
    /// </summary>
    public int? CidLength { get; set; }

    /// <summary>
    /// Gets or sets the number of locale entries.
    /// </summary>
    public int? Count { get; set; }

    /// <summary>
    /// Gets or sets the database URL.
    /// </summary>
    public string? Database { get; set; }

    /// <summary>
    /// Gets or sets the date, which is typically in "ddd MMM dd HH:mm:ss zzz yyyy" format.
    /// <br/>For example, <c>Thu Mar 13 10:10:13 PDT 2014</c>.
    /// </summary>
    public string? Date { get; set; }

    /// <summary>
    /// Gets or sets the game.
    /// </summary>
    public Game? Game { get; set; }

    /// <summary>
    /// Gets or sets the locale.
    /// </summary>
    public Locale? Locale { get; set; }

    /// <summary>
    /// Gets or sets the MD5 checksum of the associated locale .dat file.
    /// </summary>
    public string? MD5Checksum { get; set; }

    /// <summary>
    /// Gets or sets the extraction version, which typically consists of a major, minor, and build number.
    /// </summary>
    public Version? T4Version { get; set; }

    /// <summary>
    /// Gets or sets the length, in bytes, of the largest locale entry's text.
    /// </summary>
    public int? TextLength { get; set; }

    /// <summary>
    /// Gets or sets the version, which typically consists of a major, minor, and build number.
    /// </summary>
    public Version? Version { get; set; }

    /// <summary>
    /// Gets or sets the extraction date, which is typically in "ddd MMM dd HH:mm:ss zzz yyyy" format.
    /// <br/>For example, <c>Mon Jan 09 14:48:49 PST 2012</c>.
    /// </summary>
    public string? ExtractionDate { get; set; }

    /// <summary>
    /// Gets or sets the extraction version, which typically consists of a major, minor, and build number.
    /// </summary>
    public Version? ExtractionVersion { get; set; }

    /// <summary>
    /// Creates a new instance of <see cref="LocaleMetadata"/> from the specified .dat file and locale entries.
    /// </summary>
    /// <param name="localeDatFile">The path to the locale .dat file.</param>
    /// <param name="entries">A collection of locale entries.</param>
    /// <returns>A new metadata instance with some properties initialized.</returns>
    /// <exception cref="ArgumentNullException"/>
    public static LocaleMetadata Create(string localeDatFile, IEnumerable<LocaleEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(localeDatFile, nameof(localeDatFile));
        ArgumentNullException.ThrowIfNull(entries, nameof(entries));

        return new LocaleMetadata().Update(localeDatFile, entries);
    }

    /// <summary>
    /// Creates a copy of this metadata and updates its properties with the specified .dat file and locale entries.
    /// </summary>
    /// <param name="localeDatFile">The path to the locale .dat file.</param>
    /// <param name="entries">A collection of locale entries.</param>
    /// <returns>A copy of this metadata instance with the updated properties.</returns>
    /// <exception cref="ArgumentNullException"/>
    public LocaleMetadata Update(string localeDatFile, IEnumerable<LocaleEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(localeDatFile, nameof(localeDatFile));
        ArgumentNullException.ThrowIfNull(entries, nameof(entries));

        // Count the number of locale entries.
        int count = entries.Count();

        // Use the current date for the metadata, if uninitialized.
        string date = DateTime.UtcNow.ToString(GetDateFormat("UTC"));

        // Get the locale type from the filename, if uninitialized.
        string filename = Path.GetFileName(localeDatFile);
        Locale? locale = null;

        if (filename.Length == LocaleFileNameLength)
        {
            if (Enum.TryParse(filename[..LocaleCodeLength], ignoreCase: true, out Locale fileLocale))
            {
                locale = fileLocale;
            }
        }

        // Compute the MD5 checksum.
        using MD5 md5 = MD5.Create();
        using FileStream stream = File.OpenRead(localeDatFile);
        string md5Checksum = Convert.ToHexString(md5.ComputeHash(stream));

        // Compute the maximum text length, in bytes.
        int textLength = entries.MaxOrDefault(x => Encoding.UTF8.GetByteCount(x.Text));

        return this with
        {
            Count = count,
            Date = Date ?? date,
            Locale = Locale ?? locale,
            MD5Checksum = md5Checksum,
            TextLength = textLength,
            ExtractionDate = IsExtracted() ? (ExtractionDate ?? date) : null,
        };
    }

    /// <summary>
    /// Sets the metadata property with the given name to the specified value.
    /// </summary>
    /// <param name="name">The name of the property.</param>
    /// <param name="value">The value to assign.</param>
    /// <returns>The value assigned to the metadata property.</returns>
    /// <exception cref="ArgumentException"/>
    public object? SetProperty(string name, string value) => name switch
    {
        "CidLength" => CidLength = ParseValue(int.Parse, value),
        "Count" => Count = ParseValue(int.Parse, value),
        "Database" => Database = ValidateValue(x => Uri.IsWellFormedUriString(x, UriKind.Absolute), value),
        "Date" => Date = ValidateValue(ValidateDate, value),
        "Game" => Game = ParseValue(Enum.Parse<Game>, value),
        "Locale" => Locale = ParseValue(Enum.Parse<Locale>, value),
        "MD5Checksum" => MD5Checksum = ValidateValue(x => x.Length == 32 && x.All(IsUpperHexCharacter), value),
        "T4Version" => T4Version = ParseValue(x => new Version(x), value),
        "TextLength" => TextLength = ParseValue(int.Parse, value),
        "Version" => Version = ParseValue(x => new Version(x), value),
        "Extraction Date" => ExtractionDate = ValidateValue(ValidateDate, value),
        "Extraction version" => ExtractionVersion = ParseValue(x => new Version(x), value),
        _ => throw new ArgumentException($"Unrecognized metadata name: '{name}'", nameof(name)),
    };

    /// <summary>
    /// Returns the <see cref="DateTime"/> for the metadata.
    /// </summary>
    /// <exception cref="InvalidOperationException"/>
    public DateTime GetDate()
    {
        if ((Date ?? ExtractionDate) is not string date)
        {
            throw new InvalidOperationException("Date is not initialized.");
        }

        return DateTime.ParseExact(date, GetDateFormat(GetTimeZone(date)), CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Returns <see langword="true"/> if the metadata includes
    /// extraction properties; <see langword="false"/> otherwise.
    /// </summary>
    public bool IsExtracted() => ExtractionDate != null || ExtractionVersion != null;

    /// <summary>
    /// Returns <see langword="true"/> if the metadata refers to a TCG locale; otherwise <see langword="false"/>.
    /// </summary>
    public bool IsTCG() => (IsExtracted(), Game) switch
    {
        (true, _) => true,
        (false, LocaleParser.Game.FRLMTCG) => true,
        (false, LocaleParser.Game.FRLMTCGCN) => true,
        (false, LocaleParser.Game.LON) => true,
        (false, _) => false
    };

    /// <summary>
    /// Converts the string representation of <typeparamref name="T"/> to an equivalent
    /// object with type <typeparamref name="T"/> using the specified parsing function.
    /// </summary>
    /// <returns>
    /// An object of type <typeparamref name="T"/> whose value is represented by
    /// <paramref name="value"/>, or the default value of <typeparamref name="T"/>
    /// if <paramref name="value"/> is the string "Unknown".
    /// </returns>
    private static T? ParseValue<T>(Func<string, T> parse, string value)
    {
        return value == "Unknown" ? default : parse(value);
    }

    /// <summary>
    /// Checks whether the specified string conforms to the given validation function.
    /// </summary>
    /// <returns>
    /// <list type="bullet">
    /// <item><see langword="null"/>, if <paramref name="value"/> is the string "Unknown".</item>
    /// <item><paramref name="value"/>, if the validation function returns <see langword="true"/>.</item>
    /// <item>Otherwise, does not return. Throws a <see cref="FormatException"/>.</item>
    /// </list>
    /// </returns>
    /// <exception cref="FormatException"/>
    private static string? ValidateValue(Func<string, bool> validate, string value)
    {
        if (value == "Unknown")
        {
            return null;
        }
        else if (validate(value))
        {
            return value;
        }
        else
        {
            throw new FormatException($"Invalid metadata value: {value}");
        }
    }

    /// <summary>
    /// Returns <see langword="true"/> if the specified string conforms
    /// to the locale date format; otherwise <see langword="false"/>.
    /// </summary>
    private bool ValidateDate(string value)
    {
        if (value.Length != LocaleDateLength) return false;

        string timezone = GetTimeZone(value);

        if (timezone.All(char.IsUpper))
        {
            DateTime.ParseExact(value, GetDateFormat(timezone), CultureInfo.InvariantCulture);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Returns the timezone from the specified date.
    /// </summary>
    private static string GetTimeZone(string date) => date[20..23];

    /// <summary>
    /// Returns the locale date format for the specified timezone.
    /// </summary>
    private static string GetDateFormat(string timezone) => string.Format(LocaleDateFormat, timezone);

    /// <summary>
    /// Returns <see langword="true"/> if the char is an uppercase
    /// hex character; otherwise <see langword="false"/>.
    /// </summary>
    private static bool IsUpperHexCharacter(char c) => '0' <= c && c <= '9' || 'A' <= c && c <= 'F';

    /// <summary>
    /// Returns a metadata line with the specified name and value, or an empty string if the value is null.
    /// </summary>
    private static string CreateMetadataLine(string name, object? value, bool blankIfNull = false)
    {
        return blankIfNull && value == null ? "" : $"## {name}:\t{value ?? "Unknown"}{Environment.NewLine}";
    }

    /// <summary>
    /// Returns a string representation of the metadata as it would appear in a locale .dir file.
    /// </summary>
    public override string ToString()
    {
        StringBuilder sb = new();

        if (!IsExtracted())
        {
            sb.Append(CreateMetadataLine("CidLength", CidLength))
              .Append(CreateMetadataLine("Count", Count))
              .Append(CreateMetadataLine("Database", Database, blankIfNull: true))
              .Append(CreateMetadataLine("Date", Date))
              .Append(CreateMetadataLine("Game", Game))
              .Append(CreateMetadataLine("Locale", Locale))
              .Append(CreateMetadataLine("MD5Checksum", MD5Checksum))
              .Append(CreateMetadataLine("T4Version", T4Version))
              .Append(CreateMetadataLine("TextLength", TextLength))
              .Append(CreateMetadataLine("Version", Version));
        }
        else
        {
            sb.AppendLine("##")
              .Append(CreateMetadataLine("Extraction Date", ExtractionDate))
              .Append(CreateMetadataLine("Count", Count))
              .Append(CreateMetadataLine("Extraction version", ExtractionVersion))
              .AppendLine("##");
        }

        return sb.ToString();
    }
}
