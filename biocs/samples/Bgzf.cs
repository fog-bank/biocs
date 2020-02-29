using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Biocs.IO;
using ConsoleAppFramework;
using Microsoft.Extensions.Logging;

namespace Biocs
{
    partial class Program
    {
        [Command("bgzf", "Compress or decompress a file in the BGZF format.")]
        public async Task<int> Compress(
            [Option("i", "input file name")] string input,
            [Option("o", "output file name. By default, generated from input file name")] string? output = null,
            [Option("c", "write to standard output instead of file")] bool stdout = false,
            [Option("d", "decompress mode")] bool decompress = false,
            [Option("f", "overwrite a file without asking")] bool force = false,
            [Option("l", "compression level; -1 (optimal), 0 (no compression), 1 (fast)")] int level = -1)
        {
            // Check input
            if (!File.Exists(input))
            {
                Context.Logger.LogError("The input ({input}) doesn't exist.", input);
                return 1;
            }

            if (decompress && !BgzfStream.IsBgzfFile(input))
            {
                Context.Logger.LogError("The input ({input}) isn't the BGZF format.", input);
                return 1;
            }

            // Check output
            if (output == "-")
            {
                // Regards dash "-" as stdout
                stdout = true;
                output = null;
            }

            if (stdout)
            {
                if (output != null)
                {
                    Context.Logger.LogWarning(
                        "Because the output is written to stdout, the output file name ({output}) is ignored.", output);
                }
                output = "stdout";

                if (force)
                    Context.Logger.LogWarning("Because the output is written to stdout, -f option is ignored.");
            }
            else
            {
                if (output == null)
                {
                    if (decompress)
                    {
                        output = input.EndsWith(".gz") ? input[..^3] : input + ".out";
                    }
                    else
                        output += ".gz";
                }

                if (!force && File.Exists(output))
                {
                    Context.Logger.LogWarning(
                        "The output file exists already: '{output}'. To force overwriting, please specify -f option.", output);
                    return 1;
                }
            }

            // Check level
            CompressionLevel compLevel;
            switch (level)
            {
                case -1:
                    compLevel = CompressionLevel.Optimal;
                    break;

                case 0:
                    compLevel = CompressionLevel.NoCompression;
                    break;

                case 1:
                    compLevel = CompressionLevel.Fastest;
                    break;

                default:
                    Context.Logger.LogError("The invalid compression level ({level}).", level);
                    return 1;
            }

            if (decompress && level != -1)
                Context.Logger.LogWarning("When decompressing, -l option is ignored.");

            // Main
            using (var ifs = File.OpenRead(input))
            using (var ofs = stdout ? Console.OpenStandardOutput() : File.Create(output))
            {
                if (decompress)
                {
                    Context.Logger.LogInformation("Start to decompress '{input}' to '{output}'.", input, output);

                    using (var gz = new BgzfStream(ifs, CompressionMode.Decompress))
                    {
                        await gz.CopyToAsync(ofs, Context.CancellationToken);
                    }
                }
                else
                {
                    Context.Logger.LogInformation("Start to compress '{input}' to '{output}'.", input, output);

                    using (var gz = new BgzfStream(ofs, compLevel))
                    {
                        await ifs.CopyToAsync(gz, Context.CancellationToken);
                    }
                }
            }
            return 0;
        }
    }
}
