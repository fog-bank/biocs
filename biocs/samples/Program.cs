using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Biocs.IO;
using MicroBatchFramework;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Biocs
{
    class Program
    {
        static async Task Main(string[] args) => await new HostBuilder().RunBatchEngineAsync<BiocsBatch>(args);
    }

    public class BiocsBatch : BatchBase
    {
        [Command("bgzf", "Compress or decompress a file in the BGZF format.")]
        public void Compress(
            [Option("i", "input file name (string)")] string input,
            [Option("o", " output file name (string)")] string output = null,
            [Option("d", " decompress mode (bool)")] bool decompress = false,
            [Option("f", " overwrite a file without asking (bool)")] bool force = false)
        {
            if (!File.Exists(input))
            {
                Context.Logger.LogError($"'{input}' doesn't exist.");
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
                    output = input.EndsWith(".gz") ? input.Substring(0, input.Length - 3) : input + ".out";
                }
                else
                    output = input + ".gz";
            }

            if (!force && File.Exists(output))
            {
                Context.Logger.LogWarning($"'{output}' exists already.");
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
                        gz.CopyTo(ofs);
                    }
                }
                else
                {
                    Context.Logger.LogInformation($"Compress '{input}' to '{output}'.");

                    using (var gz = new BgzfStream(ofs, CompressionMode.Compress))
                    {
                        ifs.CopyTo(gz);
                    }
                }
            }
        }
    }
}
