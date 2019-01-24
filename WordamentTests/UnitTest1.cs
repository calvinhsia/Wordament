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
            int dictNum = 2;
            int cnt = 0;
            var x = new MakeDictionary.MakeDictionary(dictNum);
            var sb = new StringBuilder();
            foreach (var wrd in  x.GetWord("*"))
            {
                cnt++;
                sb.AppendLine(wrd);
            }
            Assert.Fail($"Got {cnt} words");

        }
    }
}
