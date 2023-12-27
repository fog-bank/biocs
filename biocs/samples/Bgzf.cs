using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Biocs.IO;
using Microsoft.Extensions.Logging;

namespace Biocs;

partial class Program
{
    [Command("bgzf", "Compress or decompress a file in the BGZF format.")]
    public async Task<int> Compress(
        [Option("i", "Input file name. By default, read from standard input")] string? input = null,
        [Option("o", "Output file name. By default, generate from input file name")] string? output = null,
        [Option("c", "Write to standard output instead of file")] bool stdout = false,
        [Option("d", "Decompress mode")] bool decompress = false,
        [Option("f", "Overwrite a file without asking")] bool force = false,
        [Option("l", "Compression level; -1 (optimal), 0 (no compression), 1 (fast)")] int level = -1)
    {
        if (input == null && output == null && !Console.IsInputRedirected && !Console.IsOutputRedirected)
        {
            // No option
            Context.Logger.LogError("For help, specify -help option.");
            return 1;
        }

        // Check {input}
        bool stdin;
        if (input == null || input == "-")
        {
            if (decompress && !Console.IsInputRedirected)
            {
                Context.Logger.LogError("Compressed input should be read from a file or redirected from standard input.");
                return 1;
            }
            // Regards dash "-" as stdin
            stdin = true;
            input = "stdin";

            // Use stdout if {output} isn't specified
            if (output == null)
                stdout = true;
        }
        else
        {
            stdin = false;

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
        }

        // Check {output}
        if (stdout || output == "-")
        {
            if (stdout && output != null)
            {
                Context.Logger.LogWarning(
                    "Because -c options is specified, the output file name ({output}) is ignored.", output);
            }

            if (!decompress && !Console.IsOutputRedirected)
            {
                // For Linux environment, writing binary data to console may occur IOException.
                Context.Logger.LogError("Compressed output should be written to a file or redirected from standard output.");
                return 1;
            }
            // Regards dash "-" as stdout
            stdout = true;
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
                    output = input + ".gz";
            }

            if (!force && File.Exists(output))
            {
                Context.Logger.LogError(
                    "The output file exists already: '{output}'. To force overwriting, please specify -f option.", output);
                return 1;
            }
        }

        // Check {level}
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
                Context.Logger.LogError("The specified compression level ({level}) is not supported.", level);
                return 1;
        }

        if (decompress && level != -1)
            Context.Logger.LogWarning("When decompressing, -l option is ignored.");

        // Main
        using (var ifs = stdin ? Console.OpenStandardInput() : File.OpenRead(input))
        using (var ofs = stdout ? Console.OpenStandardOutput() : File.Create(output))
        {
            if (decompress)
            {
                Context.Logger.LogInformation("Start to decompress '{input}' to '{output}'.", input, output);

                using var gz = new BgzfStream(ifs, CompressionMode.Decompress);
                await gz.CopyToAsync(ofs, Context.CancellationToken);
            }
            else
            {
                Context.Logger.LogInformation("Start to compress '{input}' to '{output}'.", input, output);

                using var gz = new BgzfStream(ofs, compLevel);
                await ifs.CopyToAsync(gz, Context.CancellationToken);
            }
        }
        return 0;
    }
}
