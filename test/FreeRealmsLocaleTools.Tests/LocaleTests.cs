using CsvHelper;
using FreeRealmsLocaleTools.IdHashing;
using FreeRealmsLocaleTools.LocaleParser;
using System.Globalization;

namespace FreeRealmsLocaleTools.Tests;

public class LocaleTests
{
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
        csv.WriteRecords(_localeFile.IdToEntry.Select(x => new { Id = x.Key, x.Value.Text }));
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
        _localeFile.UpdateEntries(x => x.Hash <= 900656, "Replace text test");
        _localeFile.RemoveEntries(x => x.Tag == LocaleTag.ucdt);
        _localeFile.AddEntries(Enumerable.Repeat("Add text test", 100));
        _localeFile.WriteEntries(OutputLocaleDatPath, OutputLocaleDirPath);
    }

    [Fact]
    public void EditLocaleTextTcg()
    {
        _localeFileTcg.RemoveEntries(x => x.Tag != LocaleTag.mgdt);
        _localeFileTcg.UpdateEntries("{v}change{3s=\"changes\"}\t0006\tCHANGE", "abc\t0006\tCHANGE");
        _localeFileTcg.RemoveEntries(x => !x.Text.Contains("abc"));
        _localeFileTcg.WriteEntries(OutputLocaleTcgDatPath, OutputLocaleTcgDirPath);
    }

    [Fact]
    public void ParseMtagId()
    {
        string text = "Increases Damage Addition\t0017\tGlobal.Text.88011";
        Assert.Equal(88011, Preimaging.ParseMtagTextId(text));
        Assert.Throws<FormatException>(() => Preimaging.ParseMtagTextId("abc"));
    }

    [Fact]
    public void CreateLocale()
    {
        LocaleFileInfo localeFile = new();
        localeFile.AddEntries(_localeFile.Entries.Take(10).Select(x => x.Text));
        localeFile.UpdateEntries(_localeFile.Entries.Take(5).Select(x => x.Text),
                                  ["abc", "def", "ghi", "jkl", "mno"]);
        localeFile.WriteEntries("locale.dat", "locale.dir");
    }

    [Fact]
    public void AddEntry()
    {
        string text = "New locale text";
        int id = _localeFile.AddEntry(text);
        Assert.True(_localeFile.IdToEntry[id].Text == text);
    }

    [Fact]
    public void ThrowSimplifiedChineseLocales()
    {
        foreach ((string localeDatPath, string localeDirPath) in GetAllLocalePaths())
        {
            LocaleMetadata metadata = LocaleFile.ReadMetadata(localeDirPath);

            if (metadata.Locale == Locale.zh_CN && metadata.IsTcg())
            {
                Assert.Throws<InvalidDataException>(() =>
                {
                    LocaleFile.ReadEntries(localeDatPath, localeDirPath, ParseOptions.Strict);
                });
            }
        }
    }

    private static IEnumerable<(string, string)> GetAllLocalePaths()
    {
        foreach (string localeDatPath in GetAllLocaleDatPaths())
        {
            string localeDirPath = Path.ChangeExtension(localeDatPath, ".dir");

            if (File.Exists(localeDirPath))
            {
                yield return (localeDatPath, localeDirPath);
            }
        }
    }

    private static IEnumerable<string> GetAllLocaleDatPaths()
    {
        string frFilesDirectory = Environment.ExpandEnvironmentVariables("%FRF%");

        if (!Directory.Exists(frFilesDirectory)) yield break;

        foreach (string path in Directory.EnumerateFiles(frFilesDirectory, "*data.dat", SearchOption.AllDirectories))
        {
            // Skip __MACOSX files.
            if (!Path.GetFileName(path).StartsWith("._"))
            {
                yield return path;
            }
        }
    }
}
