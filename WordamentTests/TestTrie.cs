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
            var bytes = System.Text.Encoding.ASCII.GetBytes("test");
            var wrd = Encoding.ASCII.GetString(bytes);
            var lstAllWords = dictionarySmall.GetAllWords();

            WordTrie.AddWords(lstAllWords);
            var testword = "testp";
            var iss = dictionarySmall.SeekWord(testword, out var compResult);
            var x = WordTrie.IsWord(testword, out var nodePath);
            var str = WordTrie.GetStringFromPath(nodePath);
            TestContext.WriteLine($"{dictionarySmall}  WordNode Count = {WordTrie.NodeCnt}");
        }

        [TestMethod]
        public void TestWordRadix()
        {
            var dictionarySmall = new DictionaryLib.DictionaryLib(DictionaryLib.DictionaryType.Small);
            var lstAllWords = dictionarySmall.GetAllWords().Take(1000000).ToList();
            //var lstAllWords = new List<string>() { "a", "aardvark", "aback", "abacus", "abacuses", "abandon" };
            //var lstAllWords = new List<string>() { "ta1", "ta2", "ta4", "ta5", "ta3", "ta6", "test" };
            //            var lstAllWords = new List<string>() { "test", "toaster", "toasting", "vslow", "vslowly" };
            WordRadix.ClearAll();
            Trace.WriteLine($"Adding words");
            WordRadix.AddWords(lstAllWords);
            var testword = "testp";
            var x = WordRadix.IsWord(testword);


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
            /*
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
        public string value;
        public List<WordRadix> Children;
        public bool IsNodeAWord; // a node with children can also be a word; E.G. "tea" can have a child "team"
        public WordRadix()
        {
            TotalNodes++;
            //            RootNode = new();
        }
        public static WordRadix AddWord(string word)
        {
            if (IsWord(word, (maxMatchNode, len) =>
            {
            }))
            {

            }
            return RootNode;
        }
        public class ComparerWordRadix : IComparer<WordRadix>
        {
            int IComparer<WordRadix>.Compare(WordRadix x, WordRadix y)
            {
                var res = string.Compare(x.value, y.value);
                return res;
            }
        }
        public static void WalkTree(Func<string, bool> func)
        {
            WalkRecursive(RootNode, string.Empty);
            void WalkRecursive(WordRadix curNode, string strSoFar)
            {
                if (curNode.IsNodeAWord)
                {
                    if (!func(strSoFar + curNode.value))
                    {
                        return;
                    }
                }
                if (curNode.Children != null)
                {
                    foreach (var child in curNode.Children)
                    {
                        WalkRecursive(child, strSoFar + curNode.value);
                    }
                }
            }
        }
        public static bool IsWord(string testword, Action<WordRadix, int> act = null)
        {
            var isWord = false;
            if (RootNode == null)
            {
                RootNode = new() { value = testword, IsNodeAWord = true };
                TotalWords++;
                return true;
            }
            WordRadix curNode = RootNode;
            if (testword == "babbling")
            {
                "".ToString();
            }
            var len = 0;
            while (curNode != null && len < testword.Length)// find a node in the tree to which the testword is being added.
            {
                // the word must always belong to the current node. Determine if we need to split the curNode or add the word as a descendant (if the word matches the entire node value)
                var testRest = testword.Substring(len);

                if ((curNode.value.Length == 0 && curNode == RootNode) || testRest.StartsWith(curNode.value)) // the word matches the prefix completely. We don't need to split the node, but we need to add it as a descendant
                {
                    if (curNode.Children == null) // with no children, we add the word as a childnode and we're done
                    {
                        len += curNode.value.Length;
                        testRest = testword.Substring(len);
                        curNode.Children = new()
                        {
                            new WordRadix() { value = testRest, IsNodeAWord=true},
                        };
                    }
                    else
                    { // we need to descend to find the target node: use binary search to find which child node to use
                        len += curNode.value.Length;
                        var tnode = new WordRadix() { value = testword.Substring(len) };
                        var res = curNode.Children.BinarySearch(tnode, WordRadix.comparerInstance);
                        if (res == 0)
                        { // exact match. word is already in tree

                        }
                        else if (res > 0)
                        {
                            curNode = curNode.Children[res];
                            continue;
                        }
                        else
                        {
                            var targnodeNdx = (~res) - 1; // prior node
                            var targnode = curNode.Children[targnodeNdx];
                            if (targnodeNdx == curNode.Children.Count - 1) // we don't want to descend, we want to add as a new sibling
                            {
                                var prefndx = GetCommonPrefLength(targnode.value, testword.Substring(len));
                                if (testword.Substring(len).StartsWith(targnode.value)) // if belongs to the prior node 
                                                                                        //                                    if (prefndx < targnode.value.Length || testword.Substring(len).StartsWith(targnode.value)) // if belongs to the prior node 
                                {
                                    curNode = targnode;
                                    continue;
                                }
                                curNode.Children.Add(
                                        new WordRadix() { value = testword.Substring(len), IsNodeAWord = true } // add as sibling
                                    );
                            }
                            else
                            {
                                if (testword.Substring(len).StartsWith(targnode.value)) // if belongs to the prior node 
                                {
                                    curNode = targnode;
                                    len += curNode.value.Length;
                                    continue;
                                }
                            }
                        }
                    }

                    TotalWords++;
                    break;
                }
                "".ToString();
                //we need to split the node
                var curnodeChildren = curNode.Children;
                curNode.Children = new List<WordRadix>()
                    {
                        new WordRadix() { value = curNode.value, IsNodeAWord=curNode.IsNodeAWord, Children = curnodeChildren }, // copy all from curnode
                        new WordRadix() {value = testword.Substring(len), IsNodeAWord = true}
                    };
                curNode.IsNodeAWord = false;
                curNode.value = string.Empty;
                break;
                //if (string.Compare(curNode.value, testword) > 0)
                //{
                //}
                //if (!testword.Substring(len).StartsWith(curNode.value)) // the word does not belong to the node?
                //{
                //    var prefndx = GetCommonPrefLength(curNode.value, testword);
                //    var nodeRest = curNode.value.Substring(prefndx);
                //    curNode.value = curNode.value.Substring(0, prefndx);
                //    var curnodeChildren = curNode.Children;
                //    curNode.Children = new List<WordRadix>()
                //    {
                //        new WordRadix() { value = nodeRest, IsNodeAWord=curNode.IsNodeAWord, Children = curnodeChildren },
                //        new WordRadix() {value = testword.Substring(prefndx), IsNodeAWord = true}
                //    };
                //    curNode.IsNodeAWord = false;
                //    TotalWords++;
                //    break;

                //}
                //if (curNode.Children != null)
                //{
                //    len += curNode.value.Length;
                //    var tnode = new WordRadix() { value = testword.Substring(len) };
                //    var res = curNode.Children.BinarySearch(tnode, WordRadix.comparerInstance);
                //    if (res >= 0)
                //    {

                //    }
                //    else
                //    {
                //        var targnodeNdx = (~res) - 1;
                //        var targnode = curNode.Children[targnodeNdx];
                //        var restTestWord = testword.Substring(len);
                //        var prefndx = GetCommonPrefLength(targnode.value, restTestWord);
                //        if (prefndx == 0) // no common prefix
                //        {
                //            curNode.Children.Insert(targnodeNdx + 1, new WordRadix() { value = restTestWord, IsNodeAWord = true });
                //        }
                //        else
                //        {
                //            curNode = curNode.Children[targnodeNdx];
                //            var nodeRest = curNode.value.Substring(prefndx);
                //            curNode.value = curNode.value.Substring(0, prefndx);
                //            curNode.Children = new List<WordRadix>()
                //            {
                //                new WordRadix() { value = nodeRest, IsNodeAWord=true },
                //                new WordRadix() {value = restTestWord.Substring(prefndx), IsNodeAWord=true}
                //            };
                //            curNode.IsNodeAWord = false;
                //        }
                //        TotalWords++;
                //    }
                //    break;

                //}
                //else
                //{ // curnode has no children, so we split it, insert and we're done
                //    if (curNode.value == testword.Substring(len))
                //    {
                //        return curNode.IsNodeAWord; // is a word
                //    }
                //    var prefndx = GetCommonPrefLength(curNode.value, testword);
                //    var nodeRest = curNode.value.Substring(prefndx);
                //    if (nodeRest.Length == 0) // a split would result in an empty parent node, so we just add one child
                //    {
                //        curNode.Children = new()
                //        {
                //            new WordRadix() {value = testword.Substring(prefndx),IsNodeAWord=true}
                //        };
                //    }
                //    else
                //    {
                //        curNode.value = curNode.value.Substring(0, prefndx);
                //        curNode.Children = new List<WordRadix>()
                //            {
                //                new WordRadix() { value = nodeRest, IsNodeAWord=curNode.IsNodeAWord },
                //                new WordRadix() {value = testword.Substring(prefndx), IsNodeAWord = true}
                //            };
                //        curNode.IsNodeAWord = false;
                //    }
                //    TotalWords++;
                //    break;
                //}
            }

            return isWord;
        }
        void SplitNode(WordRadix targetNode, string value) // insert a value node into the targetnode
        {

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

        public static void AddWords(List<string> lstAllWords)
        {
            var nWords = 0;
            foreach (var word in lstAllWords)
            {
                IsWord(word);
                var ndx = 0;
                WalkTree((str) =>
                {
                    if (nWords == ndx && word != str)
                    {
                        Trace.WriteLine($"{nWords} {ndx} {word}  {str}");
                    }
                    //    Trace.WriteLine($"{ndx,4} {str}");
                    ndx++;
                    return true; //continue
                });
                nWords++;
            }
        }
        public static void ClearAll()
        {
            TotalWords = 0;
            TotalNodes = 0;
            RootNode = null;
        }
        public override string ToString()
        {
            var str = $"{value} Children={(Children == null ? "null" : Children.Count)} {nameof(IsNodeAWord)}={IsNodeAWord} ";
            if (Children != null)
            {
                str += string.Join(",", Children.Select(e => e.value).ToArray());
            }
            return str;
        }

    }

    //class WordRadix
    //{
    //    public class Edge
    //    {
    //        public WordRadix TargetNode;
    //        public string value;
    //        public override string ToString() => $"{value} TargetNode = {TargetNode}";
    //    }
    //    public static WordRadix RootNode;
    //    public List<Edge> Edges;
    //    public bool IsLeaf=> Edges== null;
    //    static WordRadix()
    //    {
    //        RootNode = new();
    //    }
    //    public static WordRadix AddWord(string word)
    //    {
    //        if (IsWord(word, (node) =>
    //        {
    //            node.Edges = new List<Edge>()
    //                {
    //                    new Edge() { TargetNode = null, value = word }
    //                };

    //        }))
    //        {

    //        }
    //        return RootNode;
    //    }

    //    public static void AddWords(List<string> lstAllWords)
    //    {
    //        foreach (var word in lstAllWords)
    //        {
    //            AddWord(word);
    //        }
    //    }

    //    public static bool IsWord(string testword, Action<WordRadix> act = null)
    //    {
    //        var isWord = false;
    //        WordRadix curNode = RootNode;
    //        var elementsFound = 0;
    //        while (curNode != null && !curNode.IsLeaf && elementsFound < testword.Length)
    //        {
    //            foreach (var edge in curNode.Edges)
    //            {

    //            }

    //        }
    //        act?.Invoke(curNode);
    //        return isWord;
    //    }
    //    public override string ToString()
    //    {
    //        var str = $"IsLeaf={IsLeaf} Edges={(Edges == null ? "null" : Edges.Count)} ";
    //        if (Edges != null)
    //        {
    //            str += string.Join(",", Edges.Select(e => e.value).ToArray());
    //        }
    //        return str;
    //    }
    //}

}
