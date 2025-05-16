using CsvHelper;
using FreeRealmsLocaleTools.IdHashing;
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
        _localeFileTcg.ReplaceEntries("{v}change{3s=\"changes\"}\t0006\tCHANGE", "abc\t0006\tCHANGE");
        _localeFileTcg.RemoveEntries(x => !x.Text.Contains("abc"));
        _localeFileTcg.WriteEntries(OutputLocaleTcgDatPath, OutputLocaleTcgDirPath);
    }

    [Fact]
    public void ParseMtagId()
    {
        string text = "Increases Damage Addition\t0017\tGlobal.Text.88011";
        Assert.True(Preimaging.ParseMtagTextId(text) == 88011);
        Assert.Throws<FormatException>(() => Preimaging.ParseMtagTextId("abc"));
    }

    [Fact]
    public void CreateLocale()
    {
        LocaleFileInfo localeFile = new();
        localeFile.AddEntries(_localeFile.Entries.Take(10).Select(x => x.Text));
        localeFile.WriteEntries("locale.dat", "locale.dir");
    }
}
