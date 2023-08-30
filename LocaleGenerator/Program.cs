using CsvHelper;
using FreeRealmsLocaleTools.IdHashing;
using FreeRealmsLocaleTools.LocaleParser;
using System.Globalization;

namespace LocaleGenerator
{
    public static class Program
    {
        private static readonly string ClientPath = Environment.ExpandEnvironmentVariables("%CLIENTPATH%");
        private static readonly string OutputPath = "output.csv";

        public static void Main()
        {
            // Read each entry from the locale files.
            string localeDatPath = $@"{ClientPath}\locale\en_us_data.dat";
            string localeDirPath = $@"{ClientPath}\locale\en_us_data.dir";
            Dictionary<uint, LocaleEntry[]> mappedEntries = LocaleReader.ReadMappedEntries(localeDatPath, localeDirPath);
            SortedSet<LocaleEntry> localeEntries = Preimaging.CreateLocaleEntryIdSet(mappedEntries);

            // Write the entries into the CSV file.
            using StreamWriter writer = new(OutputPath);
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
            csv.WriteRecords(localeEntries);
        }
    }
}
