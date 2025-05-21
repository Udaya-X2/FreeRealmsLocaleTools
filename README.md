# FreeRealmsLocaleTools

A .NET library which allows developers to read and write text entries
from Free Realms locale files. It also includes other utilities such as
acquiring the name IDs of text entries and modifying locale metadata.

## Background

Locale files are located in the "./locale" and "./tcg/locale" directories of a
Free Realms client, in the form of `<locale>_data.dat` and `<locale>_data.dir`
files (i.e., `en_us_data.dat`/`en_us_data.dir`). The .dat file consists of text
entries while the .dir file contains metadata and information on each entry in
the .dat file. Combined, the client uses these files to display text based on
the user's [locale configuration](#supported-locales).

## Usage

See [documentation](https://udaya-x2.github.io/FreeRealmsLocaleTools) for
API reference, samples, and tutorials.

## Installation

Download the DLL from the [releases page](https://github.com/Udaya-X2/FreeRealmsLocaleTools/releases) and add it as a project reference in your assembly.

## Code Samples

### Adding a Locale Entry

```cs
using FreeRealmsLocaleTools.LocaleParser;

LocaleFileInfo localeFile = new("data/en_us_data.dat", "data/en_us_data.dir");
int nameId = localeFile.AddEntry("Illusion: Necronomicus - 15 minutes");
Console.WriteLine(localeFile.IdToEntry[nameId]);
localeFile.WriteEntries();
```

Console output:

```
2841312400      ucdt    Illusion: Necronomicus - 15 minutes
```

### Generating a CSV of Name IDs -> Text

```cs
using CsvHelper;
using FreeRealmsLocaleTools.LocaleParser;
using System.Globalization;

LocaleFileInfo localeFile = new("data/en_us_data.dat", "data/en_us_data.dir");
using StreamWriter writer = new("names.csv");
using CsvWriter csv = new(writer, CultureInfo.InvariantCulture);
csv.WriteRecords(localeFile.IdToEntry.Select(x => new { Id = x.Key, x.Value.Text }));
```

First 5 lines of names.csv:

```
Id,Text
1,Default Housing NPC
2,You receive #count([*item*])
3,You receive #count([*experience*]) and #count([*coins*])
4,coin
```

### Reading Locale Metadata

```cs
using FreeRealmsLocaleTools.LocaleParser;

LocaleMetadata metadata = LocaleFile.ReadMetadata("data/en_us_data.dir");
Console.WriteLine($"Locale = {metadata.Locale}");
Console.WriteLine($"Count = {metadata.Count}");
Console.WriteLine($"Date = {metadata.Date}");
```

Console output:

```
Locale = en_US
Count = 113286
Date = Thu Mar 13 10:10:13 PDT 2014
```

## Supported Locales

| ID | Locale | Description          |
|----|--------|----------------------|
| 1  | zh_CN  | Simplified Chinese   |
| 2  | de_DE  | German               |
| 3  | fr_FR  | French               |
| 4  | en_GB  | British English      |
| 5  | ja_JP  | Japanese             |
| 6  | ko_KR  | Korean               |
| 7  | zh_TW  | Traditional Chinese  |
| 8  | en_US  | American English     |
| 9  | es_ES  | Spanish              |
| 10 | it_IT  | Italian              |
| 11 | pt_PT  | Portuguese           |
| 12 | ru_RU  | Russian              |
| 13 | sv_SE  | Swedish              |
| 14 | pt_BR  | Brazilian Portuguese |
| 15 | es_MX  | Mexican Spanish      |
