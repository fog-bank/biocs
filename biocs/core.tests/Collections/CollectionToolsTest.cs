using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Biocs.TestTools;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Biocs.Collections
{
    [TestClass]
    public class CollectionTools
    {
        [TestMethod]
        public void AllItemsAreEqualTest()
        {
            var src = Enumerable.Empty<int>();
            int value = 1;

            Assert.IsFalse(src.AllItemsAreEqual());
            Assert.IsFalse(src.AllItemsAreEqual(null, out value));
            Assert.AreEqual(0, value);

            src = Enumerable.Repeat(1, 1);
            Assert.IsTrue(src.AllItemsAreEqual());
            Assert.IsTrue(src.AllItemsAreEqual(null, out value));
            Assert.AreEqual(1, value);

            src = Enumerable.Repeat(1, 10);
            Assert.IsTrue(src.AllItemsAreEqual());
            Assert.IsTrue(src.AllItemsAreEqual(null, out value));
            Assert.AreEqual(1, value);

            src = Enumerable.Range(1, 10);
            Assert.IsFalse(src.AllItemsAreEqual());
            Assert.IsFalse(src.AllItemsAreEqual(null, out value));
            Assert.AreEqual(0, value);

            var src2 = new[] { new object(), null };
            src2[1] = src2[0];

            Assert.IsTrue(src2.AllItemsAreEqual());
            Assert.IsTrue(src2.AllItemsAreEqual(null, out object value2));
            Assert.AreEqual(src2[0], value2);

            var src3 = new[] { "ABC", "aBC", "AbC", "ABc", "abC", "aBc", "Abc", "abc" };

            Assert.IsFalse(src3.AllItemsAreEqual());
            Assert.IsTrue(src3.AllItemsAreEqual(StringComparer.OrdinalIgnoreCase, out string value3));
            Assert.AreEqual("ABC", value3);
        }
    }
}
