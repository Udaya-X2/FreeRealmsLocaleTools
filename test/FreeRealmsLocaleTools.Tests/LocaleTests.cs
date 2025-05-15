using CsvHelper;
using FreeRealmsLocaleTools.LocaleParser;
using System.Globalization;

namespace FreeRealmsLocaleTools.Tests;

public class LocaleTests
{
    private static readonly string LocaleDatPath = "data/en_us_data.dat";
    private static readonly string LocaleDirPath = "data/en_us_data.dir";
    private static readonly string NamesCsvPath = "names.csv";

    private record TextRecord(int Id, string Text);

    [Fact]
    public void GenerateNamesCsv()
    {
        LocaleFileInfo localeFile = new(LocaleDatPath, LocaleDirPath);
        using StreamWriter writer = new(NamesCsvPath);
        using CsvWriter csv = new(writer, CultureInfo.InvariantCulture);
        csv.WriteRecords(localeFile.IdToEntry.Select(x => new TextRecord(x.Key, x.Value.Text)));
    }
}
