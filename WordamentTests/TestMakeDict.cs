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
            using (var fs = File.Open(fileName, FileMode.CreateNew))
            {
                fs.Seek(Marshal.SizeOf(dictHeader), SeekOrigin.Begin);
                //var str = "this is a test";
                //fs.Write(System.Text.ASCIIEncoding.ASCII.GetBytes(str), 0, str.Length);
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
                var bytesHeader = dictHeader.GetBytes();
                fs.Seek(0, SeekOrigin.Begin);
                fs.Write(dictHeader.GetBytes(), 0, bytesHeader.Length);
            }
            var xx = File.ReadAllBytes(fileName);
            TestContext.WriteLine($"{fileName} dump HdrSize= {Marshal.SizeOf(dictHeader)}  (0x{Marshal.SizeOf(dictHeader):x8})");
            TestContext.WriteLine(DumpBytes(xx));

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

                //var txt = fs.Read(bytes, 0, 10);
                //var tt= System.Text.ASCIIEncoding.ASCII.GetString(bytes, 0,10);
                return newheader;
            }
        }


        [TestMethod]
        public void TestGetResources()
        {
            TestContext.WriteLine($"{TestContext.TestName}  {DateTime.Now.ToString("MM/dd/yy hh:mm:ss")}");
            var x = Dictionary.Dictionary.GetResource();
            TestContext.WriteLine(DumpBytes(x));
                
        }

        public string DumpBytes(byte[] bytes, bool fIncludeCharRep = true)
        {
            StringBuilder sb = new StringBuilder();
            var addr = 0;
            var padLength = (16 - (bytes.Length % 16)) % 16;
            sb.AppendLine($"padlen = {padLength}");
            for (int i = 0; i < bytes.Length + padLength; i++, addr++)
            {
                if (i % 16 == 0) // beginning of new line
                {
                    sb.Append($"{addr:x8}  ");
                }
                else if (i % 8 == 0)
                {
                    sb.Append(" ");
                }

                var dat = i < bytes.Length ? bytes[i].ToString("x2") : "  ";
                sb.Append($" {dat}");
                if (i % 16 == 15) // we did the last on the line. Add the char rep
                {
                    if (fIncludeCharRep)
                    {
                        var charrep = string.Empty;
                        for (int j = i - 15; j <= i; j++)
                        {
                            if (j < bytes.Length)
                            {
                                var val = j < bytes.Length ? bytes[j] : 32;
                                var chr = Convert.ToChar(val);
                                if (!char.IsSymbol(chr) && !char.IsLetterOrDigit(chr) && !char.IsPunctuation(chr) && chr != ' ')
                                {
                                    chr = '.';
                                }
                                charrep += chr;
                            }
                        }
                        sb.AppendLine($"  {charrep}");
                    }
                    else
                    {
                        sb.AppendLine();
                    }
                }
            }
            return sb.ToString();

        }
    }
}
