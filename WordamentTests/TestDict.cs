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
using Wordament;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Configs;

namespace WordamentTests
{
    public class TestBase
    {
        public TestContext TestContext { get; set; }
        public void LogMessage(string msg)
        {
            if (Debugger.IsAttached)
            {
                System.Diagnostics.Debug.WriteLine(msg);
            }
            TestContext.WriteLine(msg);
        }

        [TestInitialize]
        public void TestInit()
        {
            LogMessage($"Test Init {DateTime.Now} {TestContext.TestName}");
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
        public void TestResources()
        {
            var asm = typeof(DictionaryLib.DictionaryLib).Assembly;
//            var asm = System.Reflection.Assembly.GetExecutingAssembly();
            var names = asm.GetManifestResourceNames(); // "Dictionary.Properties.Resources.resources"

            Assert.AreEqual(1, names.Length);
            Assert.AreEqual("DictionaryLib.Properties.Resources.resources", names[0]);
            var resInfo = asm.GetManifestResourceInfo(names[0]);

            Assert.IsTrue(resInfo.ResourceLocation.HasFlag(System.Reflection.ResourceLocation.Embedded));
            Assert.IsTrue(resInfo.ResourceLocation.HasFlag(System.Reflection.ResourceLocation.ContainedInManifestFile));
            var resdata = asm.GetManifestResourceStream(names[0]);
            Assert.IsTrue(resdata.Length > 600000);
            var resman = new System.Resources.ResourceManager("DictionaryLib.Properties.Resources", typeof(DictionaryLib.DictionaryLib).Assembly);
            var dict1 = (byte[])resman.GetObject("dict1"); //large
            Assert.IsTrue(dict1.Length > 500000);
            var dict2 = (byte[])resman.GetObject("dict2");
            Assert.IsTrue(dict1.Length > 180000);
            var dictSmall = new DictionaryLib.DictionaryLib(DictionaryType.Small);
            var dictLarge = new DictionaryLib.DictionaryLib(DictionaryType.Large);
        }

        [TestMethod]
        [Ignore]
        public void TestOldDictWrapper()
        {
            //            var lstWords = new List<string>();
            using var dictWrapper = new OldDictWrapper(2);
            //                lstWords.AddRange(dictWrapper.GetWords("*"));
        }
        [TestMethod]
        public void TestSeekWord()
        {
            var dict = new DictionaryLib.DictionaryLib(DictionaryType.Small);

            var testwrd = "qqq";
            var wrd = dict.SeekWord(testwrd, out var _);
            Console.WriteLine($"Seek {testwrd} {wrd} GetNextWordCount= {dict._GetNextWordCount}");
            Assert.AreEqual(1, dict._GetNextWordCount);

            foreach (var str in new[] { "zys", "me", "aband", "", "z", "mel", "asdf" })
            {
                var res = dict.SeekWord(str);
                LogMessage($"SeekWord {str}, {res}  GetNextWordCount= {dict._GetNextWordCount}");
            }
            Assert.AreEqual(dict.SeekWord(""), "a");
            Assert.AreEqual(dict.SeekWord("me"), "me");
            Assert.AreEqual(dict.SeekWord("mel"), "melancholy");
            Assert.AreEqual(dict.SeekWord("aband"), "abandon");
            Assert.AreEqual(dict.SeekWord("asdf"), "asexual");
            Assert.AreEqual(dict.SeekWord("it"), "it");

            var partial = dict.SeekWord("test", out var compResult);
            Assert.AreEqual(0, compResult);
            Assert.AreEqual("test", partial);

            partial = dict.SeekWord("testdddddd", out compResult);
            Assert.IsTrue(compResult > 0);
            Assert.AreEqual("tested", partial);

            partial = dict.SeekWord("contemptuousl", out compResult);
            Assert.IsTrue(compResult > 0);
            Assert.AreEqual("contend", partial);

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

            var result = dict.FindMatchRegEx("mel.*").ToList();
            Assert.AreEqual(66, result.Count);
            Assert.IsTrue(result.Contains("watermelon"));

            result = dict.FindMatchRegEx("zz.*").ToList(); // all words with "zz"
            Assert.AreEqual(94, result.Count);
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
        public void TestIterateDict()
        {
            var dict = new DictionaryLib.DictionaryLib(DictionaryType.Small, new Random(1));
            var rs = dict.SeekWord("");
            Trace.WriteLine($"Seek empty string == {rs}");
            var cnt = 0;
            while (!string.IsNullOrEmpty(dict.GetNextWord()))
            {
                cnt++;
            }
            Trace.WriteLine($"{cnt} entries found");
            Assert.AreEqual(38745, cnt);
        }

        [TestMethod]
        public void TestPermutationIsWord()
        {
            var dict = new DictionaryLib.DictionaryLib(DictionaryType.Small, new Random(1));
            var rs = dict.SeekWord("");
            Assert.AreEqual("a", rs);
            var iss1 = dict.IsWord("tes");
            var iss2 = dict.IsWord("zuiaelqde");
            var word = "equalized";
            var lstPerms = new List<string>();
            var nPerms = 0;
            DictionaryLib.DictionaryLib.PermuteString(word, LeftToRight: true, (str) =>
            {
                if (dict.IsWord(str))
                {
                    lstPerms.Add(str);
                    Trace.WriteLine($"{str}");
                }
                nPerms++;
                return true;
            });
            Trace.WriteLine($"# permutations of {word} = {nPerms}. {lstPerms.Count} are words");
        }

        [TestMethod]
        [Ignore]
        //        [ExpectedException(typeof(InvalidOperationException), AllowDerivedTypes = false)]
        public void TestDoAnagramOld()
        {
            using var dict = new MakeDictionary.OldDictWrapper(1);
            var lstAnagrams = new List<string>();

            var x = dict.FindAnagrams("discounterzz");
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
            Assert.AreEqual(20345, dict._nRecursionCnt);
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
            Assert.IsTrue(lstAnagrams.Contains("cot"));
            Assert.IsTrue(lstAnagrams.Contains("count"));
            Assert.IsTrue(lstAnagrams.Contains("cut"));
            Assert.IsTrue(lstAnagrams.Contains("not"));
            Assert.IsTrue(lstAnagrams.Contains("nut"));
            Assert.IsTrue(lstAnagrams.Contains("out"));
            Assert.IsTrue(lstAnagrams.Contains("ton"));
            Assert.IsTrue(lstAnagrams.Contains("unto"));
            Assert.AreEqual(9, lstAnagrams.Count);
            Assert.AreEqual(124, dict._nRecursionCnt);
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
            Assert.IsTrue(lstAnagrams.Contains("sonic"));
            Assert.IsTrue(lstAnagrams.Contains("scout"));
            Assert.IsTrue(lstAnagrams.Contains("icons"));
            Assert.AreEqual(21, lstAnagrams.Count);
            Assert.AreEqual(1640, dict._nRecursionCnt);
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
            Assert.AreEqual(1555, lstAnagrams.Count);
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
            Assert.IsTrue(lstAnagrams.Contains("sonic"));
            Assert.IsTrue(lstAnagrams.Contains("scout"));
            Assert.IsTrue(lstAnagrams.Contains("icons"));
            Assert.AreEqual(120, lstAnagrams.Count);
        }

        public class foo
        {
            private byte[] _wordBytes = new byte[DictionaryLib.DictionaryLib.MaxWordLen];
            int _currentLength;
            string TestWord = "foobarfoobarfoobar";
            string OtherTestWord = "foobarfoobarfoobar";
            public byte this[int key] { get { return this._wordBytes[key]; } set { this._wordBytes[key] = value; } }
            public foo()
            {
                SetWord(TestWord);
            }
            public void SetWord(string word)
            {
                _currentLength = word.Length;
                for (int ndx = 0; ndx < word.Length; ndx++)
                {
                    _wordBytes[ndx] = (byte)word[ndx];
                }
            }

            [BenchmarkDotNet.Attributes.Benchmark]
            public void DoOne()
            {
                var wother = new foo();
                wother.SetWord(OtherTestWord);
                CompareTo1(wother);
            }
            [BenchmarkDotNet.Attributes.Benchmark]
            public void DoTwo()
            {
                var wother = new foo();
                wother.SetWord(OtherTestWord);
                CompareTo2(wother);
            }
            public int CompareTo1(object obj)
            {
                int retval = 0;
                if (obj is foo other)
                {
                    for (int i = 0; i < Math.Min(this._currentLength, other._currentLength); i++)
                    {
                        /*
                        var thisone = this._wordBytes[i];
                        var thatone = other._wordBytes[i];
                        if (thisone != thatone)
                        {
                            retval = thisone.CompareTo(thatone);
                            if (retval != 0)
                            {
                                break;
                            }
                        }
                        /*/
                        if (this[i] != other[i])
                        {
                            retval = this[i].CompareTo(other[i]);
                            if (retval != 0)
                            {
                                break;
                            }
                        }
                        //*/
                    }
                    if (retval == 0)
                    {
                        retval = this._currentLength.CompareTo(other._currentLength);
                    }
                }
                else
                {
                    throw new InvalidOperationException();
                }
                return retval;
            }
            public int CompareTo2(object obj)
            {
                int retval = 0;
                if (obj is foo other)
                {
                    for (int i = 0; i < Math.Min(this._currentLength, other._currentLength); i++)
                    {
                        //*
                        var thisone = this._wordBytes[i];
                        var thatone = other._wordBytes[i];
                        if (thisone != thatone)
                        {
                            retval = thisone.CompareTo(thatone);
                            if (retval != 0)
                            {
                                break;
                            }
                        }
                        /*/
                        if (this[i] != other[i])
                        {
                            retval = this[i].CompareTo(other[i]);
                            if (retval != 0)
                            {
                                break;
                            }
                        }
                        //*/
                    }
                    if (retval == 0)
                    {
                        retval = this._currentLength.CompareTo(other._currentLength);
                    }
                }
                else
                {
                    throw new InvalidOperationException();
                }
                return retval;
            }
        }
        [TestMethod]
        public void TestBench()
        {
            var dict = new DictionaryLib.DictionaryLib(DictionaryType.Small, new Random(1));
            var config = ManualConfig.Create(BenchmarkDotNet.Configs.DefaultConfig.Instance);//.WithOptions(ConfigOptions.DisableOptimizationsValidator);
            BenchmarkRunner.Run<foo>(config);
        }

        [TestMethod]
        public void TestDoGenerateSubWords()
        {
            var dict = new DictionaryLib.DictionaryLib(DictionaryType.Small, new Random(1));
            var setAllWords = new HashSet<string>();
            dict.SeekWord("");
            while (true)
            {
                var word = dict.GetNextWord();
                if (string.IsNullOrEmpty(word))
                {
                    break;
                }
                setAllWords.Add(word);
            }
            var x = setAllWords.Contains("i");
            var InitWord = "discounter"; // not in small dict
            for (int i = 0; i < 1; i++)
            {
                var lst = dict.GenerateSubWords(InitWord, out var numLookups);
                LogMessage($"{InitWord} Found subwords ={lst.Count} #Lookups = {numLookups:n0}");
                foreach (var word in lst)
                {
                    LogMessage($"{word}");
                }
                Assert.AreEqual(484, lst.Count);
            }



        }

        [TestMethod]
        [Ignore]
        public void TestPerfRandWord()
        {
            using var oldDict = new OldDictWrapper(1);
            var newdict = new DictionaryLib.DictionaryLib(DictionaryType.Large, new Random(1));
            var sw = new Stopwatch();
            sw.Start();
            var nCnt = 10000;
            for (int i = 0; i < nCnt; i++)
            {
                oldDict.RandWord(0);
            }
            var olddictTime = sw.Elapsed.TotalSeconds;
            LogMessage($"Olddict {sw.Elapsed.TotalSeconds}");
            sw.Restart();
            for (int i = 0; i < nCnt; i++)
            {
                newdict.RandomWord();
            }
            var newdictTime = sw.Elapsed.TotalSeconds;
            LogMessage($"Newdict {newdictTime}");
            Assert.Fail($"This test is supposed to fail to show perf results: OldDict {olddictTime:n1} newdict {sw.Elapsed.TotalSeconds:n1}  Ratio {newdictTime / olddictTime:n1}");
        }

        [TestMethod]
        [Ignore]
        public void TestPerfIsWord()
        {
            using var oldDict = new OldDictWrapper(1);
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
                if (i == 0)
                {
                    LogMessage($"{word}  _GetNextWordCount {newdict._GetNextWordCount}");
                }
                Assert.IsTrue(r);
                Assert.AreEqual(813, newdict._GetNextWordCount);
            }
            var newdictTime = sw.Elapsed.TotalSeconds;
            LogMessage($"Newdict {newdictTime}");
            Assert.Fail($"This test is supposed to fail to show perf results: OldDict {olddictTime:n1} newdict {sw.Elapsed.TotalSeconds:n1}  Ratio {newdictTime / olddictTime:n1}");
        }

        [TestMethod]
        [Ignore]
        public void TestPerfIsNotWord()
        {
            using var oldDict = new OldDictWrapper(1);
            var newdict = new DictionaryLib.DictionaryLib(DictionaryType.Large, new Random(1));
            var sw = new Stopwatch();
            sw.Start();
            var nCnt = 10000;
            var word = "qqq";
            for (int i = 0; i < nCnt; i++)
            {
                var r = oldDict.IsWord(word);
                Assert.IsFalse(r);
            }
            var olddictTime = sw.Elapsed.TotalSeconds;
            LogMessage($"Olddict {sw.Elapsed.TotalSeconds}");
            sw.Restart();
            for (int i = 0; i < nCnt; i++)
            {
                var r = newdict.IsWord(word);
                if (i == 0)
                {
                    LogMessage($"{word}  _GetNextWordCount {newdict._GetNextWordCount}");
                }
                Assert.IsFalse(r);
                Assert.AreEqual(1, newdict._GetNextWordCount);
            }
            var newdictTime = sw.Elapsed.TotalSeconds;
            LogMessage($"Newdict {newdictTime}");
            Assert.Fail($"This test is supposed to fail to show perf results: OldDict {olddictTime:n1} newdict {sw.Elapsed.TotalSeconds:n1}  Ratio {newdictTime / olddictTime:n1}");
        }

        [TestMethod]
        [Ignore]
        public void TestPerfForTrace()
        {
            //            var oldDict = new OldDictWrapper(1);
            var newdict = new DictionaryLib.DictionaryLib(DictionaryType.Large, new Random(1));
            var sw = new Stopwatch();
            sw.Start();
            var nCnt = 5000000;
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
                newdict.IsWord(x);
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
        public void TestGetNextWord()
        {
            var dict = new DictionaryLib.DictionaryLib(DictionaryType.Small);
            // note: for small dict: from "hysterics" to "ice" no "I"

            var word = dict.GetNextWord();
            Assert.AreEqual("a", word);
            dict.SeekWord("hysterically");
            var set = new List<string>();
            while (string.CompareOrdinal( word,"iced") < 0)
            {
                word = dict.GetNextWord();
                set.Add(word);
                LogMessage($"word={word}");
            }

        }

        [TestMethod]
        public void TestDictIsWord()
        {
            var lstOldWords = GetOldDictWords(2);
            var dict = new DictionaryLib.DictionaryLib(DictionaryType.Small);
            var dictLarge = new DictionaryLib.DictionaryLib(DictionaryType.Large);
            foreach (var word in new[]
            {
                "fliest",
                "sorta",
                "oat",
                "center",
                "mister",
            })
            {
                LogMessage($"{word,-10} Small: {dict.IsWord(word)}  Large:  {dictLarge.IsWord(word)}");
            }

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
            Assert.IsFalse(dict.IsWord("zzsil"));


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
        [Ignore]
        public void TestMakedDict()
        {
            LogMessage($"{TestContext.TestName}  {DateTime.Now:MM/dd/yy hh:mm:ss}");
            //var lstSmall = GetOldDictWords((uint)DictionaryType.Small);

            var lstSmall = TestScowl.GetScowlWords(
                desiredLevel: 35,
                desiredFiles: new[] { "american", "english" },
                undesiredFiles: new[] { "proper", "upper", "variant", "contractions", "abbrev" }
                ).ToList();


            var lstlarge = GetOldDictWords((uint)DictionaryType.Large);
            var hashLarge = new HashSet<string>();
            foreach (var wrd in lstlarge)
            {
                hashLarge.Add(wrd);
            }
            lstSmall.Remove("fliest");
            hashLarge.Remove("fliest");
            lstSmall.Remove("sorta");
            lstSmall.Remove("uteri");
            lstSmall.Remove("maria");
            lstSmall.Remove("non");
            lstSmall.Remove("gonna");
            lstSmall.Remove("genii");
            lstSmall.Remove("roes");
            lstSmall.Remove("mes");
            lstSmall.Remove("dos");
            lstSmall.Add("oat");
            lstSmall.Add("center");
            lstSmall.Add("mister");

            foreach (var word in new[]
            {
                "fliest",
                "sorta",
                "oat",
                "center",
                "mister",
            })
            {
                LogMessage($"{word,-10} Small: {lstSmall.Contains(word)}  Large:  {hashLarge.Contains(word)}");
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
                "surdity",
                "misinforman",
                "consessione",
                "consessiones",
                "nonparticipat",
                "seventeenthes",
                "qtly", // QTLY 44, Mendaciously 43  ysoc luia tqsd lmen
            })
            {
                hashLarge.Remove(str);
            }
            //todo: remove words like "jurisdictione","nonparticipat"

            // when changing contents of dictionary, this test will fail until you update the resources, 
            // XCOPY /dy C:\Users\calvinh\Source\Repos\Wordament\WordamentTests\bin\Debug\*.bin C:\Users\calvinh\Source\Repos\Wordament\DictionaryLib\Resources
            // Then rebuild all

            for (uint dictNum = 2; dictNum >= 1; dictNum--)
            {
                List<string> lstWords = null;
                if ((DictionaryType)dictNum == DictionaryType.Small)
                {
                    lstWords = lstSmall.OrderBy(w => w).ToList();
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
                for (int i = 0; i < lstWords.Count; i++)
                {
                    Assert.AreEqual(lstWords[i], newlstWord[i], $"dict {dictNum}");
                }
                Assert.AreEqual(newlstWord.Count(), lstWords.Count(), $"dict num {dictNum} ");
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
            StringBuilder sb = new();
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


        [TestMethod]
        public void TestGetTimeAsString()
        {
            foreach (var time in new[] { 0, 59, 60, 61, 119, 120, 121 })
            {
                var str = WordamentWindow.GetTimeAsString(nSecs: time);
                Console.WriteLine($"for time {time} str={str}");

            }
        }

        [TestMethod]
        [Ignore]
        public void TestCryptogram()
        {
            var dict = new DictionaryLib.DictionaryLib(DictionaryType.Small);
            var str = "JGLQIN XR QYL DBYXLPLULTQ GE QYL RNTQYLRXR GE YNDBXTQYR DTA CXRBHXQR.  BDIF RDTACHIZ";
            var result = dict.CryptoGram(str);
            Assert.IsTrue(result == string.Empty);
        }

        [TestMethod]
        public void TestPermutation()
        {
            var txtToUse = "abcdefghi";
            int nTimes = 0;
            bool ActGotResult(string res)
            {
                LogMessage($" {res}");
                if (++nTimes > 362880) // n! = permute 1st n letters of a long string. 40320 = 8!, 362880=9!= < .3 secs
                {
                    return false;
                }
                return true;
            }
            nTimes = 0;
            LogMessage($"RightToLeft:");
            DictionaryLib.DictionaryLib.PermuteString(txtToUse, LeftToRight: false, act: ActGotResult);
            nTimes = 0;
            LogMessage($"LeftToRight:");
            DictionaryLib.DictionaryLib.PermuteString(txtToUse, LeftToRight: true, act: ActGotResult);
        }

        [TestMethod]
        public void TestCryptFindQMarkMatch()
        {
            var dict = new DictionaryLib.DictionaryLib(DictionaryType.Small);
            var dictTestData = new Dictionary<string, Tuple<int, int>>() // word to cntNumSearches, cntresults, 
            {
                ["__"] = new Tuple<int, int>(38746, 42),
                ["____i_i__"] = new Tuple<int, int>(38746, 211),
                ["c_n___ion"] = new Tuple<int, int>(3801, 3), //confusion, contagion
                ["_ondition"] = new Tuple<int, int>(38746, 1),
                ["c_ndition"] = new Tuple<int, int>(3801, 1),
                ["conditio_"] = new Tuple<int, int>(683, 1),
                ["con______"] = new Tuple<int, int>(683, 111),
                ["condition"] = new Tuple<int, int>(683, 1),
            };
            foreach (var kvp in dictTestData)
            {
                var mwordQMark = new MyWord(kvp.Key);
                var lstResults = new List<string>();
                dict.FindQMarkMatches(mwordQMark, (m) =>
                {
                    lstResults.Add(m.GetWord());
                    return true;
                });
                //                LogMessage($"FindQMarkMatch '{mwordQMark}'  _GetNextWordCount= {dict._GetNextWordCount} #Res={lstResults.Count}");
                foreach (var res in lstResults)
                {
                    LogMessage($" {res}");
                }
                LogMessage($"{kvp.Key,-20}  {kvp.Value.Item1} _GetNextWordCount={dict._GetNextWordCount}         #Res= {kvp.Value.Item2} #Res={lstResults.Count}  ");
                Assert.AreEqual(kvp.Value.Item1, dict._GetNextWordCount, kvp.Key);

                Assert.AreEqual(kvp.Value.Item2, lstResults.Count, kvp.Key);
            }
            //foreach (var strQMark in new[] {
            //    "c_ndition",
            //    "con_____",
            //    "condi_ion",
            //})
            //{
            //    var mwordQMark = new MyWord(strQMark);
            //    var lstResults = new List<string>();
            //    dict.FindQMarkMatches(mwordQMark, (m) =>
            //     {
            //         lstResults.Add(m.GetWord());
            //         return true;
            //     });
            //    LogMessage($"FindQMarkMatch {mwordQMark}  _GetNextWordCount= {dict._GetNextWordCount} #Res={lstResults.Count}");
            //    foreach (var res in lstResults)
            //    {
            //        LogMessage($" {res}");
            //    }
            //}
        }
    }
}
