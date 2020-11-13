using Biocs.TestTools;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Biocs
{
    [TestClass]
    public class ResTest
    {
        [TestMethod]
        public void StringResource_Test()
            => StringResourceTester.CheckStringResource(typeof(StringResourceUsageAttribute).Assembly.GetType("Biocs.Res"));
    }
}
