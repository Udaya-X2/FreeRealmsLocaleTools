---
uid: Introduction
---

<div class="article">

# Introduction

## Installation

The package is available on the project [releases page](https://github.com/Udaya-X2/FreeRealmsLocaleTools/releases).
Simply download the DLL and add it as a project reference in your assembly.

## Background

Locale files are located in the "./locale" and "./tcg/locale" directories of a
Free Realms client, in the form of `<locale>_data.dat` and `<locale>_data.dir`
files (i.e., `en_us_data.dat`/`en_us_data.dir`). The .dat file consists of text
entries while the .dir file contains metadata and information on each entry in
the .dat file. Combined, the client uses these files to display text based on
the user's [locale configuration](../api/FreeRealmsLocaleTools.LocaleParser.Locale.yml).

Examples of an [`en_us_data.dat`](https://raw.githubusercontent.com/Udaya-X2/FreeRealmsLocaleTools/refs/heads/main/test/FreeRealmsLocaleTools.Tests/data/en_us_data.dat) and [`en_us_data.dir`](https://raw.githubusercontent.com/Udaya-X2/FreeRealmsLocaleTools/refs/heads/main/test/FreeRealmsLocaleTools.Tests/data/en_us_data.dir) file can be seen [here](https://github.com/Udaya-X2/FreeRealmsLocaleTools/tree/main/test/FreeRealmsLocaleTools.Tests/data).

## Usage

Your main entry points into reading/writing locale files are the [`LocaleFile`](../api/FreeRealmsLocaleTools.LocaleParser.LocaleFile.yml) and [`LocaleFileInfo`](../api/FreeRealmsLocaleTools.LocaleParser.LocaleFileInfo.yml) classes.
Similar to the [`System.IO.File`](https://learn.microsoft.com/dotnet/api/system.io.file) and [`System.IO.FileInfo`](https://learn.microsoft.com/dotnet/api/system.io.fileinfo) classes from .NET, `LocaleFile` provides static methods to manipulate locale files whereas `LocaleFileInfo` provides properties and instance methods to read/write them.
However, some operations can only be performed from the `LocaleFileInfo` class, as it provides more granularity in its approach.

## Reading Locale Entries

Printing locale entries for Coin Flow weapons:

```cs
using FreeRealmsLocaleTools.LocaleParser;

LocaleEntry[] entries = LocaleFile.ReadEntries("data/en_us_data.dat", "data/en_us_data.dir");

foreach (LocaleEntry entry in entries)
{
    if (entry.Tag == LocaleTag.ucdt && entry.Text.StartsWith("Coin Flow"))
    {
        Console.WriteLine(entry);
    }
}
```

Console output:

```plaintext
905058873       ucdt    Coin Flow Saw
1783849047      ucdt    Coin Flow Chopper
2113847445      ucdt    Coin Flow Diadem Wand
2194606925      ucdt    Coin Flow Mantis Bow
2708266239      ucdt    Coin Flow Electric Knife
3455052469      ucdt    Coin Flow Jackhammer
3724665230      ucdt    Coin Flow Megablade
3777688522      ucdt    Coin Flow Blow Torch
4260058758      ucdt    Coin Flow Mega Hammer
```

## Adding Locale Entries

Adding the locale entry "Illusion: Necronomicus - 15 minutes":

```cs
using FreeRealmsLocaleTools.LocaleParser;

LocaleFileInfo localeFile = new("data/en_us_data.dat", "data/en_us_data.dir");
int nameId = localeFile.AddEntry("Illusion: Necronomicus - 15 minutes");
Console.WriteLine(localeFile.IdToEntry[nameId]);
localeFile.WriteEntries(); // Updates the locale .dat/.dir file.
```

Console output:

```plaintext
2841312400      ucdt    Illusion: Necronomicus - 15 minutes
```

**Note**: Adding entries may require more time than reading, updating, or removing entries since it requires several lazily-computed properties, such as `LocaleFileInfo.IdToEntry`. Consider passing `lazyInit = false` to the `LocaleFileInfo` constructor to initialize these properties at startup.

## Updating Locale Entries

Appending the suffix " (Retired)" to locale entries for Coin Flow weapons:

```cs
using FreeRealmsLocaleTools.LocaleParser;

LocaleFileInfo localeFile = new("data/en_us_data.dat", "data/en_us_data.dir");
localeFile.UpdateEntries(x => x.Text.StartsWith("Coin Flow"), x => $"{x.Text} (Retired)");
localeFile.WriteEntries(); // Updates the locale .dat/.dir file.

foreach (LocaleEntry entry in localeFile.StoredEntries)
{
    if (entry.Tag == LocaleTag.ucdt && entry.Text.StartsWith("Coin Flow"))
    {
        Console.WriteLine(entry);
    }
}
```

Console output:

```plaintext
905058873       ucdt    Coin Flow Saw (Retired)
1783849047      ucdt    Coin Flow Chopper (Retired)
2113847445      ucdt    Coin Flow Diadem Wand (Retired)
2194606925      ucdt    Coin Flow Mantis Bow (Retired)
2708266239      ucdt    Coin Flow Electric Knife (Retired)
3455052469      ucdt    Coin Flow Jackhammer (Retired)
3724665230      ucdt    Coin Flow Megablade (Retired)
3777688522      ucdt    Coin Flow Blow Torch (Retired)
4260058758      ucdt    Coin Flow Mega Hammer (Retired)
```

## Removing Locale Entries

Removing locale entries for Coin Flow weapons:

```cs
using FreeRealmsLocaleTools.LocaleParser;

LocaleFileInfo localeFile = new("data/en_us_data.dat", "data/en_us_data.dir");
localeFile.RemoveEntries(x => x.Text.StartsWith("Coin Flow"));
localeFile.WriteEntries(); // Updates the locale .dat/.dir file.
int count = localeFile.StoredEntries.Count(x => x.Text.StartsWith("Coin Flow"));
Console.WriteLine($"Number of Coin Flow weapons = {count}");
```

Console output:

```plaintext
Number of Coin Flow weapons = 0
```

## Getting Name IDs from Locale Files

Generating a CSV of name IDs -> text called `names.csv`:

```cs
using CsvHelper;
using FreeRealmsLocaleTools.LocaleParser;
using System.Globalization;

LocaleFileInfo localeFile = new("data/en_us_data.dat", "data/en_us_data.dir");
using StreamWriter writer = new("names.csv");
using CsvWriter csv = new(writer, CultureInfo.InvariantCulture);
csv.WriteRecords(localeFile.IdToEntry.Select(x => new { Id = x.Key, x.Value.Text }));
```

First 5 lines of `names.csv`:

```plaintext
Id,Text
1,Default Housing NPC
2,You receive #count([*item*])
3,You receive #count([*experience*]) and #count([*coins*])
4,coin
```

## Reading Locale Metadata

Printing locale, count, and date from metadata:

```cs
using FreeRealmsLocaleTools.LocaleParser;

LocaleMetadata metadata = LocaleFile.ReadMetadata("data/en_us_data.dir");
Console.WriteLine($"Locale = {metadata.Locale}");
Console.WriteLine($"Count = {metadata.Count}");
Console.WriteLine($"Date = {metadata.Date}");
```

Console output:

```plaintext
Locale = en_US
Count = 113286
Date = Thu Mar 13 10:10:13 PDT 2014
```

</div>
