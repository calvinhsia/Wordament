using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using MakeDictionary;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DictionaryLib;

namespace WordamentTests
{
    public class TestBase
    {
        public TestContext TestContext { get; set; }
        public void LogMessage(string msg)
        {
            TestContext.WriteLine(msg);
        }

        [TestInitialize]
        public void TestInit()
        {
            LogMessage($"Test Init {DateTime.Now.ToString()} {TestContext.TestName}");
        }
        public TestBase()
        {
            void LogMsgAction(string str) => LogMessage(str);
            DictionaryLib.DictionaryLib.logMessageAction = LogMsgAction;
            MakeDictionary.MakeDictionary.logMessageAction = LogMsgAction;
        }
    }

    [TestClass]
    public class TestDict : TestBase
    {

        [TestMethod]
        public void TestSeekWord()
        {
            var dict = new DictionaryLib.DictionaryLib(DictionaryType.Small);


            var x = dict.SeekWord("a", out var cres);


            foreach (var str in new[] { "zys", "me", "aband", "", "z", "mel", "asdf" })
            {
                var res = dict.SeekWord(str);
                LogMessage($"SeekWord {str}, {res}");
            }
            Assert.AreEqual(dict.SeekWord(""), "a");
            Assert.AreEqual(dict.SeekWord("me"), "me");
            Assert.AreEqual(dict.SeekWord("mel"), "melancholia");
            Assert.AreEqual(dict.SeekWord("aband"), "abandon");
            Assert.AreEqual(dict.SeekWord("asdf"), "asdic");

            var partial = dict.SeekWord("test", out var compResult);
            Assert.AreEqual(0, compResult);
            Assert.AreEqual("test", partial);

            partial = dict.SeekWord("testdddddd", out compResult);
            Assert.IsTrue(compResult > 0);
            Assert.AreEqual("tested", partial);

            partial = dict.SeekWord("contemptuousl", out compResult);
            Assert.IsTrue(compResult > 0);
            Assert.AreEqual("contemptuously", partial);

            partial = dict.SeekWord("contemplatio", out compResult);
            Assert.IsTrue(compResult > 0);
            Assert.AreEqual("contemplation", partial);

            partial = dict.SeekWord("contemplatip", out compResult);
            Assert.IsTrue(compResult > 0);
            Assert.AreEqual("contemplative", partial);

        }


        [TestMethod]
        public void TestFindMatchRegEx()
        {
            var dict = new DictionaryLib.DictionaryLib(DictionaryType.Small, new Random(1));

            var result = dict.FindMatchRegEx("melt*").ToList();
            Assert.AreEqual(131, result.Count);
            Assert.IsTrue(result.Contains("wholesomely"));

            result = dict.FindMatchRegEx("zz.*").ToList(); // all words with "zz"
            Assert.AreEqual(109, result.Count);
            Assert.IsTrue(result.Contains("jazz"));

            foreach (var str in new[] { "aband*", "^x.*", "asdfg*" })
            {
                var res = dict.FindMatchRegEx(str);
                LogMessage($"FindMatchRegEx {str}");
                int ndx = 0;
                foreach (var wrd in res)
                {
                    LogMessage($"           {ndx++,6}          {wrd}");
                }
            }
        }

        [TestMethod]
        public void TestDoAnagramOld()
        {
            var dict = new MakeDictionary.OldDictWrapper(1);
            var lstAnagrams = new List<string>();

            var x = dict.FindAnagrams("discounter");
            foreach (var w in x)
            {
                LogMessage($"xxxx  {w}");
            }
            foreach (var anagram in lstAnagrams)
            {
                LogMessage(anagram);
            }
        }

        [TestMethod]
        public void TestDoAnagram()
        {
            var dict = new DictionaryLib.DictionaryLib(DictionaryType.Large, new Random(1));
            var lstAnagrams = new List<string>();
            // relive, discounter, top
            var word = "discounter";
            //word = "aeivlsd";
            ////            word = "aughnnd";
            ////          word = "aeuyrqv";
            //word = "eauttsp";
            //word = "aeidlvr";
            //word = "aioutds";
            //word = "harigds";
            //word = "keepdan";
            LogMessage($"doing anagrams {word}");
            dict.FindAnagrams(word, DictionaryLib.DictionaryLib.AnagramType.WholeWord, (str) =>
            {
                lstAnagrams.Add(str);
                return true; // continue
            });
            Console.WriteLine($"Got {lstAnagrams.Count} results in {dict._nRecursionCnt:n0} calls");
            foreach (var anagram in lstAnagrams)
            {
                Console.WriteLine($"Found anagram {anagram}");
            }
            Assert.IsTrue(lstAnagrams.Contains("discounter"));
            Assert.IsTrue(lstAnagrams.Contains("rediscount"));
            Assert.IsTrue(lstAnagrams.Contains("introduces"));
            Assert.IsTrue(lstAnagrams.Contains("reductions"));
            Assert.AreEqual(20395, dict._nRecursionCnt);
        }

        [TestMethod]
        public void TestDoSubAnagrams()
        {
            var dict = new DictionaryLib.DictionaryLib(DictionaryType.Small, new Random(1));
            var lstAnagrams = new List<string>();
            var word = "count";
            var anagType = DictionaryLib.DictionaryLib.AnagramType.SubWord5;
            LogMessage($"doing subanagrams {anagType} {word}");
            dict.FindAnagrams(word,
                DictionaryLib.DictionaryLib.AnagramType.SubWord3,
                (str) =>
            {
                lstAnagrams.Add(str);
                return true; // continue
            });
            Console.WriteLine($"# anagrams found = {lstAnagrams.Count}");
            Console.WriteLine($"Got {lstAnagrams.Count} results in {dict._nRecursionCnt:n0} calls");
            foreach (var anagram in lstAnagrams)
            {
                Console.WriteLine($"Found anagram {anagram}");
            }
            Assert.IsTrue(lstAnagrams.Contains("con"));
            Assert.IsTrue(lstAnagrams.Contains("cont"));
            Assert.IsTrue(lstAnagrams.Contains("cot"));
            Assert.IsTrue(lstAnagrams.Contains("count"));
            Assert.IsTrue(lstAnagrams.Contains("cut"));
            Assert.IsTrue(lstAnagrams.Contains("not"));
            Assert.IsTrue(lstAnagrams.Contains("nut"));
            Assert.IsTrue(lstAnagrams.Contains("ont"));
            Assert.IsTrue(lstAnagrams.Contains("oct"));
            Assert.IsTrue(lstAnagrams.Contains("out"));
            Assert.IsTrue(lstAnagrams.Contains("ton"));
            Assert.IsTrue(lstAnagrams.Contains("unto"));
            Assert.AreEqual(12, lstAnagrams.Count);
            Assert.AreEqual(144, dict._nRecursionCnt);
        }

        [TestMethod]
        public void TestDoSubAnagrams3()
        {
            var dict = new DictionaryLib.DictionaryLib(DictionaryType.Small, new Random(1));
            var lstAnagrams = new List<string>();
            var word = "discount";
            var anagType = DictionaryLib.DictionaryLib.AnagramType.SubWord3;
            LogMessage($"doing subanagrams {anagType} {word}");
            dict.FindAnagrams(word,
                DictionaryLib.DictionaryLib.AnagramType.SubWord5,
                (str) =>
                {
                    lstAnagrams.Add(str);
                    return true; // continue
                });
            Console.WriteLine($"Got {lstAnagrams.Count} results in {dict._nRecursionCnt:n0} calls");
            foreach (var anagram in lstAnagrams)
            {
                Console.WriteLine($"Found anagram {anagram}");
            }
            Assert.IsTrue(lstAnagrams.Contains("tucson"));
            Assert.IsTrue(lstAnagrams.Contains("conduit"));
            Assert.IsTrue(lstAnagrams.Contains("donuts"));
            Assert.AreEqual(32, lstAnagrams.Count);
            Assert.AreEqual(1967, dict._nRecursionCnt);
        }


        [TestMethod]
        public void TestDoSubAnagrams5()
        {
            var dict = new DictionaryLib.DictionaryLib(DictionaryType.Small, new Random(1));
            var lstAnagrams = new List<string>();
            var word = "discount";
            var anagType = DictionaryLib.DictionaryLib.AnagramType.SubWord5;
            LogMessage($"doing subanagrams {anagType} {word}");
            dict.FindAnagrams(word, anagType,
                (str) =>
                {
                    lstAnagrams.Add(str);
                    return true; // continue
                });
            Console.WriteLine($"Got {lstAnagrams.Count} results in {dict._nRecursionCnt:n0} calls");
            foreach (var anagram in lstAnagrams)
            {
                Console.WriteLine($"Found anagram {anagram}");
            }
            Assert.IsTrue(lstAnagrams.Contains("tucson"));
            Assert.IsTrue(lstAnagrams.Contains("conduit"));
            Assert.IsTrue(lstAnagrams.Contains("donuts"));
            Assert.AreEqual(32, lstAnagrams.Count);
            Assert.AreEqual(1967, dict._nRecursionCnt);
        }

        [TestMethod]
        public void TestDoSubWords3Long()
        {
            var dict = new DictionaryLib.DictionaryLib(DictionaryType.Small, new Random(1));
            var lstAnagrams = new List<string>();
            var word = "counterrevolutionary";
            var anagType = DictionaryLib.DictionaryLib.AnagramType.SubWord3;
            LogMessage($"doing subword {anagType} {word}");
            foreach (var subword in dict.FindSubWordsFromLetters(word, anagType))
            {
                lstAnagrams.Add(subword);
            }
            Console.WriteLine($"Got {lstAnagrams.Count} results in {dict._nRecursionCnt:n0} calls");
            foreach (var anagram in lstAnagrams)
            {
                Console.WriteLine($"Found subword {anagram}");
            }
            Assert.IsTrue(lstAnagrams.Contains("accountancy"));
            Assert.IsTrue(lstAnagrams.Contains("act"));
            Assert.IsTrue(lstAnagrams.Contains("vaccine"));
            Assert.AreEqual(2400, lstAnagrams.Count);
        }


        [TestMethod]
        public void TestDoFindSubWordsFromLetters()
        {
            var dict = new DictionaryLib.DictionaryLib(DictionaryType.Small, new Random(1));
            var lstAnagrams = new List<string>();
            var word = "discountttt";
            var anagType = DictionaryLib.DictionaryLib.AnagramType.SubWord5;
            LogMessage($"doing subwords {anagType} {word}");
            foreach (var subword in dict.FindSubWordsFromLetters(word,
                DictionaryLib.DictionaryLib.AnagramType.SubWord5))
            {
                lstAnagrams.Add(subword);
            }
            Console.WriteLine($"# subwords found = {lstAnagrams.Count}");
            foreach (var anagram in lstAnagrams)
            {
                Console.WriteLine($"Found subwords {anagram}");
            }
            Assert.IsTrue(lstAnagrams.Contains("concussion"));
            Assert.IsTrue(lstAnagrams.Contains("tucson"));
            Assert.IsTrue(lstAnagrams.Contains("conduit"));
            Assert.IsTrue(lstAnagrams.Contains("donuts"));
            Assert.AreEqual(158, lstAnagrams.Count);
        }


        [TestMethod]
        public void TestPerfRandWord()
        {
            var oldDict = new OldDictWrapper(1);
            var newdict = new DictionaryLib.DictionaryLib(DictionaryType.Large, new Random(1));
            var sw = new Stopwatch();
            sw.Start();
            var nCnt = 10000;
            for (int i = 0; i < nCnt; i++)
            {
                var r = oldDict.RandWord(0);
            }
            var olddictTime = sw.Elapsed.TotalSeconds;
            LogMessage($"Olddict {sw.Elapsed.TotalSeconds}");
            sw.Restart();
            for (int i = 0; i < nCnt; i++)
            {
                var r = newdict.RandomWord();
            }
            var newdictTime = sw.Elapsed.TotalSeconds;
            LogMessage($"Newdict {newdictTime}");
            Assert.Fail($"This test is supposed to fail to show perf results: OldDict {olddictTime:n1} newdict {sw.Elapsed.TotalSeconds:n1}  Ratio {newdictTime / olddictTime:n1}");
        }

        [TestMethod]
        public void TestPerfIsWord()
        {
            var oldDict = new OldDictWrapper(1);
            var newdict = new DictionaryLib.DictionaryLib(DictionaryType.Large, new Random(1));
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
            LogMessage($"Olddict {sw.Elapsed.TotalSeconds}");
            sw.Restart();
            for (int i = 0; i < nCnt; i++)
            {
                var r = newdict.IsWord(word);
                Assert.IsTrue(r);
            }
            var newdictTime = sw.Elapsed.TotalSeconds;
            LogMessage($"Newdict {newdictTime}");
            Assert.Fail($"This test is supposed to fail to show perf results: OldDict {olddictTime:n1} newdict {sw.Elapsed.TotalSeconds:n1}  Ratio {newdictTime / olddictTime:n1}");
        }

        [TestMethod]
        [Ignore]
        public void TestPerfForTrace()
        {
            var oldDict = new OldDictWrapper(1);
            var newdict = new DictionaryLib.DictionaryLib(DictionaryType.Large, new Random(1));
            var sw = new Stopwatch();
            sw.Start();
            var nCnt = 5000;
            //for (int i = 0; i < nCnt; i++)
            //{
            //    var r = oldDict.RandWord(0);
            //}
            //LogMessage($"Olddict {sw.Elapsed.TotalSeconds}");
            sw.Restart();
            for (int i = 0; i < nCnt; i++)
            {
                var r = newdict.RandomWord();
                var x = newdict.SeekWord(r);
                var xx = newdict.IsWord(x);
            }
            LogMessage($"Newdict {sw.Elapsed.TotalSeconds}");
        }

        [TestMethod]
        public void TestDictLongWord()
        {
            var dict = new DictionaryLib.DictionaryLib(DictionaryType.Small);
            var longwords = new[] { "nonparticipating", "precautionary" };
            foreach (var longWord in longwords)
            {
                for (int i = longWord.Length; i >= 0; i--)
                {
                    var word = longWord.Substring(0, i);
                    var SeekWord = dict.SeekWord(word);
                    LogMessage($"SeekWord {SeekWord} '{word}'");
                }
            }

            foreach (var longWord in longwords)
            {
                for (int i = longWord.Length; i >= 0; i--)
                {
                    var word = longWord.Substring(0, i);
                    var isword = dict.IsWord(word);
                    LogMessage($"isword {isword} {word}");
                }
            }
        }

        [TestMethod]
        public void TestDictIsWord()
        {
            var lstOldWords = GetOldDictWords(2);
            var dict = new DictionaryLib.DictionaryLib(DictionaryType.Small);

            var w = dict.IsWord("sinoiaterrpze");
            Assert.IsFalse(w);
            foreach (var word in lstOldWords)
            {
                if (word.Length > 1)
                {
                    //Assert.IsTrue(dict.IsWord(word), $"{word}");
                    LogMessage($"{dict.IsWord(word)} {word}");
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
            var dict = new DictionaryLib.DictionaryLib(DictionaryType.Small, new Random(1));
            for (int i = 0; i < 1000; i++)
            {
                var r = dict.RandomWord();
                LogMessage($"rand {r}");
            }
        }

        [TestMethod]
        public void TestMakedDict()
        {
            LogMessage($"{TestContext.TestName}  {DateTime.Now.ToString("MM/dd/yy hh:mm:ss")}");
            var lstSmall = GetOldDictWords((uint)DictionaryType.Small);
            var lstlarge = GetOldDictWords((uint)DictionaryType.Large);
            var hashLarge = new HashSet<string>();
            foreach (var wrd in lstlarge)
            {
                hashLarge.Add(wrd);
            }
            foreach (var wrd in lstSmall)
            {
                if (!hashLarge.Contains(wrd))
                {
                    hashLarge.Add(wrd);
                    //                    Console.WriteLine($"sm not in lrg = {wrd}");
                }
            }
            foreach (var str in new[]
            {
                "miscinceptions",
                "substantialia",
                "nonconfin",
                "surdity"
            })
            {
                hashLarge.Remove(str);
            }
            //todo: remove "substantialia", "nonconfin", "surdity"

            // when changing contents of dictionary, this test will fail until you update the resources, 
            // XCOPY /dy C:\Users\calvinh\Source\Repos\Wordament\WordamentTests\bin\Debug\*.bin C:\Users\calvinh\Source\Repos\Wordament\Dictionary\Resources

            for (uint dictNum = 1; dictNum <= 2; dictNum++)
            {
                List<string> lstWords = null;
                if ((DictionaryType)dictNum == DictionaryType.Small)
                {
                    lstWords = lstSmall;
                }
                else
                {
                    lstWords = hashLarge.OrderBy(s => s).ToList();
                }
                LogMessage($"DictSect {dictNum} NumWords = {lstWords.Count}");
                //            var fileName = @"C:\Users\calvinh\Source\Repos\Wordament\MakeDictionary\Resources\dict.bin";
                var fileName = Path.Combine(Environment.CurrentDirectory, $@"dict{dictNum}.bin");
                MakeDictionary.MakeDictionary.MakeBinFile(lstWords, fileName, dictNum);

                LogMessage($"DictSect {dictNum}  Dictionary NibTable");
                var dictBytes = File.ReadAllBytes(fileName);
                MakeDictionary.MakeDictionary.DumpDict(fileName);

                LogMessage($"DictSect {dictNum}  Raw Bytes");
                LogMessage(DumpBytes(dictBytes));

                // note: this will now read the resources of Dictionary.dll, not the just generated dumpfile, so need to update it if dictHeader struct changes
                var dict = new DictionaryLib.DictionaryLib((DictionaryType)dictNum);
                var newlstWord = new List<string>();
                var word = dict.SeekWord("");
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
                Assert.AreEqual(newlstWord.Count(), lstWords.Count(), $"dict num {dictNum} ");
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
