using System;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WordamentTests
{
    [TestClass]
    public class TestMakeDict
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void TestMakedDict()
        {
            uint dictNum = 1;
            int cnt = 0;
            var x = new MakeDictionary.OldDictWrapper(dictNum);
            var sb = new StringBuilder();
            foreach (var wrd in  x.GetWord("*"))
            {
                cnt++;
                sb.AppendLine(wrd);
                TestContext.WriteLine($"{wrd}");
            }
            Assert.Fail($"Got {cnt} words len= {sb.ToString().Length}");

        }
    }
}
