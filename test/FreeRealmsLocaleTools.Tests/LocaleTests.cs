using CsvHelper;
using FreeRealmsLocaleTools.LocaleParser;
using System.Globalization;

namespace FreeRealmsLocaleTools.Tests;

public class LocaleTests
{
    private record TextRecord(int Id, string Text);

    private static readonly string InputLocaleDatPath = "data/en_us_data.dat";
    private static readonly string InputLocaleDirPath = "data/en_us_data.dir";
    private static readonly string InputLocaleTcgDatPath = "data/en_us_data_tcg.dat";
    private static readonly string InputLocaleTcgDirPath = "data/en_us_data_tcg.dir";
    private static readonly string OutputLocaleDatPath = "en_us_data.dat";
    private static readonly string OutputLocaleDirPath = "en_us_data.dir";
    private static readonly string OutputLocaleTcgDatPath = "en_us_data_tcg.dat";
    private static readonly string OutputLocaleTcgDirPath = "en_us_data_tcg.dir";
    private static readonly string NamesCsvPath = "names.csv";

    private readonly LocaleFileInfo _localeFile;
    private readonly LocaleFileInfo _localeFileTcg;

    public LocaleTests()
    {
        _localeFile = new(InputLocaleDatPath, InputLocaleDirPath);
        _localeFileTcg = new(InputLocaleTcgDatPath, InputLocaleTcgDirPath);
    }

    [Fact]
    public void GenerateNamesCsv()
    {
        using StreamWriter writer = new(NamesCsvPath);
        using CsvWriter csv = new(writer, CultureInfo.InvariantCulture);
        csv.WriteRecords(_localeFile.IdToEntry.Select(x => new TextRecord(x.Key, x.Value.Text)));
    }

    [Fact]
    public void ThrowInvalidOperationTcgLocale()
    {
        Assert.Throws<InvalidOperationException>(() =>
        {
            _ = _localeFileTcg.HashToEntry;
            _ = _localeFileTcg.IdToEntry;
        });
    }

    [Fact]
    public void EditLocaleText()
    {
        _localeFile.ReplaceEntries(x => x.Hash <= 900656, "Replace text test");
        _localeFile.RemoveEntries(x => x.Tag == LocaleTag.ucdt);
        _localeFile.AddEntries(Enumerable.Repeat("Add text test", 100));
        _localeFile.WriteEntries(OutputLocaleDatPath, OutputLocaleDirPath);
    }

    [Fact]
    public void EditLocaleTextTcg()
    {
        _localeFileTcg.RemoveEntries(x => x.Tag != LocaleTag.mgdt);
        _localeFileTcg.ReplaceEntries(x => x.Text == "{v}change{3s=\"changes\"}\t0006\tCHANGE", "abc\t0006\tCHANGE");
        _localeFileTcg.RemoveEntries(x => !x.Text.Contains("abc"));
        _localeFileTcg.WriteEntries(OutputLocaleTcgDatPath, OutputLocaleTcgDirPath);
    }
}
