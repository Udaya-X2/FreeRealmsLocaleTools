using CsvHelper;
using FreeRealmsLocaleTools.LocaleParser;
using System.Globalization;

namespace FreeRealmsLocaleTools.Tests;

public class LocaleTests
{
    private record TextRecord(int Id, string Text);

    private static readonly string InputLocaleDatPath = "data/en_us_data.dat";
    private static readonly string InputLocaleDirPath = "data/en_us_data.dir";
    private static readonly string OutputLocaleDatPath = "en_us_data.dat";
    private static readonly string OutputLocaleDirPath = "en_us_data.dir";
    private static readonly string NamesCsvPath = "names.csv";

    private readonly LocaleFileInfo _localeFile;

    public LocaleTests()
    {
        _localeFile = new(InputLocaleDatPath, InputLocaleDirPath);
    }

    [Fact]
    public void GenerateNamesCsv()
    {
        using StreamWriter writer = new(NamesCsvPath);
        using CsvWriter csv = new(writer, CultureInfo.InvariantCulture);
        csv.WriteRecords(_localeFile.IdToEntry.Select(x => new TextRecord(x.Key, x.Value.Text)));
    }

    [Fact]
    public void EditLocaleText()
    {
        _localeFile.ReplaceEntries(x => x.Hash <= 900656, "Replace text test");
        _localeFile.RemoveEntries(x => x.Tag == LocaleTag.ucdt);
        _localeFile.AddEntries(Enumerable.Repeat("Add text test", 100));
        _localeFile.WriteEntries(OutputLocaleDatPath, OutputLocaleDirPath);
    }
}
