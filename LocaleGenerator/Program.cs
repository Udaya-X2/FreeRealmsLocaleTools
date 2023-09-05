using FreeRealmsLocaleTools.IdHashing;
using FreeRealmsLocaleTools.LocaleParser;
using System.Diagnostics;

namespace LocaleGenerator
{
    public static class Program
    {
        private static readonly string ClientPath = Environment.ExpandEnvironmentVariables("%CLIENTPATH%");
        private static readonly string ClientFiles = Environment.ExpandEnvironmentVariables("%FRF%");
        private static readonly string OutputPath = "output.csv";
        private static readonly Stopwatch SW = Stopwatch.StartNew();

        public static void Main()
        {
            foreach (string datPath in Directory.EnumerateFiles(ClientFiles, "*data.dat", SearchOption.AllDirectories))
            {
                if (datPath.Contains("__MACOSX") || datPath.Contains("PS3")) continue;

                string dirPath = Path.ChangeExtension(datPath, ".dir");

                if (File.Exists(dirPath))
                {
                    Console.WriteLine(datPath);
                    LocaleFileInfo info = new(datPath, dirPath, true);
                }
            }

            Console.WriteLine($"Elapsed time: {SW.Elapsed}");
            return;

            //string localeDatFile = $@"{ClientPath}\locale\en_us_data.dat";
            //string localeDirFile = $@"{ClientPath}\locale\en_us_data.dir";
            //LocaleEntry[] entries = LocaleReader.ReadEntries(localeDatFile);
            //SortedDictionary<uint, LocaleEntry> idToEntry = Preimaging.CreateIdMapping(entries);
            //using StreamWriter writer = new(OutputPath);
            //using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
            //csv.WriteHeader(new { Id = 0u, Hash = 0u, Tag = default(LocaleTag), Text = "" }.GetType());
            //csv.NextRecord();
            //csv.WriteRecords(idToEntry);
            //Console.WriteLine($"Elapsed time: {sw.Elapsed}");
            //return;

            Console.Clear();
            Console.ForegroundColor = ConsoleColor.White;

            foreach (string datPath in Directory.EnumerateFiles(ClientFiles, "*data.dat", SearchOption.AllDirectories))
            {
                if (datPath.Contains("__MACOSX")) continue;

                string dirPath = Path.ChangeExtension(datPath, ".dir");
                Console.WriteLine(datPath);

                try
                {
                    LocaleEntry[] localeEntries1 = LocaleFile.ReadEntries(datPath);
                    LocaleEntry[] localeEntries2 = LocaleFile.ReadEntries(datPath, dirPath);

                    for (int i = 0; i < localeEntries1.Length; i++)
                    {
                        if (localeEntries1[i] != localeEntries2[i])
                        {
                            Console.WriteLine(localeEntries1[i].ToString().Replace("\r", "^M"));
                            Console.WriteLine(string.Concat(Enumerable.Repeat("-", Console.WindowWidth)));
                            Console.WriteLine(localeEntries2[i].ToString().Replace("\r", "^M"));
                        }
                    }

                    if (!localeEntries1.SequenceEqual(localeEntries2))
                    {
                        Console.WriteLine("DISCREPANCY FOUND");
                        Environment.Exit(0);
                    }
                    continue;
                    LocaleMetadata metadata = LocaleFile.ReadMetadata(dirPath);
                    var tagCounts = localeEntries1.GroupBy(x => x.Tag)
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

            Console.WriteLine($"Elapsed time: {SW.Elapsed}");
        }
    }
}
