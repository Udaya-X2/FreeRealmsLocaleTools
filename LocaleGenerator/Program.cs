using CsvHelper;
using FreeRealmsLocaleTools.LocaleParser;
using System.Diagnostics;
using System.Globalization;

namespace LocaleGenerator;

public static class Program
{
    public static readonly string ClientPath = Environment.ExpandEnvironmentVariables("%CLIENTPATH%");
    public static readonly string ClientFiles = Environment.ExpandEnvironmentVariables("%FRF%");
    public static readonly string ClientDatFile = $@"{ClientPath}\locale\en_us_data.dat";
    public static readonly string ClientDirFile = $@"{ClientPath}\locale\en_us_data.dir";
    public const string DatFile2014 = @"C:\Users\udaya\Downloads\FR Files\Free Realms [2014-03-27, Nird]\Free Realms\locale\en_us_data.dat";
    public const string DirFile2014 = @"C:\Users\udaya\Downloads\FR Files\Free Realms [2014-03-27, Nird]\Free Realms\locale\en_us_data.dir";
    public const string OutputPath = @"C:\Users\udaya\GitHub\FreeRealmsLocaleTools\LocaleGenerator\output\names.csv";

    public static void Main()
    {
        Stopwatch sw = Stopwatch.StartNew();
        LocaleFileInfo localeFile = new(DatFile2014, DirFile2014);
        //localeFile.WriteEntries(ClientDatFile, ClientDirFile);

        using StreamWriter writer = new(OutputPath);
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
        csv.WriteRecords(localeFile.IdToEntry.Select(x => new TextRecord(x.Key, x.Value.Text)));
        Console.WriteLine($"Elapsed time: {sw.Elapsed}");
        return;
    }
}
