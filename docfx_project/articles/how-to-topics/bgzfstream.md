# How to: Compress and extract a BFZF file

## Example 1: Compress a file as BGZF

```cs
using System.IO;
using System.IO.Compression;
using Biocs.IO;

string input = "data.txt";
string output = "data.txt.gz";

using var ifs = File.OpenRead(input);
using var ofs = File.Create(output);
using var gz = new BgzfStream(ofs, CompressionMode.Compress);
ifs.CopyTo(gz);
```

## Example 2: Extract a .gz file

The [GZipStream](xref:System.IO.Compression.GZipStream) class may not be able to decompress concatenated gzip archives such as
BGZF files. Meanwhile, the [BgzfStream](xref:Biocs.IO.BgzfStream) class can decompress BGZF files only. Threfore, before using
the BgzfStream class, you should check if the input file is in the BGZF format.

```cs
using System.IO;
using System.IO.Compression;
using Biocs.IO;

string input = "data.txt.gz";
string output = "data.txt";

bool isBgzf = BgzfStream.IsBgzfFile(input);

using var ifs = File.OpenRead(input);
using var ofs = File.Create(output);
using Stream gz = isBgzf ? new BgzfStream(ifs, CompressionMode.Decompress) : new GZipStream(ifs, CompressionMode.Decompress);
gz.CopyTo(ofs);
```