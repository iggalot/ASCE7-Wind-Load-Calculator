using Microsoft.VisualStudio.TestTools.UnitTesting;

[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]

namespace ASCE7Wind.Tests
{
    [TestClass]
    public class Smoke
    {
        [TestMethod]
        public void Harness_Runs() => Assert.IsTrue(true);
    }
}
