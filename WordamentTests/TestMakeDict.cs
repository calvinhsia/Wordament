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
    public class TestBase
    {
        public TestContext TestContext { get; set; }
        public void LogMessage(string msg)
        {
            TestContext.WriteLine(msg);
        }
        public TestBase()
        {
            void LogMsgAction(string str) => LogMessage(str);
            Dictionary.Dictionary.logMessageAction = LogMsgAction;
            MakeDictionary.MakeDictionary.logMessageAction = LogMsgAction;
        }
    }

    [TestClass]
    public class TestMakeDict: TestBase
    {

        [TestMethod]
        public void TestSeekWord()
        {
            var dict = new Dictionary.Dictionary(Dictionary.DictionaryType.Small, new Random(1));
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
        }


        [TestMethod]
        public void TestFindMatchRegEx()
        {
            var dict = new Dictionary.Dictionary(Dictionary.DictionaryType.Small, new Random(1));
            foreach (var str in new[] { "me*", "aband*", "*", "z*", "mel*", "asdf*" })
            {
                var res = dict.FindMatchRegEx(str);
                LogMessage($"FindMatchRegEx {str}, {res}");
            }
            throw new NotImplementedException();
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
            var dict = new Dictionary.Dictionary(Dictionary.DictionaryType.Large, new Random(1));
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
            dict.FindAnagrams(word, (str) =>
            {
                lstAnagrams.Add(str);
            });
            foreach (var anagram in lstAnagrams)
            {
                Console.WriteLine($"Found anagram {anagram}");
            }
            Assert.IsTrue(lstAnagrams.Contains("discounter"));
            Assert.IsTrue(lstAnagrams.Contains("rediscount"));
            Assert.IsTrue(lstAnagrams.Contains("introduces"));
            Assert.IsTrue(lstAnagrams.Contains("reductions"));
        }


        [TestMethod]
        public void TestPerfRandWord()
        {
            var oldDict = new OldDictWrapper(1);
            var newdict = new Dictionary.Dictionary(Dictionary.DictionaryType.Large, new Random(1));
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
            Assert.Fail($"OldDict {olddictTime:n1} newdict {sw.Elapsed.TotalSeconds:n1}  {newdictTime / olddictTime:n1}");
        }

        [TestMethod]
        public void TestPerfIsWord()
        {
            var oldDict = new OldDictWrapper(1);
            var newdict = new Dictionary.Dictionary(Dictionary.DictionaryType.Large, new Random(1));
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
            Assert.Fail($"OldDict {olddictTime:n1} newdict {sw.Elapsed.TotalSeconds:n1}  {newdictTime / olddictTime:n1}");
        }

        [TestMethod]
        public void TestPerfForTrace()
        {
            var oldDict = new OldDictWrapper(1);
            var newdict = new Dictionary.Dictionary(Dictionary.DictionaryType.Large, new Random(1));
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
            var dict = new Dictionary.Dictionary(Dictionary.DictionaryType.Small);
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
            var dict = new Dictionary.Dictionary(Dictionary.DictionaryType.Small);

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
            var dict = new Dictionary.Dictionary(Dictionary.DictionaryType.Small, new Random(1));
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

            for (uint dictNum = 1; dictNum <= 2; dictNum++)
            {
                var lstWords = GetOldDictWords(dictNum);
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
                var dict = new Dictionary.Dictionary((Dictionary.DictionaryType)dictNum);
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
