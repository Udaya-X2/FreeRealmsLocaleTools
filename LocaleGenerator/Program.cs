using CsvHelper;
using FreeRealmsLocaleTools.IdHashing;
using FreeRealmsLocaleTools.LocaleParser;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace LocaleGenerator
{
    public static class Program
    {
        private static readonly string ClientPath = Environment.ExpandEnvironmentVariables("%CLIENTPATH%");
        private static readonly string ClientFiles = Environment.ExpandEnvironmentVariables("%FRF%");
        private static readonly string OutputPath = "output.csv";

        public static void Main()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.White;
            Console.OutputEncoding = Encoding.Latin1;

            // Read each entry from the locale files.
            foreach (string dirPath in Directory.EnumerateFiles(ClientFiles, "zh_cn*.dir", SearchOption.AllDirectories))
            {
                if (dirPath.Contains("__MACOSX")) continue;

                string datPath = Path.ChangeExtension(dirPath, ".dat");
                Console.WriteLine(datPath);

                try
                {
                    Dictionary<uint, LocaleEntry[]> mappedEntries = LocaleReader.ReadMappedEntries(datPath, dirPath);
                    LocaleMetadata metadata = LocaleReader.ReadMetadata(dirPath);
                    var tagCounts = mappedEntries.Values.SelectMany(x => x)
                                                        .GroupBy(x => x.Tag)
                                                        .Select(x => new { Tag = x.Key, Count = x.Count() });

                    foreach (var tagCount in tagCounts)
                    {
                        Console.WriteLine(tagCount);
                    }
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(ex.Message);
                    Console.ForegroundColor = ConsoleColor.White;
                }
                //SortedSet<LocaleEntry> localeEntries = Preimaging.CreateEntryIdSet(mappedEntries);

                // Write the entries into the CSV file.
                //using StreamWriter writer = new(OutputPath);
                //using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
                //csv.WriteRecords(localeEntries);
                //Console.WriteLine(mappedEntries.Count);
                //Console.WriteLine(mappedEntries.Select(x => x.Value.Length).Sum());
                //Console.WriteLine(localeEntries.Count);
            }
        }
    }
}
