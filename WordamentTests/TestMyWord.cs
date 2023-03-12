using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DictionaryLib;

namespace WordamentTests
{
    [TestClass]
    public class TestMyWord
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void TestMyWordWorks()
        {
            var w1 = new MyWord();
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
        [TestMethod]
        public void TestMyWordSubstring()
        {
            var w1 = new MyWord("testing");
            var s = w1.Substring(2);
            Assert.AreEqual("sting",s.GetWord());
            var s2 = w1.Substring(3, 4);
            Assert.AreEqual("ting", s2.GetWord());

            var s3 = w1.Substring(7);
            Assert.AreEqual("", s3.GetWord());
        }
    }
}
