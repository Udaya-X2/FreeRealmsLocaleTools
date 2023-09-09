using FreeRealmsLocaleTools.LocaleParser;
using System.Diagnostics;

namespace LocaleGenerator
{
    public static class Program
    {
        private static readonly string ClientPath = Environment.ExpandEnvironmentVariables("%CLIENTPATH%");
        private static readonly string ClientFiles = Environment.ExpandEnvironmentVariables("%FRF%");
        private static readonly string OutputPath = "output.csv";

        public static void Main()
        {
            Stopwatch sw = Stopwatch.StartNew();
            string datFile2014 = $@"C:\Users\udaya\Downloads\FR Files\Free Realms [2014-03-27, Nird]\Free Realms\locale\en_us_data.dat";
            string dirFile2014 = $@"C:\Users\udaya\Downloads\FR Files\Free Realms [2014-03-27, Nird]\Free Realms\locale\en_us_data.dir";
            LocaleFile.RemoveEntries($@"{ClientPath}\locale\en_us_data.dat",
                                     $@"{ClientPath}\locale\en_us_data.dir",
                                     x => x.Tag is LocaleTag.ugdn);

            //using StreamWriter writer = new(OutputPath);
            //using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
            //csv.WriteHeader(new { Id = 0u, Hash = 0u, Tag = default(LocaleTag), Text = "" }.GetType());
            //csv.NextRecord();
            //csv.WriteRecords(localeFile.IdToEntry);

            Console.WriteLine($"Elapsed time: {sw.Elapsed}");
            return;
        }
    }
}
