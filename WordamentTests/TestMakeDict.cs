using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
        public void TestDictIsWord()
        {
            var dict = new Dictionary.Dictionary(Dictionary.DictionaryType.Small);
            Assert.IsTrue(dict.IsWord("Abandon"));
            Assert.IsTrue(dict.IsWord("contemporary"));
            Assert.IsTrue(dict.IsWord("police"));
            Assert.IsFalse(dict.IsWord("pollice"));
        }

        [TestMethod]
        public void TestRandWord()
        {
            var dict = new Dictionary.Dictionary(Dictionary.DictionaryType.Small, randSeed: 1);
            var r = dict.RandomWord();
            Console.WriteLine($"rand {r}");

        }

        [TestMethod]
        public void TestMakedDict()
        {
            Console.WriteLine($"{TestContext.TestName}  {DateTime.Now.ToString("MM/dd/yy hh:mm:ss")}");

            for (uint dictNum = 1; dictNum <= 2; dictNum++)
            {
                var lstWords = new List<string>();
                using (var dictWrapper = new OldDictWrapper(dictNum))
                {
                    lstWords.AddRange(dictWrapper.GetWords("*"));
                }
                Console.WriteLine($"DictSect {dictNum} NumWords = {lstWords.Count}");
                //            var fileName = @"C:\Users\calvinh\Source\Repos\Wordament\MakeDictionary\Resources\dict.bin";
                var fileName = Path.Combine(Environment.CurrentDirectory, $@"dict{dictNum}.bin");
                MakeDictionary.MakeDictionary.MakeBinFile(lstWords.Take(10000000), fileName);

                Console.WriteLine($"DictSect {dictNum}  Dictionary NibTable");
                var dictBytes = File.ReadAllBytes(fileName);
                MakeDictionary.MakeDictionary.DumpDict(fileName);

                Console.WriteLine($"DictSect {dictNum}  Raw Bytes");
                Console.WriteLine(DumpBytes(dictBytes));


                var dict = new Dictionary.Dictionary((Dictionary.DictionaryType)dictNum);
                //var newlstWord = new List<string>();
                var newlstWord = dict.FindMatch("*");
                //newlstWord.Add(result.Word);
                //while (true)
                //{
                //    result = result.GetNextResult();
                //    if (result == null)
                //    {
                //        break;
                //    }
                //    newlstWord.Add(result.Word);
                //}

                //                var newlstWord = DictionaryData.DictionaryUtil.ReadDict(dictBytes);
                Assert.AreEqual(newlstWord.Count(), lstWords.Count());
                for (int i = 0; i < lstWords.Count; i++)
                {
                    Assert.AreEqual(lstWords[i], newlstWord[i]);
                }
            }
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
