﻿using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Biocs.IO;
using ConsoleAppFramework;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Biocs
{
    class Program
    {
        static async Task Main(string[] args) => await Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<BiocsBatch>(args);
    }

    public class BiocsBatch : ConsoleAppBase
    {
        [Command("bgzf", "Compress or decompress a file in the BGZF format.")]
        public async Task Compress(
            [Option("i", "input file name")] string input,
            [Option("o", "output file name")] string output = null,
            [Option("d", "decompress mode")] bool decompress = false,
            [Option("f", "overwrite a file without asking")] bool force = false)
        {
            if (!File.Exists(input))
            {
                Context.Logger.LogError($"The input doesn't exist: '{input}'.");
                return;
            }

            if (decompress && !BgzfStream.IsBgzfFile(input))
            {
                Context.Logger.LogError($"'{input}' isn't the BGZF format.");
                return;
            }

            if (output == null)
            {
                if (decompress)
                {
                    output = input.EndsWith(".gz") ? input[0..^3] : input + ".out";
                }
                else
                    output = input + ".gz";
            }

            if (!force && File.Exists(output))
            {
                Context.Logger.LogWarning($"The output exists already: '{output}'.");
                return;
            }

            using (var ifs = File.OpenRead(input))
            using (var ofs = File.Create(output))
            {
                if (decompress)
                {
                    Context.Logger.LogInformation($"Decompress '{input}' to '{output}'.");

                    using (var gz = new BgzfStream(ifs, CompressionMode.Decompress))
                    {
                        await gz.CopyToAsync(ofs, Context.CancellationToken);
                    }
                }
                else
                {
                    Context.Logger.LogInformation($"Compress '{input}' to '{output}'.");

                    using (var gz = new BgzfStream(ofs, CompressionMode.Compress))
                    {
                        await ifs.CopyToAsync(gz, Context.CancellationToken);
                    }
                }
            }
        }
    }
}
