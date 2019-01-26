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
            DictHeader dictHeader = new DictHeader
            {
                tab1 = " acdegilmnorstu", // A 0 in the nib stream indicates end of word, so the 0th entry isn't used: 15 is escape to next table
                tab2 = " bfhjkpqvwxzy", // 0th entry isn't used
                lookupTab1 = new byte[16],
                lookupTab2 = new byte[16]
            };
            void initTab(byte[] lookupTab, string data)
            {
                int ndx = 0;
                foreach (var c in data)
                {
                    var b = Convert.ToByte(c);
                    if (!char.IsLetter(c))
                    {
                        b = 0;
                    }
                    else
                    {
                        b -= 0x60; // a == 1, b == 2, etc
                    }
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
            var wordSofar = string.Empty;
            var curNibNdx = 0;
            var priorNibNdx = 0;
            using (var fs = File.Open(fileName, FileMode.CreateNew))
            {
                var havePartialByte = false;
                byte partialByte = 0;
                void AddNib(byte nib)
                {
                    Debug.Assert(nib < 16);
                    curNibNdx++;
                    if (!havePartialByte)
                    {
                        partialByte = (byte)(nib << 4);
                    }
                    else
                    {
                        partialByte += nib;
                        fs.WriteByte(partialByte);
                    }
                    havePartialByte = !havePartialByte;
                }
                void addChar(char chr)
                {
                    var ndx = dictHeader.tab1.IndexOf(chr);
                    if (ndx > 0)
                    {
                        AddNib((byte)ndx);
                    }
                    else
                    {
                        if (chr == 0) // EOW
                        {
                            ndx = 0;
                        }
                        else
                        {
                            ndx = dictHeader.tab2.IndexOf(chr);
                        }
                        Debug.Assert(ndx >= 0 && ndx < 15);
                        AddNib(DictHeader.escapeChar);
                        AddNib((byte)ndx);
                    }
                }
                fs.Seek(Marshal.SizeOf(dictHeader), SeekOrigin.Begin);
                //var str = "this is a test";
                //fs.Write(System.Text.ASCIIEncoding.ASCII.GetBytes(str), 0, str.Length);
                foreach (var word in words)
                {
                    dictHeader.wordCount++;
                    // each word starts with the length to keep from the prior word. e.g. from "common" to "computer", the 1st 3 letters are the same, so lenToKeep = 3
                    // All the encoded data after the header 
                    // encoding an int length 
                    // So the length consists of a nib indicating how many chars to keep from prior word. If that nibPrior == 15, then the keep = nibPrior + the next nib

                    var nkeepSoFar = 0;
                    for (int i = 0; i < wordSofar.Length; i++)
                    {
                        if (i < word.Length && wordSofar[i] == word[i])
                        {
                            nkeepSoFar++;
                        }
                        else
                        {
                            break;
                        }
                    }
                    while (nkeepSoFar > 15)
                    {
                        AddNib(15);
                        nkeepSoFar -= 15;
                    }
                    AddNib((byte)nkeepSoFar);
                    wordSofar = word;
                    for (int i = nkeepSoFar; i < word.Length; i++)
                    {
                        addChar(word[i]);
                    }
                    addChar((char)0);// indicate EndOfWord

                    if (word[0] == let1 && (word.Length < 2 || word[1] == let2))
                    {
                        // same bucket
                        dictHeader.nibPairPtr[nibpairNdx].cnt++;
                        dictHeader.nibPairPtr[nibpairNdx].nibbleOffset = priorNibNdx;
                    }
                    else
                    { // diff bucket
                      //                    TestContext.WriteLine($"Add  {let1} {let2} {wordSofar} {curnibbleEntry}");
                        priorNibNdx = curNibNdx;
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
                if (havePartialByte)
                {
                    AddNib(0); // write out last nibble
                }
                var bytesHeader = dictHeader.GetBytes();
                fs.Seek(0, SeekOrigin.Begin);
                fs.Write(dictHeader.GetBytes(), 0, bytesHeader.Length);
            }
            var xx = File.ReadAllBytes(fileName);
            var wordSoFar2 = string.Empty;
            int cnt = 1000;
            var nibBaseNdx = Marshal.SizeOf(dictHeader);
            var nibndx = 0;

            byte partialNib = 0;
            var havePartialNib = false;
            byte GetNextNib()
            {
                byte result;
                if (havePartialNib)
                {
                    result = partialNib;
                }
                else
                {
                    partialNib = xx[nibBaseNdx + nibndx++];
                    result = (byte)(partialNib >> 4);
                    partialNib = (byte)(partialNib & 0xf);
                }
                havePartialNib = !havePartialNib;
                return result;
            }
            var lenSoFar = 0;
            while (cnt-- > 0)
            {
                byte nib;
                while ((nib = GetNextNib()) == 0xf)
                {
                    lenSoFar += nib;
                }
                lenSoFar += nib;
                while ((nib = GetNextNib()) != 0)
                {
                    var newchar = (char)nib + 'a' - 1;
                    wordSoFar2 += newchar;
                }
            }
            TestContext.WriteLine($"{fileName} dump HdrSize= {Marshal.SizeOf(dictHeader)}  (0x{Marshal.SizeOf(dictHeader):x8})");
            TestContext.WriteLine($"Entire dump len= {xx.Length}");
            TestContext.WriteLine(DumpBytes(xx));

            var that = ReadDictHeaderFromFile(fileName);
            Assert.IsTrue(dictHeader.Equals(that));
        }


        DictHeader ReadDictHeaderFromFile(string fileName)
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
