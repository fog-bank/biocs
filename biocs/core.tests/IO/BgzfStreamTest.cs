using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Biocs.IO
{
    [TestClass]
    public class BgzfStreamTest
    {
        private const string PathRawFile = @"Deployments\\ce.sam";
        private const string PathGzFile = @"Deployments\\ce.sam.gz";

        [TestMethod]
        public void IsBgzfFileTest()
        {
            Assert.IsTrue(BgzfStream.IsBgzfFile(PathGzFile));
            Assert.IsFalse(BgzfStream.IsBgzfFile(PathRawFile));
            Assert.IsFalse(BgzfStream.IsBgzfFile(@"Deployments\\nonexist.txt.gz"));
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
    }
}
