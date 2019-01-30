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
        public void TestFindMatch()
        {
            var dict = new Dictionary.Dictionary(Dictionary.DictionaryType.Small, randSeed: 0);
            foreach (var str in new[] { "me*","aband*", "*", "z*", "mel*", "asdf*"})
            {
                var res = dict.FindMatch(str);
                Console.WriteLine($"FindMatch {str}, {res}");
            }
            Assert.AreEqual(dict.FindMatch("me*"), "mea");
            Assert.AreEqual(dict.FindMatch("mel*"), "melancholia");
            Assert.AreEqual(dict.FindMatch("aband*"), "abandon");
            Assert.AreEqual(dict.FindMatch("asdf*"), string.Empty);
        }

        [TestMethod]
        public void TestDoAnagram()
        {
            var dict = new Dictionary.Dictionary(Dictionary.DictionaryType.Small, randSeed: 0);
            throw new NotImplementedException();
        }


        [TestMethod]
        public void TestPerfRandWord()
        {
            var oldDict = new OldDictWrapper(1);
            var newdict = new Dictionary.Dictionary(Dictionary.DictionaryType.Large, randSeed: 0);
            var sw = new Stopwatch();
            sw.Start();
            var nCnt = 100000;
            for (int i = 0; i < nCnt; i++)
            {
                var r = oldDict.RandWord(0);
            }
            var olddictTime = sw.Elapsed.TotalSeconds;
            Console.WriteLine($"Olddict {sw.Elapsed.TotalSeconds}");
            sw.Restart();
            for (int i = 0; i < nCnt; i++)
            {
                var r = newdict.RandomWord();
            }
            var newdictTime = sw.Elapsed.TotalSeconds;
            Console.WriteLine($"Newdict {newdictTime}");
            Assert.Fail($"OldDict {olddictTime:n1} newdict {sw.Elapsed.TotalSeconds:n1}  {newdictTime/olddictTime:n1}");
        }
        [TestMethod]
        public void TestPerfIsWord()
        {
            var oldDict = new OldDictWrapper(1);
            var newdict = new Dictionary.Dictionary(Dictionary.DictionaryType.Large, randSeed: 0);
            var sw = new Stopwatch();
            sw.Start();
            var nCnt = 10000;
            var word = "computer";
            for (int i = 0; i < nCnt; i++)
            {
                var r = oldDict.IsWord(word);
                Assert.IsTrue(r);
            }
            var olddictTime = sw.Elapsed.TotalSeconds;
            Console.WriteLine($"Olddict {sw.Elapsed.TotalSeconds}");
            sw.Restart();
            for (int i = 0; i < nCnt; i++)
            {
                var r = newdict.IsWord(word);
                Assert.IsTrue(r);
            }
            var newdictTime = sw.Elapsed.TotalSeconds;
            Console.WriteLine($"Newdict {newdictTime}");
            Assert.Fail($"OldDict {olddictTime:n1} newdict {sw.Elapsed.TotalSeconds:n1}  {newdictTime / olddictTime:n1}");
        }

        [TestMethod]
        public void TestPerfForTrace()
        {
            var oldDict = new OldDictWrapper(1);
            var newdict = new Dictionary.Dictionary(Dictionary.DictionaryType.Large, randSeed: 0);
            var sw = new Stopwatch();
            sw.Start();
            var nCnt = 100000;
            //for (int i = 0; i < nCnt; i++)
            //{
            //    var r = oldDict.RandWord(0);
            //}
            //Console.WriteLine($"Olddict {sw.Elapsed.TotalSeconds}");
            sw.Restart();
            for (int i = 0; i < nCnt; i++)
            {
                var r = newdict.RandomWord();
            }
            Console.WriteLine($"Newdict {sw.Elapsed.TotalSeconds}");
        }

        [TestMethod]
        public void TestDictIsWord()
        {
            var lstOldWords = GetOldDictWords(2);
            var dict = new Dictionary.Dictionary(Dictionary.DictionaryType.Small);

            var w = dict.IsWord("our");
            Assert.IsTrue(w);
            foreach (var word in lstOldWords)
            {
                if (word.Length > 1)
                {
                    //Assert.IsTrue(dict.IsWord(word), $"{word}");
                    Console.WriteLine($"{dict.IsWord(word)} {word}");
                }
            }

            Assert.IsTrue(dict.IsWord("Abandon"));
            var sentence = "four score and seven years ago our fathers brought forth on this continent a new nation conceived in liberty and dedicated to the proposition that all men are created equal";

            foreach (var wrd in sentence.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                Assert.IsTrue(dict.IsWord(wrd), $"{wrd}");
                Assert.IsFalse(dict.IsWord("dd" + wrd), $"{wrd}");
            }

            Assert.IsTrue(dict.IsWord("contemporary"));
            Assert.IsTrue(dict.IsWord("police"));
            Assert.IsFalse(dict.IsWord("pollice"));
        }

        [TestMethod]
        public void TestRandWord()
        {
            var dict = new Dictionary.Dictionary(Dictionary.DictionaryType.Small, randSeed: 1);
            for (int i = 0; i < 1000; i++)
            {
                var r = dict.RandomWord();
                Console.WriteLine($"rand {r}");
            }
        }

        [TestMethod]
        public void TestMakedDict()
        {
            Console.WriteLine($"{TestContext.TestName}  {DateTime.Now.ToString("MM/dd/yy hh:mm:ss")}");

            for (uint dictNum = 1; dictNum <= 2; dictNum++)
            {
                var lstWords = GetOldDictWords(dictNum);
                Console.WriteLine($"DictSect {dictNum} NumWords = {lstWords.Count}");
                //            var fileName = @"C:\Users\calvinh\Source\Repos\Wordament\MakeDictionary\Resources\dict.bin";
                var fileName = Path.Combine(Environment.CurrentDirectory, $@"dict{dictNum}.bin");
                MakeDictionary.MakeDictionary.MakeBinFile(lstWords, fileName);

                Console.WriteLine($"DictSect {dictNum}  Dictionary NibTable");
                var dictBytes = File.ReadAllBytes(fileName);
                MakeDictionary.MakeDictionary.DumpDict(fileName);

                Console.WriteLine($"DictSect {dictNum}  Raw Bytes");
                Console.WriteLine(DumpBytes(dictBytes));

                // note: this will now read the resources of Dictionary.dll, not the just generated dumpfile, so need to update it if dictHeader struct changes
                var dict = new Dictionary.Dictionary((Dictionary.DictionaryType)dictNum);
                var newlstWord = new List<string>();
                var word = dict.FindMatch("*");
                while (!string.IsNullOrEmpty(word))
                {
                    newlstWord.Add(word);
                    word = dict.GetNextWord();
                }
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
                Assert.AreEqual(newlstWord.Count(), lstWords.Count(), $"dict num {dictNum} " );
                for (int i = 0; i < lstWords.Count; i++)
                {
                    Assert.AreEqual(lstWords[i], newlstWord[i]);
                }
            }
        }

        private List<string> GetOldDictWords(uint dictNum)
        {
            var lstWords = new List<string>();
            using (var dictWrapper = new OldDictWrapper(dictNum))
            {
                lstWords.AddRange(dictWrapper.GetWords("*"));
            }
            return lstWords;
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
