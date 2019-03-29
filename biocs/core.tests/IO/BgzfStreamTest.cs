using System;
using System.IO;
using System.IO.Compression;
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
        public void IsBgzfFileTest()
        {
            Assert.IsTrue(BgzfStream.IsBgzfFile(PathGzFile));
            Assert.IsFalse(BgzfStream.IsBgzfFile(PathRawFile));
            Assert.IsFalse(BgzfStream.IsBgzfFile(Path.Combine("Deployments", "nonexist.txt.gz")));
        }

        [TestMethod]
        public void ReadTest()
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
        public async Task ReadAsyncTest()
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
                        bytes = await gz.ReadAsync(actual, offset, Count);
                        Assert.AreEqual(Count, bytes);
                    }

                    var buffer = new byte[Count];
                    bytes = await gz.ReadAsync(buffer, 0, buffer.Length);

                    Assert.AreEqual(actual.Length - offset, bytes);
                    Assert.AreEqual(-1, gz.ReadByte());

                    Array.Copy(buffer, 0, actual, offset, bytes);
                    CollectionAssert.AreEqual(raw, actual);
                }
                Assert.AreEqual(fs.Length, fs.Position);
            }
        }

        [TestMethod]
        public void WriteTestByOwnDecompressor()
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
        public void WriteTestWithoutCompression()
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
    }
}
