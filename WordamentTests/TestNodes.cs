using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WordamentTests
{
    [TestClass]
    public class TestNodes : TestBase
    {
        [TestMethod]
        public void TestWordNodes()
        {
            var dictionarySmall = new DictionaryLib.DictionaryLib(DictionaryLib.DictionaryType.Small);
            var bytes = System.Text.Encoding.ASCII.GetBytes("test");
            var wrd = Encoding.ASCII.GetString(bytes);
            var setAllWords = new HashSet<string>();
            while (true)
            {
                var word = dictionarySmall.GetNextWord();
                if (string.IsNullOrEmpty(word))
                {
                    break;
                }
                setAllWords.Add(word);

            }
            //WordNode.AddWord("test");
            //WordNode.AddWord("testing");
            //var xx = WordNode.IsWord("test");
            //var xy = WordNode.IsWord("testings");

            foreach (var word in setAllWords)
            {
                WordNode.AddWord(word);
            }
            TestContext.WriteLine($"{dictionarySmall}  WordNode Count = {WordNode.NodeCnt}");
        }
        [TestMethod]
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
            WordNode.Clear();
            if (WordNode.NodeCnt < 2)
            {
                foreach (var word in setAllWords)
                {
                    WordNode.AddWord(word);
                }
            }
            // find sweet spot
            for (int i = 0; i < numIter; i++)
            {
                foreach (var word in WordsToTest)
                {
                    var x = WordNode.IsWord(word);
                    Assert.IsTrue(x, word);
                }
                foreach (var word in NonWordsToTest)
                {
                    var x = WordNode.IsWord(word);
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

    class WordNode // a node in a graph: a single root node has 26 first letter words, each of which has child nodes. For 38751 words in small dict, this produces 86711 nodes,each with an array of 26 = 18M (vs 191K for small dict)
    {
        public static char RootNodeChar = '\0';
        public static WordNode root = new WordNode(RootNodeChar);
        public static int NodeCnt;
        public char ltr;
        public WordNode[] wordNodes = new WordNode[26];
        public WordNode(char ltr)
        {
            NodeCnt++;
            this.ltr = ltr;
        }
        public static void Clear()
        {
            root.wordNodes = new WordNode[26];
            NodeCnt = 1;
        }
        public static void AddWord(string word) // lower case word passed in
        {
            var curNode = WordNode.root;
            foreach (var chr in word)
            {
                var ndx = chr - 97;
                var nextNode = curNode.wordNodes[ndx];
                if (nextNode == null)
                {
                    nextNode = new WordNode(chr);
                    curNode.wordNodes[ndx] = nextNode;
                }
                curNode = nextNode;
            }
        }
        public static bool IsWord(string word)
        {
            var isWord = true;
            var curNode = WordNode.root;
            foreach (var chr in word)
            {
                var ndx = chr - 97;
                var nextNode = curNode.wordNodes[ndx];
                if (nextNode == null)
                {
                    isWord = false;
                    break;
                }
                curNode = nextNode;
            }
            return isWord;
        }
        public override string ToString() => $"{ltr}";
    }

}
