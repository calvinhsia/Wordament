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
        public void TestWordRadix()
        {
            var rand = new Random();
            var dictionarySmall = new DictionaryLib.DictionaryLib(DictionaryLib.DictionaryType.Small);
            var lstAllWords = dictionarySmall.GetAllWords();
            WordRadix.ClearAll();
            Trace.WriteLine($"Adding {lstAllWords.Count} words");
            WordRadix.AddWords(lstAllWords);
            // now verify
            var nWords = 0;
            var maxDepth = 0;
            WordRadix.WalkTreeWords((str, nDepth) =>
            {
                nWords++;
                maxDepth = Math.Max(nDepth, maxDepth);
                return true;//continue
            });
            Trace.WriteLine($"Tree has #words={nWords}  MaxDepth= {maxDepth} # nodes = {WordRadix.TotalNodes}");

            lstAllWords.ForEach(w => Assert.IsTrue(WordRadix.IsWord(w), $"{w} not found"));


            WordRadix.VerifyTree(lstAllWords, NumWordsExpected: lstAllWords.Count);

            var maxNodeLength = 0;
            var maxnodelengstr = string.Empty;
            WordRadix.WalkTreeNodes((node, depth) =>
            {
                if (node.NodeString.Length>maxNodeLength)
                {
                    maxNodeLength = node.NodeString.Length;
                    maxnodelengstr = node.NodeString;
                }
                //                Trace.WriteLine($"{new string(' ', depth)} {node.NodeString} {(node.Children == null ? "0" : node.Children.Count)}");
                return true;
            });
            Trace.WriteLine($"Maximum node length = {maxNodeLength}  {maxnodelengstr}");


            // verify all nodes reachable via GetNextNode
            var curNode = WordRadix.RootNode;
            var wrdCnt = 0;
            while (true)
            {
                var node = curNode.GetNextNode();
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
            var lstBadWords = new List<string>() {
                "beckoningly",
                "testp",
                "foobar",
            };
            lstBadWords.ForEach(w =>
            {
                var resSeek = WordRadix.SeekWord(w, out var compResult);

            });
            lstBadWords.ForEach(w => Assert.IsFalse(WordRadix.IsWord(w), $"{w} found ??"));


            Assert.AreEqual(lstAllWords.Count, WordRadix.TotalWords);
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
        [TestMethod]
        public void TestBenchGenSubWords()
        {
            //*
            var config = ManualConfig.Create(BenchmarkDotNet.Configs.DefaultConfig.Instance);//.WithOptions(ConfigOptions.DisableOptimizationsValidator);
            BenchmarkRunner.Run<BenchGenSubWords>(config);
            /*/
            var x = new BenchGenSubWords()
            {
                InitialWord = "discounter"
            };
            //Word Testing == 45 subwords, Discount = 75
            x.DoWithWordNode();
            x.DoWithNone();
            x.DoWithHashSet();
            //*/

        }
    }

    [MemoryDiagnoser]
    public class BenchGenSubWords
    {
        public enum GenSubWordType
        {
            UseNone,
            UseWordNode,
            UseHashSet,
        }
        //        [ParamsAllValues]
        public GenSubWordType GenType { get; set; }

        [Params("discounter", "testing")]
        public string InitialWord { get; set; }
        public int MinLength = 3;
        public int MaxSubWords = int.MaxValue;
        private readonly DictionaryLib.DictionaryLib dict;

        public BenchGenSubWords()
        {
            dict = new DictionaryLib.DictionaryLib(DictionaryLib.DictionaryType.Small);
        }
        [Benchmark]
        public void DoWithNone()
        {
            var lst = dict.GenerateSubWords(InitialWord, out var numSearches);
            Console.WriteLine($"{InitialWord,12} None #SubWords= {lst.Count} #Searches={numSearches}");
            //var ndx = 0;
            //lst.ForEach(d => Console.WriteLine($"N {ndx++} {d}"));
        }
        [Benchmark]
        public void DoWithHashSet()
        {
            var lstAllWords = dict.GetAllWords();
            var hashSetSubWords = new SortedSet<string>();
            var numSearches = 0;
            DictionaryLib.DictionaryLib.PermuteString(InitialWord, LeftToRight: true, (str) =>
            {
                for (int i = MinLength; i <= str.Length; i++)
                {
                    var testWord = str.Substring(0, i);
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
                            break;
                        }
                    }
                }
                return hashSetSubWords.Count < MaxSubWords; // continue ?
            });
            Console.WriteLine($"{InitialWord,12} Hash #SubWords= {hashSetSubWords.Count} #Searches={numSearches}");
            //var ndx = 0;
            //hashSetSubWords.ToList().ForEach(d => Console.WriteLine($"H {ndx++} {d}"));
        }

        //        [Benchmark]
        public void DoWithWordNode()
        {
            var lstAllWords = dict.GetAllWords();
            WordTrie.AddWords(lstAllWords);
            var hashSetSubWords = new SortedSet<string>();
            DictionaryLib.DictionaryLib.PermuteString(InitialWord, LeftToRight: true, (str) =>
            {
                for (int i = MinLength; i <= str.Length; i++)
                {
                    var testWord = str.Substring(0, i);
                    var partial = dict.SeekWord(testWord, out var compResult);
                    if (!string.IsNullOrEmpty(partial) && compResult == 0)
                    {
                        hashSetSubWords.Add(testWord);
                    }
                    else
                    {
                        if (!partial.StartsWith(testWord))
                        {
                            break;
                        }
                    }
                }
                return hashSetSubWords.Count < MaxSubWords; // continue 
            });

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

    class WordRadix
    {
        public static WordRadix RootNode;
        public static int TotalNodes = 0;
        public static int TotalWords = 0;
        public static ComparerWordRadix comparerInstance = new();

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct SmallStuff
        {
            public byte NodePrefixLength;
            public byte ChildNumber;
            public bool IsNodeAWord;
        }
        static WordRadix TestNode = new(parentNode: null, nodePrefixLength: 0, "dummy", IsAWord: false); // used for testing a string using List.BinarySearch. Reuse same node to reduce mem consumption
        public WordRadix ParentNode;
        public SmallStuff smallStuff;
        public string NodeString;
        public int NodePrefixLength { get { return smallStuff.NodePrefixLength; } set { smallStuff.NodePrefixLength = (byte)value; } } // the # of chars missing before NodeString
        public bool IsNodeAWord { get { return smallStuff.IsNodeAWord; } set { smallStuff.IsNodeAWord = value; } } // a node with children can also be a word; E.G. "tea" can have a child "team"
        public int ChildNumber { get { return smallStuff.ChildNumber; } set { smallStuff.ChildNumber = (byte)value; } }
        public List<WordRadix> Children;
        public WordRadix(WordRadix parentNode, int nodePrefixLength, string str, bool IsAWord)
        {
            if (string.IsNullOrEmpty(str) && this != RootNode)
            {
                throw new Exception($"empty string not root?");
            }

            //unsafe
            //{

            //    var ss = new SmallStuff();
            //    var addr = (byte*)&ss;
            //    var size = sizeof(SmallStuff);
            //    var msize = Marshal.SizeOf(smallStuff);
            //    var msize2 = Marshal.SizeOf<SmallStuff>();
            //    var off1 = (byte*)&ss.IsNodeAWord - addr;
            //    var arr = new SmallStuff[2];
            //    //var addr2 = (byte*)&arr;
            //    arr[0] = new SmallStuff();
            //    //                var addrarr0 = (byte*)&arr[0];
            //    //fixed (byte *ptr0= (byte*)&arr[0]) {

            //    //};
            //    //arr[1]=new SmallStuff();
            //}
            NodeString = str;
            IsNodeAWord = IsAWord;
            NodePrefixLength = nodePrefixLength;
            ParentNode = parentNode;
            TotalNodes++;
        }
        public static void AddWords(List<string> lstAllWords)
        {
            var nCnt = 0;
            foreach (var word in lstAllWords)
            {
                IsWord(word, AddIfAbsent: true);
                nCnt++;
                //                VerifyTree(lstAllWords, nCnt);
            }
            VerifyTree(lstAllWords, nCnt);
        }
        public static void ClearAll()
        {
            TotalWords = 0;
            TotalNodes = 0;
            RootNode = null;
        }
        public static WordRadix AddWord(string word)
        {
            if (IsWord(word, AddIfAbsent: true))
            {

            }
            return RootNode;
        }
        public class ComparerWordRadix : IComparer<WordRadix>
        {
            int IComparer<WordRadix>.Compare(WordRadix x, WordRadix y)
            {
                var res = string.Compare(x.NodeString, y.NodeString);
                return res;
            }
        }

        internal static void VerifyTree(List<string> lstAllWords, int NumWordsExpected)
        {
            var strSoFar = string.Empty;
            var lstNodes = new List<WordRadix>();
            var nWords = 0;
            WalkTreeNodes((node, depth) =>
            {
                if (lstNodes.Count == depth)
                {
                    lstNodes.Add(node);
                }
                else if (lstNodes.Count < depth)
                {

                }
                else
                {
                    while (lstNodes.Count > depth)
                    {
                        lstNodes.RemoveAt(lstNodes.Count - 1);

                    }
                    lstNodes.Add(node);

                }
                if (node.Children != null)
                {
                    for (var i = 0; i < node.Children.Count; i++)
                    {
                        if (node.Children[i].ChildNumber != i)
                        {
                            throw new Exception($"{nameof(VerifyTree)} ChildNumber incorrect {node}");
                        }
                    }
                }
                if (node.IsNodeAWord)
                {
                    var wrd = string.Join("", lstNodes.Where(w => !string.IsNullOrEmpty(w.NodeString)).Select(w => w.NodeString).ToList());
                    if (!WordRadix.IsWord(wrd))
                    {
                        throw new Exception($"{nameof(VerifyTree)} Word not found {wrd}");
                    }
                    // now verify parent links
                    var wrdViaParents = string.Empty;
                    var curNode = node;
                    var preflen = wrd.Length;
                    while (curNode != null) // walk back from curnode to root via parentNode chain. Construct the word backwards
                    {
                        preflen -= curNode.NodeString.Length;
                        wrdViaParents = curNode.NodeString + wrdViaParents;
                        if (nWords > 1 && preflen != wrd.Length - wrdViaParents.Length)
                        {
                            throw new Exception($"{nameof(VerifyTree)} Word pref len not match {wrd} {wrdViaParents}");
                        }

                        curNode = curNode.ParentNode;
                    }
                    if (wrdViaParents != wrd)
                    {
                        throw new Exception($"{nameof(VerifyTree)} Word parents not match {wrd} {wrdViaParents}");
                    }
                    nWords++;
                }
                return true;///continue
            });
            if (nWords != NumWordsExpected)
            {
                throw new Exception($"{nameof(VerifyTree)} Expected: {NumWordsExpected}  WrdsFound={nWords} ");
            }
        }
        public static void WalkTreeNodes(Func<WordRadix, int, bool> func)
        {
            var fAbort = false;
            WalkTreeNodesRecur(RootNode, 0);
            void WalkTreeNodesRecur(WordRadix curNode, int depth)
            {
                if (fAbort)
                {
                    return;
                }
                if (!func(curNode, depth))
                {
                    fAbort = true;
                }
                if (curNode.Children != null)
                {
                    foreach (var child in curNode.Children)
                    {
                        WalkTreeNodesRecur(child, depth + 1);
                    }
                }
            }
        }
        public static void WalkTreeWords(Func<string, int, bool> func)
        {
            WalkWordsRecursive(RootNode, string.Empty, 0);
            void WalkWordsRecursive(WordRadix curNode, string strSoFar, int nDepth)
            {
                if (curNode.IsNodeAWord)
                {
                    if (strSoFar.Length != curNode.NodePrefixLength)
                    {
                        "".ToString();
                    }
                    if (!func(strSoFar + curNode.NodeString, nDepth))
                    {
                        return;
                    }
                }
                if (curNode.Children != null)
                {
                    foreach (var child in curNode.Children)
                    {
                        WalkWordsRecursive(child, strSoFar + curNode.NodeString, nDepth + 1);
                    }
                }
            }
        }
        public static string SeekWord(string testWord, out int compResult)
        {
            var strResult = string.Empty;
            compResult = 0;
            if (!IsWord(testWord, AddIfAbsent: false, out var closestNode))
            {
                var curnode = closestNode;
                while (curnode != null)
                {
                    curnode = curnode.GetNextNode();
                    if (curnode.IsNodeAWord)
                    {
                        var tmp = curnode.GetWord();
                        if (string.Compare(tmp, testWord) > 0)
                        {
                            strResult = tmp;
                            break;
                        }
                    }
                }
            }
            return strResult;
        }
        public static bool IsWord(string testword, bool AddIfAbsent = false)
        {
            return IsWord(testword, AddIfAbsent, out _);
        }
        public static bool IsWord(string testword, bool AddIfAbsent, out WordRadix closestNode)
        {
            var isWord = false;
            closestNode = RootNode;
            if (RootNode == null)
            {
                RootNode = new(parentNode: null, nodePrefixLength: 0, testword, IsAWord: true);
                TotalWords++;
                return true;
            }
            WordRadix curNode = RootNode;
            var len = 0;
            var lstNodesVisited = new List<WordRadix>(); // doesn't work for AddMode when we're building the tree because of node splits
            while (true) //while (curNode != null && len < testword.Length)// find a node in the tree to which the testword is being added.
            {   // the word must always belong to the current node. Determine if we need to split the curNode or add the word as a descendant (if the word matches the entire node value)
                lstNodesVisited.Add(curNode);
                if (testword.Substring(len).StartsWith(curNode.NodeString)) // if the word matches the prefix completely. We don't need to split the node, but we need to add it as a descendant
                {
                    if (!AddIfAbsent && len + curNode.NodeString.Length == testword.Length && curNode.IsNodeAWord) // if we're not adding and exact match with node
                    {
                        isWord = true;
                        break;
                    }
                    len += curNode.NodeString.Length;
                    TestNode.NodeString = testword.Substring(len);
                    if (curNode.Children == null) // with no children, we add the word as a childnode and we're done
                    {
                        if (!AddIfAbsent)
                        {
                            isWord = false; // not found
                            closestNode = curNode;
                            break;
                        }
                        curNode.Children = new() { new WordRadix(curNode, nodePrefixLength: len, TestNode.NodeString, isWord = true) };
                    }
                    else
                    { // we need to descend to find the target node
                        var res = curNode.Children.BinarySearch(TestNode, WordRadix.comparerInstance);
                        if (res == 0)// exact match. word is already in tree
                        {
                            if (!AddIfAbsent)
                            {
                                isWord = true;
                            }
                            break;
                        }
                        else if (res > 0) // found the node to which the word belongs
                        {
                            curNode = curNode.Children[res]; //descend
                            continue;
                        }
                        else
                        {
                            var targnodeNdx = (~res) - 1; // prior node
                            if (targnodeNdx == -1)
                            {
                                if (!AddIfAbsent)
                                {
                                    isWord = false; // not found
                                    closestNode = curNode;
                                    break;
                                }
                                throw new Exception($"How did we get here? Are words sorted? {testword} {curNode}");
                            }
                            var targnode = curNode.Children[targnodeNdx];
                            var prefndx = GetCommonPrefLength(TestNode.NodeString, targnode.NodeString);
                            if (prefndx > 0) // if belongs to the prior node 
                            {
                                curNode = targnode;
                                continue;
                            }
                            else
                            {
                                if (!AddIfAbsent)
                                {
                                    isWord = false; // not found
                                    closestNode = curNode;
                                    break;
                                }
                                curNode.Children.Add(new WordRadix(curNode, nodePrefixLength: len, TestNode.NodeString, IsAWord: true) { ChildNumber = curNode.Children.Count }); // add as last sibling
                            }
                        }
                    }
                }
                else
                {   //we need to split the node: curnode gets a shorter string and 2 children are created: the first with the same children as the curnode, the 2nd with the rest of the string
                    if (!AddIfAbsent)
                    {
                        throw new Exception($"how did we get here? {testword} {curNode} ");
                    }
                    var curnodeChildren = curNode.Children; // save these values to move them
                    var curnodeIsAWord = curNode.IsNodeAWord;// save these values to move them
                    var curnodePrefixLen = curNode.NodePrefixLength;// save these values to move them
                    var prefndx2 = GetCommonPrefLength(testword.Substring(len), curNode.NodeString);
                    var split1 = curNode.NodeString.Substring(0, prefndx2);
                    var split2 = curNode.NodeString.Substring(prefndx2);
                    var child2 = testword.Substring(len + prefndx2);
                    curNode.IsNodeAWord = false;
                    curNode.NodeString = split1;
                    var newchild1 = new WordRadix(curNode, nodePrefixLength: curnodePrefixLen + split1.Length, split2, curnodeIsAWord)
                    {
                        Children = curnodeChildren
                    }; // copy all from curnode
                    if (curnodeChildren != null)
                    {
                        foreach (var pnode in curnodeChildren) // reparent each of the kids
                        {
                            pnode.ParentNode = newchild1;
                        }
                    }
                    curNode.Children = new List<WordRadix>() { newchild1 };
                    if (!string.IsNullOrEmpty(child2))
                    {
                        curNode.Children.Add(new WordRadix(curNode, nodePrefixLength: len + split1.Length, child2, IsAWord: true) { ChildNumber = curNode.Children.Count });
                    }

                }
                TotalWords++;
                break;
            }
            return isWord;
        }
        static int GetCommonPrefLength(string word1, string word2)
        {
            var prefndx = -1;
            for (var i = 0; i < Math.Min(word1.Length, word2.Length); i++)
            {
                if (word1[i] != word2[i])
                {
                    prefndx = i;
                    break;
                }
            }
            if (prefndx == -1)
            {
                prefndx = Math.Min(word1.Length, word2.Length);
            }
            return prefndx;
        }
        public string GetWord()
        {
            var str = string.Empty;
            var curnode = this;
            while (curnode != RootNode)
            {
                str = curnode.NodeString + str;
                curnode = curnode.ParentNode;
            }
            return str;
        }

        public WordRadix GetNextNode()
        {
            var curNode = this;
            var down = true;
            while (curNode != null)
            {
                if (down)
                {
                    if (curNode.Children != null)
                    {
                        curNode = curNode.Children[0];
                        break;
                    }
                    else
                    {
                        down = false;
                        continue;
                    }
                }
                else
                {
                    if (curNode.ParentNode == null) // root?
                    {
                        curNode = null;
                        break;
                    }
                    if (curNode.ChildNumber + 1 < curNode.ParentNode.Children.Count)
                    {
                        curNode = curNode.ParentNode.Children[curNode.ChildNumber + 1];
                        break;
                    }
                }
                curNode = curNode.ParentNode;
            }
            return curNode;
        }
        public override string ToString()
        {
            var str = $"{NodeString} Children={(Children == null ? "null" : Children.Count)} {nameof(IsNodeAWord)}={IsNodeAWord} ";
            if (Children != null)
            {
                str += string.Join(",", Children.Select(e => e.NodeString).ToArray());
            }
            return str;
        }
    }
}
