using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using DictionaryLib;
using Iced.Intel;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace WordamentTests
{
    [TestClass]
    public class TestTrie : TestBase
    {
        [TestMethod]
        public void TestWordTrie()
        {
            var dictionarySmall = new DictionaryLib.DictionaryLib(DictionaryLib.DictionaryType.Small);
            var lstAllWords = dictionarySmall.GetAllWords();

            WordTrie.AddWords(lstAllWords);


            //var testword = "testp";
            //var iss = dictionarySmall.SeekWord(testword, out var compResult);
            //var x = WordTrie.IsWord(testword, out var nodePath);
            //var str = WordTrie.GetStringFromPath(nodePath);
            TestContext.WriteLine($"{dictionarySmall}  WordNode Count = {WordTrie.NodeCnt}");
        }
        [TestMethod]
        public void TestGenSubWordPerf()
        {
            var bench = new BenchGenSubWords()
            {
                InitialWord = "discounter"
            };
            for (int i = 0; i < 1; i++)
            {
                var lst = bench.DoWordRadix();
                Trace.WriteLine($"{i} {lst.Count} words");
            }
            //var dictionarySmall = new DictionaryLib.DictionaryLib(DictionaryLib.DictionaryType.Small);
            //var lstAllWords = dictionarySmall.GetAllWords();
            //var wordRadixTree = new WordRadixTree(lstAllWords);
            //var testWord = new MyWord();
            //for (int i = 0; i < 1; i++)
            //{
            //    var hashSetSubWords = new SortedSet<string>();
            //    DictionaryLib.DictionaryLib.PermuteString("discounter", LeftToRight: true, act: null, actMyWord: (str) =>
            //    {
            //        for (int i = 3; i <= str.WordLength; i++)
            //        {
            //            testWord.SetLength(i);
            //            for (int j = 0; j < i; j++)
            //            {
            //                testWord[j] = str[j];
            //            }
            //            var partial = wordRadixTree.SeekWord(testWord, out var compResult);
            //            if (partial.WordLength > 0 && compResult == 0)
            //            {
            //                hashSetSubWords.Add(testWord.GetWord());
            //            }
            //            else
            //            {
            //                if (!partial.StartsWith(testWord))
            //                {
            //                    break;
            //                }
            //            }
            //        }
            //        return true; // continue 
            //    });
            //    Trace.WriteLine($"{i} {hashSetSubWords.Count} words");
            //}
        }

        [TestMethod]
        public void TestWordRadix()
        {
            var dictionarySmall = new DictionaryLib.DictionaryLib(DictionaryLib.DictionaryType.Small);
            var lstAllWords = dictionarySmall.GetAllWords();
            var wordRadixTree = new WordRadixTree(lstAllWords);
            Trace.WriteLine($"Adding {lstAllWords.Count} words");
            // now verify
            var nWords = 0;
            var maxDepth = 0;
            wordRadixTree.WalkTreeWords((str, nDepth) =>
            {
                nWords++;
                maxDepth = Math.Max(nDepth, maxDepth);
                return true;//continue
            });
            Trace.WriteLine($"Tree has #words={nWords}  MaxDepth= {maxDepth} # nodes = {wordRadixTree.TotalNodes}");

            lstAllWords.ForEach(w => Assert.IsTrue(wordRadixTree.IsWord(w), $"{w} not found"));


            wordRadixTree.VerifyTree(lstAllWords, NumWordsExpected: lstAllWords.Count);

            var maxNodeLength = 0;
            var maxnodelengstr = string.Empty;
            wordRadixTree.WalkTreeNodes((node, depth) =>
            {
                if (node.NodeString.WordLength > maxNodeLength)
                {
                    maxNodeLength = node.NodeString.WordLength;
                    maxnodelengstr = node.GetWord();
                }
                //                Trace.WriteLine($"{new string(' ', depth)} {node.NodeString} {(node.Children == null ? "0" : node.Children.Count)}");
                return true;
            });
            Trace.WriteLine($"Maximum node length = {maxNodeLength}  {maxnodelengstr}");


            // verify all nodes reachable via GetNextNode
            var curNode = wordRadixTree.RootNode;
            var wrdCnt = 0;
            while (true)
            {
                var node = curNode.GetNextNode(OnlyWordNodes: false);
                if (node == null)
                {
                    break;
                }
                if (node.IsNodeAWord)
                {
                    var str = node.GetWord();
                    if (lstAllWords[wrdCnt] != str)
                    {
                        throw new Exception($"Word not matched {wrdCnt} {str}  {lstAllWords[wrdCnt]}");
                    }
                    wrdCnt++;
                }
                curNode = node;
            }
            var x = wordRadixTree.SeekWord("cids", out var compResult);
            var lstBadWords = new List<string>() { // 3 different kinds of find failures
                "beckoningly",
                "testp",
                "foobar",
            };
            lstBadWords.ForEach(w =>
            {
                var resSeek = wordRadixTree.SeekWord(w, out var compResult);

            });
            lstBadWords.ForEach(w => Assert.IsFalse(wordRadixTree.IsWord(w), $"{w} found ??"));


            Assert.AreEqual(lstAllWords.Count, wordRadixTree.TotalWords);
        }
        [TestMethod]
        [Ignore]
        public void TestWordNodesBench()
        {
            var config = BenchmarkDotNet.Configs.ManualConfig.Create(BenchmarkDotNet.Configs.DefaultConfig.Instance);//.WithOptions(BenchmarkDotNet.Configs.ConfigOptions.DisableOptimizationsValidator);
            BenchmarkRunner.Run<BenchWordNode>(config);
            /*
    
|           Method | numIter |          Mean |         Error |        StdDev |        Median |       Gen0 |      Gen1 |     Gen2 |    Allocated |
|----------------- |-------- |--------------:|--------------:|--------------:|--------------:|-----------:|----------:|---------:|-------------:|
| TestSeekWordNode |      10 |  51,751.95 us |  1,410.129 us |  4,113.412 us |  51,420.40 us |  2875.0000 | 2000.0000 | 875.0000 |  11194.64 KB |
|     TestSeekDict |      10 |      59.99 us |      2.575 us |      6.828 us |      58.47 us |     4.3335 |         - |        - |     17.92 KB |
| TestSeekWordNode |    1000 |  53,721.10 us |  3,198.644 us |  9,279.847 us |  50,717.96 us |  2750.0000 | 2000.0000 | 750.0000 |  11195.13 KB |
|     TestSeekDict |    1000 |   5,670.67 us |    159.839 us |    458.609 us |   5,634.58 us |   429.6875 |         - |        - |    1791.7 KB |
| TestSeekWordNode |  100000 |  64,721.48 us |  3,683.243 us | 10,802.314 us |  61,162.74 us |  2777.7778 | 2000.0000 | 777.7778 |   11194.8 KB |
|     TestSeekDict |  100000 | 565,850.30 us | 17,978.040 us | 50,115.590 us | 561,969.60 us | 43000.0000 |         - |        - | 179169.59 KB |

encoding.ascii.getbytes:
|           Method | numIter |          Mean |         Error |        StdDev |       Gen0 |      Gen1 |     Gen2 |   Allocated |
|----------------- |-------- |--------------:|--------------:|--------------:|-----------:|----------:|---------:|------------:|
| TestSeekWordNode |      10 |  54,459.00 us |  2,613.950 us |  7,707.290 us |  2777.7778 | 2000.0000 | 777.7778 |  11194.8 KB |
|     TestSeekDict |      10 |      55.47 us |      1.921 us |      5.512 us |     2.0142 |         - |        - |     8.25 KB |
| TestSeekWordNode |    1000 |  57,358.68 us |  2,835.868 us |  8,317.109 us |  2800.0000 | 2000.0000 | 800.0000 | 11194.14 KB |
|     TestSeekDict |    1000 |   5,447.16 us |    170.356 us |    488.784 us |   195.3125 |         - |        - |   825.45 KB |
| TestSeekWordNode |  100000 |  64,764.08 us |  2,850.571 us |  8,270.024 us |  2750.0000 | 2000.0000 | 750.0000 | 11194.13 KB |
|     TestSeekDict |  100000 | 543,545.28 us | 18,671.240 us | 54,168.652 us | 20000.0000 |         - |        - | 82544.77 KB |
remove tolower:
|           Method | numIter |          Mean |         Error |        StdDev |        Median |       Gen0 |      Gen1 |     Gen2 |   Allocated |
|----------------- |-------- |--------------:|--------------:|--------------:|--------------:|-----------:|----------:|---------:|------------:|
| TestSeekWordNode |      10 |  55,024.98 us |  2,666.358 us |  7,861.817 us |  53,740.09 us |  2750.0000 | 2000.0000 | 750.0000 | 11194.13 KB |
|     TestSeekDict |      10 |      42.13 us |      1.884 us |      5.375 us |      40.62 us |     1.2207 |         - |        - |     5.05 KB |
| TestSeekWordNode |    1000 |  54,887.25 us |  2,644.305 us |  7,755.289 us |  53,649.46 us |  2714.2857 | 2000.0000 | 714.2857 | 11193.84 KB |
|     TestSeekDict |    1000 |   4,314.71 us |    158.339 us |    456.843 us |   4,217.86 us |   121.0938 |         - |        - |   504.66 KB |
| TestSeekWordNode |  100000 |  68,494.99 us |  3,374.120 us |  9,735.108 us |  67,718.28 us |  2750.0000 | 2000.0000 | 750.0000 | 11194.13 KB |
|     TestSeekDict |  100000 | 408,598.25 us | 12,898.466 us | 37,828.968 us | 411,510.90 us | 12000.0000 |         - |        - | 50472.61 KB |

            
             */
        }
        [TestMethod]
//        [Ignore]
        public void TestBenchGenSubWords()
        {
            //*
            var config = ManualConfig.Create(BenchmarkDotNet.Configs.DefaultConfig.Instance);//.WithOptions(ConfigOptions.DisableOptimizationsValidator);
            BenchmarkRunner.Run<BenchGenSubWords>(config);
            /*/
            var x = new BenchGenSubWords()
            {
                InitialWord = "testing"
            };
            //Word Testing == 45 subwords, Discount = 75
            x.DoWordRadix();
            x.DoHashSet();
            x.DoWithNone();
            //*/
            /*
calvinh5:
|      Method | InitialWord |         Mean |      Error |     StdDev |        Gen0 |        Gen1 |     Allocated |
|------------ |------------ |-------------:|-----------:|-----------:|------------:|------------:|--------------:|
|  DoWithNone |  discounter | 2,693.806 ms |  4.4708 ms |  3.9633 ms | 134000.0000 | 134000.0000 |   687285.7 KB |
|   DoHashSet |  discounter | 7,388.738 ms | 16.9374 ms | 15.8432 ms |  46000.0000 |  46000.0000 |  239401.89 KB |
| DoWordRadix |  discounter | 9,305.156 ms | 11.4991 ms |  9.6023 ms | 262000.0000 | 262000.0000 | 1346081.56 KB |
|  DoWithNone |     testing |     3.587 ms |  0.0217 ms |  0.0203 ms |    175.7813 |    175.7813 |     914.85 KB |
|   DoHashSet |     testing |    10.029 ms |  0.0586 ms |  0.0519 ms |     46.8750 |     46.8750 |     292.63 KB |
| DoWordRadix |     testing |    12.378 ms |  0.0425 ms |  0.0398 ms |    328.1250 |    328.1250 |    1721.76 KB |

With MyWord:
|      Method | InitialWord |         Mean |      Error |     StdDev |        Gen0 |        Gen1 |     Allocated |
|------------ |------------ |-------------:|-----------:|-----------:|------------:|------------:|--------------:|
|  DoWithNone |  discounter | 2,666.301 ms |  6.6024 ms |  5.8529 ms | 134000.0000 |           - |   687285.7 KB |
|   DoHashSet |  discounter | 7,306.645 ms | 17.4893 ms | 15.5038 ms |  46000.0000 |           - |  239401.89 KB |
| DoWordRadix |  discounter | 4,746.020 ms | 11.1651 ms |  9.8976 ms | 742000.0000 | 742000.0000 | 3799920.26 KB |
|  DoWithNone |     testing |     3.598 ms |  0.0180 ms |  0.0168 ms |    175.7813 |           - |     914.85 KB |
|   DoHashSet |     testing |    10.025 ms |  0.0344 ms |  0.0287 ms |     46.8750 |           - |     292.63 KB |
| DoWordRadix |     testing |     6.055 ms |  0.0088 ms |  0.0073 ms |    968.7500 |      7.8125 |    4987.86 KB |

Fewer allocs:
|      Method | InitialWord |         Mean |      Error |     StdDev |        Gen0 |     Allocated |
|------------ |------------ |-------------:|-----------:|-----------:|------------:|--------------:|
|  DoWithNone |  discounter | 2,709.671 ms | 11.0827 ms | 10.3668 ms | 134000.0000 |   687285.7 KB |
|   DoHashSet |  discounter | 7,437.304 ms | 22.8454 ms | 20.2518 ms |  46000.0000 |  239401.89 KB |
| DoWordRadix |  discounter | 4,442.320 ms | 15.0772 ms | 14.1032 ms | 340000.0000 | 1740924.73 KB |
|  DoWithNone |     testing |     3.601 ms |  0.0152 ms |  0.0134 ms |    175.7813 |     914.85 KB |
|   DoHashSet |     testing |    10.055 ms |  0.0509 ms |  0.0451 ms |     46.8750 |     292.63 KB |
| DoWordRadix |     testing |     5.556 ms |  0.0385 ms |  0.0341 ms |    421.8750 |     2187.2 KB |
Permute:MyWord
|      Method | InitialWord |         Mean |      Error |     StdDev |        Gen0 |     Allocated |
|------------ |------------ |-------------:|-----------:|-----------:|------------:|--------------:|
|  DoWithNone |  discounter | 2,697.122 ms | 13.0143 ms | 12.1736 ms | 134000.0000 |   687285.7 KB |
|   DoHashSet |  discounter | 7,325.215 ms | 18.2224 ms | 15.2166 ms |  46000.0000 |  239401.89 KB |
| DoWordRadix |  discounter | 3,428.236 ms | 25.5278 ms | 22.6297 ms | 260000.0000 | 1335771.01 KB |
|  DoWithNone |     testing |     3.601 ms |  0.0221 ms |  0.0207 ms |    175.7813 |     914.85 KB |
|   DoHashSet |     testing |    10.011 ms |  0.0391 ms |  0.0347 ms |     46.8750 |     292.63 KB |
| DoWordRadix |     testing |     4.459 ms |  0.0254 ms |  0.0237 ms |    335.9375 |    1746.39 KB |
remove List<nodes> in IsWord
|      Method | InitialWord |         Mean |      Error |     StdDev |        Gen0 |     Allocated |
|------------ |------------ |-------------:|-----------:|-----------:|------------:|--------------:|
|  DoWithNone |  discounter | 2,742.581 ms | 10.6433 ms |  9.9557 ms | 134000.0000 |   687285.7 KB |
|   DoHashSet |  discounter | 7,380.937 ms | 13.0620 ms | 11.5792 ms |  46000.0000 |  239401.89 KB |
| DoWordRadix |  discounter | 3,259.323 ms |  7.6864 ms |  6.4185 ms | 203000.0000 | 1041945.14 KB |
|  DoWithNone |     testing |     3.599 ms |  0.0172 ms |  0.0161 ms |    175.7813 |     914.85 KB |
|   DoHashSet |     testing |    10.009 ms |  0.0211 ms |  0.0176 ms |     46.8750 |     292.63 KB |
| DoWordRadix |     testing |     4.227 ms |  0.0321 ms |  0.0300 ms |    257.8125 |    1336.82 KB |

remove substr in Permute lambda
|      Method | InitialWord |         Mean |      Error |     StdDev |        Gen0 |        Gen1 |    Allocated |
|------------ |------------ |-------------:|-----------:|-----------:|------------:|------------:|-------------:|
|  DoWithNone |  discounter | 2,702.335 ms | 12.7247 ms | 11.2801 ms | 134000.0000 | 134000.0000 |  687285.7 KB |
|   DoHashSet |  discounter | 7,437.212 ms | 26.8032 ms | 22.3819 ms |  46000.0000 |           - | 239409.89 KB |
| DoWordRadix |  discounter | 2,311.770 ms | 22.4443 ms | 20.9944 ms |  71000.0000 |           - | 364003.37 KB |
|  DoWithNone |     testing |     3.587 ms |  0.0093 ms |  0.0078 ms |    175.7813 |           - |    914.85 KB |
|   DoHashSet |     testing |    10.026 ms |  0.0327 ms |  0.0290 ms |     46.8750 |           - |       293 KB |
| DoWordRadix |     testing |     2.915 ms |  0.0043 ms |  0.0040 ms |     85.9375 |     85.9375 |       441 KB |

DoWithNone: use MyWord
|      Method | InitialWord |         Mean |      Error |     StdDev |       Gen0 |    Gen1 |    Allocated |
|------------ |------------ |-------------:|-----------:|-----------:|-----------:|--------:|-------------:|
|  DoWithNone |  discounter | 1,921.353 ms |  6.9252 ms |  6.1390 ms | 62000.0000 |       - | 322202.94 KB |
|   DoHashSet |  discounter | 7,424.749 ms | 14.3898 ms | 13.4602 ms | 46000.0000 |       - | 239409.89 KB |
| DoWordRadix |  discounter | 2,318.888 ms |  7.7698 ms |  7.2679 ms | 71000.0000 |       - | 364003.37 KB |
|  DoWithNone |     testing |     2.590 ms |  0.0198 ms |  0.0176 ms |    85.9375 |       - |    450.57 KB |
|   DoHashSet |     testing |    10.130 ms |  0.0659 ms |  0.0551 ms |    46.8750 |       - |       293 KB |
| DoWordRadix |     testing |     2.945 ms |  0.0132 ms |  0.0117 ms |    85.9375 | 85.9375 |       441 KB |

RejectCache
|      Method | InitialWord |         Mean |      Error |     StdDev |       Gen0 |      Gen1 |    Allocated |
|------------ |------------ |-------------:|-----------:|-----------:|-----------:|----------:|-------------:|
|  DoWithNone |  discounter | 1,256.089 ms | 13.8997 ms | 12.3217 ms | 43000.0000 | 1000.0000 | 221772.26 KB |
|   DoHashSet |  discounter | 7,365.139 ms | 14.0870 ms | 12.4878 ms | 46000.0000 |         - | 239409.89 KB |
| DoWordRadix |  discounter | 2,297.427 ms |  7.1246 ms |  6.6644 ms | 71000.0000 | 1000.0000 | 364003.37 KB |
|  DoWithNone |     testing |     1.763 ms |  0.0043 ms |  0.0036 ms |    68.3594 |   68.3594 |    350.22 KB |
|   DoHashSet |     testing |    10.017 ms |  0.0538 ms |  0.0477 ms |    46.8750 |         - |       293 KB |
| DoWordRadix |     testing |     2.939 ms |  0.0147 ms |  0.0137 ms |    85.9375 |         - |       441 KB |


             */
        }
        [TestMethod]
        public void TestGenSubWordsHashSetRadix()
        {
            var x = new BenchGenSubWords()
            {
                InitialWord = "testing"
            };
            //Word Testing == 45 subwords, Discount = 75
            var lstWordRadix = x.DoWordRadix();
            TestContext.WriteLine($"WordRadix {lstWordRadix.Count}");
            var lstHashSet = x.DoHashSet();
            TestContext.WriteLine($"HashSet {lstHashSet.Count}");
            var lstWithNone = x.DoWithNone();
            TestContext.WriteLine($"None {lstWithNone.Count}");
            foreach (var word in lstWithNone)
            {
                lstWordRadix.Remove(word);
            }
            foreach (var word in lstWordRadix)
            {
                TestContext.WriteLine($"Extra {word}");
            }
            // 45 for "testing", 484 for "discounter"
            Assert.AreEqual(0, lstWordRadix.Count);
            Assert.AreEqual(45, lstHashSet.Count);
            Assert.AreEqual(45, lstWithNone.Count);
        }
    }

    [MemoryDiagnoser]
    public class BenchGenSubWords
    {
        public enum GenSubWordType
        {
            Dict,
            //WordNode,
            HashSet,
            WordRadix
        }
        //        [ParamsAllValues]
        public GenSubWordType GenType { get; set; }

        [Params("discounter", "testing")]
        public string InitialWord { get; set; }
        public int MinLength = 3;
        public int MaxSubWords = int.MaxValue;
        private readonly DictionaryLib.DictionaryLib dict;
        private readonly List<string> lstAllWords;
        private readonly WordRadixTree wordRadixTree;

        public BenchGenSubWords()
        {
            dict = new DictionaryLib.DictionaryLib(DictionaryLib.DictionaryType.Small);
            lstAllWords = dict.GetAllWords();
            wordRadixTree = new WordRadixTree(lstAllWords);
        }
        [Benchmark]
        public List<string> DoWithNone()
        {
            var lst = dict.GenerateSubWords(InitialWord, out var numSearches);
            Console.WriteLine($"{InitialWord,12} None #SubWords= {lst.Count} #Searches={numSearches}");
            //var ndx = 0;
            //lst.ForEach(d => Console.WriteLine($"N {ndx++} {d}"));
            return lst;
        }
        [Benchmark]
        public List<string> DoHashSet()
        {
            var hashSetSubWords = new SortedSet<string>();
            var numSearches = 0;
            var rejectsCached = new HashSet<string>();
            DictionaryLib.DictionaryLib.PermuteString(InitialWord, LeftToRight: true, (str) =>
            {
                for (int i = MinLength; i <= str.Length; i++)
                {
                    var testWord = str.Substring(0, i);
                    if (rejectsCached.Contains(testWord))
                    {
                        break;
                    }
                    numSearches++;
                    var res = lstAllWords.BinarySearch(testWord);
                    if (res >= 0)
                    {
                        hashSetSubWords.Add(testWord);
                    }
                    else
                    {
                        var partial = lstAllWords[~res];
                        if (!partial.StartsWith(testWord))
                        {
                            rejectsCached.Add(testWord);
                            break;
                        }
                    }
                }
                return hashSetSubWords.Count < MaxSubWords; // continue ?
            });
            Console.WriteLine($"{InitialWord,12} Hash #SubWords= {hashSetSubWords.Count} #Searches={numSearches}");
            //var ndx = 0;
            //hashSetSubWords.ToList().ForEach(d => Console.WriteLine($"H {ndx++} {d}"));
            return hashSetSubWords.ToList();
        }

        [Benchmark]
        public List<string> DoWordRadix()
        {
            var hashSetSubWords = new SortedSet<string>();
            var testWord = new MyWord();
            var rejectsCached = new HashSet<MyWord>();
            DictionaryLib.DictionaryLib.PermuteString(InitialWord, LeftToRight: true, act: null, actMyWord: (str) =>
            {
                for (int i = MinLength; i <= str.WordLength; i++)
                {
                    testWord.SetLength(i);
                    for (int j = 0; j < i; j++)
                    {
                        testWord[j] = str[j];
                    }
                    if (rejectsCached.Contains(testWord))
                    {
                        break;
                    }
                    var partial = wordRadixTree.SeekWord(testWord, out var compResult);
                    if (partial.WordLength > 0 && compResult == 0)
                    {
                        hashSetSubWords.Add(testWord.GetWord());
                    }
                    else
                    {
                        if (!partial.StartsWith(testWord))
                        {
                            rejectsCached.Add(testWord);
                            break;
                        }
                    }
                }
                return hashSetSubWords.Count < MaxSubWords; // continue 
            });
            Console.WriteLine($"{InitialWord,12} WordRadix #SubWords= {hashSetSubWords.Count} ");
            return hashSetSubWords.ToList();
        }
    }

    [MemoryDiagnoser]
    public class BenchWordNode
    {
        [Params(10, 1000, 100000)]
        public int numIter { get; set; }

        System.Lazy<object> MakeWordNodes;

        string[] WordsToTest = new[] {
            "food",
            "things",
            "testing",
        };
        string[] NonWordsToTest = new[] {
            "foodr",
            "thingsr",
            "testingr",
        };
        private readonly DictionaryLib.DictionaryLib dict;
        private readonly HashSet<string> setAllWords;
        public BenchWordNode()
        {
            dict = new DictionaryLib.DictionaryLib(DictionaryLib.DictionaryType.Small);
            setAllWords = new HashSet<string>();
            while (true)
            {
                var word = dict.GetNextWord();
                if (string.IsNullOrEmpty(word))
                {
                    break;
                }
                setAllWords.Add(word);
            }
            MakeWordNodes = new Lazy<object>(() =>
            {
                return null;
            });
        }
        [Benchmark]
        public void TestSeekWordNode()
        {
            _ = MakeWordNodes.Value;
            WordTrie.Clear();
            if (WordTrie.NodeCnt < 2)
            {
                foreach (var word in setAllWords)
                {
                    WordTrie.AddWord(word);
                }
            }
            // find sweet spot
            for (int i = 0; i < numIter; i++)
            {
                foreach (var word in WordsToTest)
                {
                    var x = WordTrie.IsWord(word);
                    Assert.IsTrue(x, word);
                }
                foreach (var word in NonWordsToTest)
                {
                    var x = WordTrie.IsWord(word);
                    Assert.IsFalse(x, word);
                }
            }
        }
        [Benchmark]
        public void TestSeekDict()
        {
            for (int i = 0; i < numIter; i++)
            {
                foreach (var word in WordsToTest)
                {
                    var x = dict.IsWord(word);
                    Assert.IsTrue(x, word);
                }
                foreach (var word in NonWordsToTest)
                {
                    var x = dict.IsWord(word);
                    Assert.IsFalse(x, word);
                }
            }
        }
    }



    class WordTrie // a node in a graph: a single root node has 26 first letter words, each of which has child nodes. For 38751 words in small dict, this produces 86711 nodes,each with an array of 26 = 18M (vs 191K for small dict)
    {
        public static char RootNodeChar = '\0';
        public static WordTrie root = new WordTrie(RootNodeChar);
        public static int NodeCnt;
        public char ltr;
        public WordTrie[] wordNodes = new WordTrie[26];
        public WordTrie(char ltr)
        {
            NodeCnt++;
            this.ltr = ltr;
        }
        public static void Clear()
        {
            root.wordNodes = new WordTrie[26];
            NodeCnt = 1;
        }
        public static void AddWords(IEnumerable<string> words)
        {
            foreach (var word in words)
            {
                AddWord(word);
            }
        }
        public static void AddWord(string word) // lower case word passed in
        {
            var curNode = WordTrie.root;
            foreach (var chr in word)
            {
                var ndx = chr - 97;
                var nextNode = curNode.wordNodes[ndx];
                if (nextNode == null)
                {
                    nextNode = new WordTrie(chr);
                    curNode.wordNodes[ndx] = nextNode;
                }
                curNode = nextNode;
            }
        }
        public static bool IsWord(string word)
        {
            return IsWord(word, out var _);
        }
        public static string GetStringFromPath(List<WordTrie> wordNodes)
        {
            var str = string.Empty;
            wordNodes.ForEach(n => str += n.ltr);
            return str;
        }
        public static bool IsWord(string word, out List<WordTrie> path)
        {
            var isWord = true;
            var curNode = WordTrie.root;
            path = new List<WordTrie>();
            foreach (var chr in word)
            {
                var ndx = chr - 97;
                var nextNode = curNode.wordNodes[ndx];
                if (nextNode == null)
                {
                    isWord = false;
                    break;
                }
                path.Add(nextNode);
                curNode = nextNode;
            }
            return isWord;
        }
        public override string ToString() => $"{ltr}";
    }
}
