using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Biocs.IO
{
    [TestClass]
    public class BgzfStreamTest
    {
        private readonly string PathRawFile = Path.Combine("Deployments", "ce.sam");
        private readonly string PathGzFile = Path.Combine("Deployments", "ce.sam.gz");

        [TestMethod]
        public void IsBgzfFile_Test()
        {
            Assert.IsTrue(BgzfStream.IsBgzfFile(PathGzFile));
            Assert.IsFalse(BgzfStream.IsBgzfFile(PathRawFile));
            Assert.IsFalse(BgzfStream.IsBgzfFile(Path.Combine("Deployments", "nonexist.txt.gz")));
        }

        [TestMethod]
        public void Constructor_Test()
        {
            Assert.ThrowsException<ArgumentNullException>(() => new BgzfStream(null, CompressionMode.Decompress));
            Assert.ThrowsException<ArgumentNullException>(() => new BgzfStream(null, CompressionLevel.Optimal));
        }

        [TestMethod]
        public void Read_Test()
        {
            var raw = File.ReadAllBytes(PathRawFile);
            var actual = new byte[raw.Length];

            using (var fs = File.OpenRead(PathGzFile))
            {
                using (var gz = new BgzfStream(fs, CompressionMode.Decompress, true))
                {
                    int offset = 0;
                    int bytes = 0;
                    const int Count = 100;

                    for (; offset + Count <= actual.Length; offset += Count)
                    {
                        bytes = gz.Read(actual, offset, Count);
                        Assert.AreEqual(Count, bytes);
                    }

                    var buffer = new byte[Count];
                    bytes = gz.Read(buffer, 0, buffer.Length);

                    Assert.AreEqual(actual.Length - offset, bytes);
                    Assert.AreEqual(-1, gz.ReadByte());

                    Array.Copy(buffer, 0, actual, offset, bytes);
                    CollectionAssert.AreEqual(raw, actual);
                }
                Assert.AreEqual(fs.Length, fs.Position);
            }
        }

        [TestMethod]
        public async Task ReadAsync_Test()
        {
            var raw = File.ReadAllBytes(PathRawFile);
            var actual = new byte[raw.Length];

            using (var fs = File.OpenRead(PathGzFile))
            {
                using (var gz = new BgzfStream(fs, CompressionMode.Decompress, true))
                {
                    int offset = 0;
                    int bytes = 0;
                    const int Count = 100;

                    for (; offset + Count <= actual.Length; offset += Count)
                    {
                        bytes = await gz.ReadAsync(actual.AsMemory(offset, Count));
                        Assert.AreEqual(Count, bytes);
                    }

                    var buffer = new byte[Count];
                    bytes = await gz.ReadAsync(buffer.AsMemory(0, buffer.Length));

                    Assert.AreEqual(actual.Length - offset, bytes);
                    Assert.AreEqual(-1, gz.ReadByte());

                    Array.Copy(buffer, 0, actual, offset, bytes);
                    CollectionAssert.AreEqual(raw, actual);
                }
                Assert.AreEqual(fs.Length, fs.Position);
            }
        }

        [TestMethod]
        public void Write_TestByOwnDecompressor()
        {
            using (var fs = File.OpenRead(PathRawFile))
            using (var ms = new MemoryStream())
            {
                using (var gz = new BgzfStream(ms, CompressionMode.Compress, true))
                {
                    fs.CopyTo(gz, 100);
                }

                fs.Position = 0;
                ms.Position = 0;

                var raw = File.ReadAllBytes(PathRawFile);
                var actual = new byte[raw.Length];

                using (var gz = new BgzfStream(ms, CompressionMode.Decompress, true))
                {
                    int bytes = gz.Read(actual, 0, actual.Length);

                    Assert.AreEqual(raw.Length, bytes);
                    Assert.AreEqual(-1, gz.ReadByte());
                    CollectionAssert.AreEqual(raw, actual);
                }
            }
        }

        [TestMethod]
        public void Write_TestWithSingleBlock()
        {
            byte[] Data = "122333444455555".Select(x => (byte)x).ToArray();

            using (var ms = new MemoryStream())
            {
                using (var gz = new BgzfStream(ms, CompressionMode.Compress, true))
                {
                    gz.Write(Data, 0, Data.Length);
                }

                ms.Position = 0;

                using (var gz = new GZipStream(ms, CompressionMode.Decompress))
                {
                    var actual = new byte[Data.Length];
                    int bytes = gz.Read(actual, 0, actual.Length);

                    Assert.AreEqual(Data.Length, bytes);
                    Assert.AreEqual(-1, gz.ReadByte());
                    CollectionAssert.AreEqual(Data, actual);
                }
            }
        }

        [TestMethod]
        public void Write_TestWithoutCompression()
        {
            using (var fs = File.OpenRead(PathRawFile))
            using (var ms = new MemoryStream())
            {
                using (var gz = new BgzfStream(ms, CompressionLevel.NoCompression, true))
                {
                    fs.CopyTo(gz, 100);
                }

                fs.Position = 0;
                ms.Position = 0;

                var raw = File.ReadAllBytes(PathRawFile);

                using (var gz = new BgzfStream(ms, CompressionMode.Decompress, true))
                {
                    var actual = new byte[raw.Length];
                    int bytes = gz.Read(actual, 0, actual.Length);

                    Assert.AreEqual(raw.Length, bytes);
                    Assert.AreEqual(-1, gz.ReadByte());
                    CollectionAssert.AreEqual(raw, actual);
                }
            }
        }
    }
}
