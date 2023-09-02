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
        //private static readonly string ClientPath = @"C:\Users\udaya\Downloads\FR Files\Free Realms [2010-03-29, Jonathan]\Free Realms\tcg";
        private static readonly string ClientFiles = Environment.ExpandEnvironmentVariables("%FRF%");
        private static readonly string OutputPath = "output.csv";

        public static void Main()
        {
            //Stopwatch sw = Stopwatch.StartNew();
            //string localeDatFile = $@"{ClientPath}\locale\en_us_data.dat";
            //string localeDirFile = $@"{ClientPath}\locale\en_us_data.dir";
            //LocaleEntry[] entries = LocaleReader.ReadEntries(localeDatFile, localeDirFile);
            //SortedSet<LocaleEntry> idEntries = Preimaging.CreateEntryIdSet(entries);
            //using StreamWriter writer = new(OutputPath);
            //using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
            //csv.WriteRecords(idEntries);
            //Console.WriteLine($"Elapsed time: {sw.Elapsed}");
            //return;
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.White;
            Console.OutputEncoding = Encoding.Latin1;

            // Read each entry from the locale files.
            foreach (string dirPath in Directory.EnumerateFiles(ClientFiles, "*.dir", SearchOption.AllDirectories))
            {
                if (dirPath.Contains("__MACOSX")) continue;

                string datPath = Path.ChangeExtension(dirPath, ".dat");
                Console.WriteLine(datPath);

                try
                {
                    LocaleEntry[] localeEntries = LocaleReader.ReadEntries(datPath, dirPath);
                    LocaleMetadata metadata = LocaleReader.ReadMetadata(dirPath);
                    var tagCounts = localeEntries.GroupBy(x => x.Tag)
                                                 .Select(x => new { Tag = x.Key, Count = x.Count() })
                                                 .OrderBy(x => x.Count)
                                                 .Reverse();

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
            }
        }
    }
}
