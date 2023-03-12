using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace WordamentTests
{
    public class WordRadix
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
        SmallStuff smallStuff;
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
                while (true)
                {
                    if (curnode == null)
                    {
                        break;
                    }
                    var tmp = curnode.GetWord();
                    if (string.Compare(tmp, testWord) > 0)
                    {
                        compResult = -1;
                        strResult = tmp;
                        break;
                    }
                    curnode = curnode.GetNextNode(OnlyWordNodes: true);
                }
            }
            else
            {
                strResult = testWord;
                compResult = 0;
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
                            closestNode = curNode.GetNextNode(OnlyWordNodes: true, InitialDirectionDown: false); // the node on which the word would go: would be just before testword in alpha order
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
                                    closestNode = curNode.GetNextNode(OnlyWordNodes: true, InitialDirectionDown: true); // before testnode in alpha order. Would be 1st child of closestnode
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
                                    closestNode = targnode.GetNextNode(OnlyWordNodes: true, InitialDirectionDown: false); // before testword in alpha order. did not descend into node because prefndx == 0 
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
                        isWord = false; //not found: TestWord == "cids"  curnode=="(ci)der". 
                        closestNode = curNode.GetNextNode(OnlyWordNodes: true, InitialDirectionDown: true);
                        break;
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

        public WordRadix GetNextNode(bool OnlyWordNodes, bool InitialDirectionDown = true)
        {
            var curNode = this;
            while (curNode != null)
            {
                curNode = GetNextNode(curNode, InitialDirectionDown);
                if (!OnlyWordNodes || curNode.IsNodeAWord)
                {
                    break;
                }
                InitialDirectionDown = true;
            }
            return curNode;
        }
        static WordRadix GetNextNode(WordRadix curNode, bool InitialDirectionDown)
        {
            var down = InitialDirectionDown;
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
