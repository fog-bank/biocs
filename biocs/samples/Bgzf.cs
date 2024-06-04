using System.IO.Compression;
using Biocs.IO;

namespace Biocs;

partial class Bgzf(ILogger<Bgzf> logger)
{
    /// <summary>
    /// Compress or decompress a file in the BGZF format.
    /// </summary>
    /// <param name="input">-i, Input file name. By default, read from standard input.</param>
    /// <param name="output">-o, Output file name. By default, generate from input file name.</param>
    /// <param name="stdout">-c, Write to standard output instead of file.</param>
    /// <param name="decompress">-d, Decompress mode.</param>
    /// <param name="force">-f, Overwrite a file without asking.</param>
    /// <param name="level">-l, Compression level; -1 (optimal), 0 (no compression), 1 (fast).</param>
    [Command("bgzf")]
    public async Task<int> Compress(string? input = null, string? output = null, bool stdout = false, bool decompress = false,
        bool force = false, /*[Range(-1, 1)]*/ int level = -1, CancellationToken cancellationToken = default)
    {
        if (input == null && output == null && !Console.IsInputRedirected && !Console.IsOutputRedirected)
        {
            // No option
            logger.LogError("For help, specify -help option.");
            return 1;
        }

        // Check {input}
        bool stdin;
        if (input == null || input == "-")
        {
            if (decompress && !Console.IsInputRedirected)
            {
                logger.LogError("Compressed input should be read from a file or redirected from standard input.");
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
                logger.LogError("The input ({input}) doesn't exist.", input);
                return 1;
            }

            if (decompress && !BgzfStream.IsBgzfFile(input))
            {
                logger.LogError("The input ({input}) isn't the BGZF format.", input);
                return 1;
            }
        }

        // Check {output}
        if (stdout || output == "-")
        {
            if (stdout && output != null)
            {
                logger.LogWarning(
                    "Because -c options is specified, the output file name ({output}) is ignored.", output);
            }

            if (!decompress && !Console.IsOutputRedirected)
            {
                // For Linux environment, writing binary data to console may occur IOException.
                logger.LogError("Compressed output should be written to a file or redirected from standard output.");
                return 1;
            }
            // Regards dash "-" as stdout
            stdout = true;
            output = "stdout";

            if (force)
                logger.LogWarning("Because the output is written to stdout, -f option is ignored.");
        }
        else
        {
            if (output == null)
            {
                if (decompress)
                    output = input.EndsWith(".gz") ? input[..^3] : input + ".out";
                else
                    output = input + ".gz";
            }

            if (!force && File.Exists(output))
            {
                logger.LogError(
                    "The output file exists already: '{output}'. To force overwriting, please specify -f option.", output);
                return 1;
            }
        }

        // Check {level}
        var compLevel = level switch
        {
            0 => CompressionLevel.NoCompression,
            1 => CompressionLevel.Fastest,
            _ => CompressionLevel.Optimal
        };

        if (decompress && level != -1)
            logger.LogWarning("When decompressing, -l option is ignored.");

        // Main
        using var ifs = stdin ? Console.OpenStandardInput() : File.OpenRead(input);
        using var ofs = stdout ? Console.OpenStandardOutput() : File.Create(output);

        if (decompress)
        {
            logger.LogInformation("Start to decompress '{input}' to '{output}'.", input, output);

            using var gz = new BgzfStream(ifs, CompressionMode.Decompress);
            await gz.CopyToAsync(ofs, cancellationToken);
        }
        else
        {
            logger.LogInformation("Start to compress '{input}' to '{output}'.", input, output);

            using var gz = new BgzfStream(ofs, compLevel);
            await ifs.CopyToAsync(gz, cancellationToken);
        }
        return 0;
    }
}
