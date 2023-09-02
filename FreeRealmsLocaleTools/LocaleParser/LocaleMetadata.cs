using System.Text;

namespace FreeRealmsLocaleTools.LocaleParser
{
    /// <summary>
    /// Provides properties and instance methods to serialize and deserialize metadata from a Free Realms .dir file.
    /// </summary>
    public class LocaleMetadata
    {
        public int? CidLength { get; set; }
        public int? Count { get; set; }
        public string? Database { get; set; }
        public string? Date { get; set; }
        public Game? Game { get; set; }
        public Locale? Locale { get; set; }
        public string? MD5Checksum { get; set; }
        public string? T4Version { get; set; }
        public int? TextLength { get; set; }
        public string? Version { get; set; }
        public string? ExtractionDate { get; set; }
        public string? ExtractionVersion { get; set; }

        /// <summary>
        /// Sets the metadata property with the given name to the specified value.
        /// </summary>
        /// <returns>The value assigned to the metadata property.</returns>
        /// <exception cref="ArgumentException"></exception>
        public object SetProperty(string name, string value) => name switch
        {
            "CidLength" => CidLength = int.Parse(value),
            "Count" => Count = int.Parse(value),
            "Database" => Database = value,
            "Date" => Date = value,
            "Game" => Game = Enum.Parse<Game>(value),
            "Locale" => Locale = Enum.Parse<Locale>(value),
            "MD5Checksum" => MD5Checksum = value,
            "T4Version" => T4Version = value,
            "TextLength" => TextLength = int.Parse(value),
            "Version" => Version = value,
            "Extraction Date" => ExtractionDate = value,
            "Extraction version" => ExtractionVersion = value,
            _ => throw new ArgumentException($"Unrecognized metadata name: '{name}'", nameof(name)),
        };

        /// <summary>
        /// Returns true if the metadata does not include older American English TCG locale properties.
        /// </summary>
        public bool UsesDefaultMetadataFormat() => ExtractionDate == null && ExtractionVersion == null;

        /// <summary>
        /// Returns a metadata line with the specified name and value, or an empty string if the value is null.
        /// </summary>
        private static string CreateMetadataLine(string name, object? value)
        {
            return value == null ? "" : $"## {name}:\t{value}\r\n";
        }

        /// <summary>
        /// Returns a string representation of the metadata as it would appear in a locale .dir file.
        /// </summary>
        public override string ToString()
        {
            StringBuilder sb = new();

            if (UsesDefaultMetadataFormat())
            {
                sb.Append(CreateMetadataLine("CidLength", CidLength))
                  .Append(CreateMetadataLine("Count", Count))
                  .Append(CreateMetadataLine("Database", Database))
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
                sb.Append("##\r\n")
                  .Append(CreateMetadataLine("Extraction Date", ExtractionDate))
                  .Append(CreateMetadataLine("Count", Count))
                  .Append(CreateMetadataLine("Extraction version", ExtractionVersion))
                  .Append("##\r\n");
            }

            return sb.ToString();
        }
    }
}
