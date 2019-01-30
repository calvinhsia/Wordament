using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dictionary;

namespace WordamentTests
{
    [TestClass]
    public class TestMyWord
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void TestMyWordWorks()
        {
            var w1 = new MyWord(30);
            w1.SetWord("test");
            var w2 = new MyWord("test");
            Assert.IsTrue(w1.CompareTo(w2) == 0);
            w2.AddByte(Convert.ToByte('a'));

            Assert.IsTrue(w1.CompareTo(w2) < 0);


            var res = new MyWord("foo").CompareTo(new MyWord("bar"));
            Assert.IsTrue(res > 0);

            Assert.IsTrue(new MyWord("foo").CompareTo(new MyWord("foo")) == 0);

            Assert.IsTrue(new MyWord("foo").CompareTo(new MyWord("food")) < 0);

            Assert.IsTrue(new MyWord("food").CompareTo(new MyWord("foo")) > 0);



        }
    }
}
