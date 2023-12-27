using Biocs.TestTools;

namespace Biocs;

[TestClass]
public class ResTest
{
    [TestMethod]
    public void StringResource_Test()
        => StringResourceTester.CheckStringResource(typeof(StringResourceUsageAttribute).Assembly.GetType("Biocs.Res")!);
}
