﻿using System.IO;
using System.IO.Compression;
using System.Linq;
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
        public void ReadTestThroughStreamReader()
        {
            var contents = File.ReadAllLines(PathRawFile);

            using (var fs = File.OpenRead(PathGzFile))
            {
                using (var gz = new BgzfStream(fs, CompressionMode.Decompress, true))
                using (var sr = new StreamReader(gz))
                {
                    for (int i = 0; i <= contents.Length; i++)
                    {
                        string line = sr.ReadLine();
                        Assert.AreEqual(contents.ElementAtOrDefault(i), line);
                    }
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
    }
}
