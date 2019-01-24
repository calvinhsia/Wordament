using System;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WordamentTests
{
    [TestClass]
    public class TestMakeDict
    {
        [TestMethod]
        public void TestMakedDict()
        {
            int dictNum = 1;
            var x = new MakeDictionary.MakeDictionary(dictNum);
            var sb = new StringBuilder();
            foreach (var wrd in  x.GetWord("a*"))
            {
                sb.AppendLine(wrd);
            }
            Assert.Fail(sb.ToString());

        }
    }
}
