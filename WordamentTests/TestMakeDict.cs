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
            TestContext.WriteLine($"{TestContext.TestName}  {DateTime.Now.ToString("MM/dd/yy hh:mm:ss")}");
            var rsrc = Dictionary.Properties.Resources.dict1;
            TestContext.WriteLine($"Got resources size = {rsrc.Length}");
            var mm = new MakeDictionary.MakeDictionary();
            //foreach (var tentry in mm.dictHeader.lookupTab1)
            //{
            //    TestContext.WriteLine($"x {tentry}");
            //}

            //uint dictNum = 1;
            //int cnt = 0;
            //var x = new MakeDictionary.OldDictWrapper(dictNum);
            //var sb = new StringBuilder();
            //foreach (var wrd in  x.GetWords("*"))
            //{
            //    cnt++;
            //    sb.AppendLine(wrd);
            //    TestContext.WriteLine($"{wrd}");
            //}
            //Assert.Fail($"Got {cnt} words len= {sb.ToString().Length}");

        }
    }
}
