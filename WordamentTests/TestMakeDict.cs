using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using MakeDictionary;
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

            for (uint dictNum = 2; dictNum <= 2; dictNum++)
            {
                var lstWords = new List<string>();
                using (var dictWrapper = new OldDictWrapper(dictNum))
                {
                    lstWords.AddRange(dictWrapper.GetWords("*"));
                }
                //            var fileName = @"C:\Users\calvinh\Source\Repos\Wordament\MakeDictionary\Resources\dict.bin";
                var fileName = Path.Combine(Environment.CurrentDirectory, $@"dict{dictNum}.bin");
                MakeBinFile(lstWords, fileName);
            }


            //var mm = new MakeDictionary.MakeDictionary();
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
        void MakeBinFile(List<string> words, string fileName)
        {
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }
            DictHeader dictHeader = new DictHeader();
            dictHeader.tab1 = " acdegilmnorstu"; // 0th entry isn't used: 15 is escape to next table
            dictHeader.tab2 = " bfhjkpqvwxzy"; // 0th entry isn't used
            dictHeader.lookupTab1 = new byte[16];
            dictHeader.lookupTab2 = new byte[16];
            void initTab(byte[] lookupTab, string data)
            {
                int ndx = 0;
                foreach (var c in data)
                {
                    var b = Convert.ToByte(c);
                    //var enc = Encoding.ASCII.GetBytes(new char[] { c });
                    lookupTab[ndx++] = b;
                }
            }
            initTab(dictHeader.lookupTab1, dictHeader.tab1);
            initTab(dictHeader.lookupTab2, dictHeader.tab2);
            dictHeader.nibPairPtr = new DictHeaderNibbleEntry[26 * 26];
            var nibpairNdx = 0;
            var letndx1 = 97; // from 'a'
            var letndx2 = 97; // from 'a'
            var let1 = Convert.ToChar(letndx1);
            var let2 = Convert.ToChar(letndx2);
            var curNib = 0;
            var nkeepSoFar = 0;
            var wordSofar = "a";
            foreach (var word in words)
            {
                dictHeader.wordCount++;
                if (word[0] == let1 && (word.Length < 2 || word[1] == let2))
                {
                    // same bucket
                    dictHeader.nibPairPtr[nibpairNdx].cnt++;
                    for (int i = 1; i < wordSofar.Length && i < word.Length; i++)
                    {
                        if (wordSofar[i] == word[i])
                        {

                        }
                    }
                    wordSofar = word;
                }
                else
                { // diff bucket
                  //                    TestContext.WriteLine($"Add  {let1} {let2} {wordSofar} {curnibbleEntry}");
                    ++nibpairNdx;
                    if (let2 == 'z')
                    {
                        let2 = 'a';
                        letndx2 = Convert.ToInt32('a');
                        let1 = Convert.ToChar(++letndx1);
                    }
                    else
                    {
                        let2 = Convert.ToChar(++letndx2);
                    }
                }
            }
            //            dictHeader.nibPairPtr = lstnbiPairEntry.ToArray();
            using (var fs = File.Open(fileName, FileMode.CreateNew))
            {
                var bytesHeader = dictHeader.GetBytes();
                fs.Write(dictHeader.GetBytes(), 0, bytesHeader.Length);
            }
            var that = ReadDictFromFile(fileName);
            Assert.IsTrue(dictHeader.Equals(that));
        }

        DictHeader ReadDictFromFile(string fileName)
        {
            using (var fs = File.OpenRead(fileName))
            {
                var size = Marshal.SizeOf<DictHeader>();
                var bytes = new byte[size];
                var nbytes = fs.Read(bytes, 0, size);
                var newheader = DictHeader.MakeHeaderFromBytes(bytes);
                return newheader;
            }
        }
    }
}
