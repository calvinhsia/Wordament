using DictionaryData;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("WordamentTests")]

namespace DictionaryLib
{
    //todo find words in small not in large "miscinceptions", "containedness"
    public class DictionaryResult
    {
        public string Word;
        internal DictionaryState dictionaryState;
        public DictionaryResult GetNextResult()
        {
            return this.dictionaryState.GetNextWord();
        }
    }
    internal class DictionaryState
    {
        internal DictionaryLib dictionary;
        internal byte partialNib = 0;
        internal bool havePartialNib = false;
        internal int nibndx;
        internal string wordSoFar = string.Empty;
        internal DictionaryResult GetNextWord()
        {
            DictionaryResult dictionaryResult = new DictionaryResult();
            return dictionaryResult;
        }
        byte GetNextNib()
        {
            byte result;
            if (havePartialNib)
            {
                result = partialNib;
            }
            else
            {
                var ndx = Marshal.SizeOf<DictHeader>() + nibndx / 2;
                if (ndx < dictionary._dictBytes.Length)
                {
                    partialNib = dictionary._dictBytes[ndx];
                    result = (byte)(partialNib >> 4);
                    partialNib = (byte)(partialNib & 0xf);
                }
                else
                {
                    result = DictHeader.EOFChar;
                }
            }
            nibndx++;
            havePartialNib = !havePartialNib;
            //                LogMessage($"  GetNextNib {nibndx} {result}");
            return result;
        }
    }

    public enum DictionaryType
    {
        /// <summary>
        /// About 171201 words
        /// </summary>
        Large = 1,
        /// <summary>
        /// 53869 words
        /// </summary>
        Small = 2
    }

    public class DictionaryLib
    {
        internal DictHeader _dictHeader;
        internal int _dictHeaderSize;
        internal DictionaryType _dictionaryType;
        internal byte[] _dictBytes;
        internal Random _random;

        byte _partialNib = 0;
        bool _havePartialNib = false;
        int _nibndx;

        readonly MyWord _MyWordSoFar;
        internal static Action<string> logMessageAction;
        internal static void LogMessage(string msg)
        {
            logMessageAction?.Invoke(msg);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dictType"></param>
        /// <param name="randSeed">0 means random seed TickCount. >0 means use as seed </param>
        public DictionaryLib(DictionaryType dictType, Random random = null)
        {
            this._dictionaryType = dictType;

            //var asm = System.Reflection.Assembly.GetExecutingAssembly();
            //var names = asm.GetManifestResourceNames(); // "Dictionary.Properties.Resources.resources"

            //var res = asm.GetManifestResourceInfo(names[0]);

            //var resdata = asm.GetManifestResourceStream(names[0]);

            //var resman = new System.Resources.ResourceManager("Dictionary.Properties.Resources", typeof(Dictionary).Assembly);
            //var dict1 = (byte[])resman.GetObject("dict1");

            if (dictType == DictionaryType.Large)
            {
                _dictBytes = Properties.Resources.dict1;
            }
            else
            {
                _dictBytes = Properties.Resources.dict2;
            }
            if (random == null)
            {
                _random = new Random(Environment.TickCount);
            }
            else
            {
                _random = random;
            }
            _dictHeader = DictionaryData.DictHeader.MakeHeaderFromBytes(_dictBytes);
            _dictHeaderSize = Marshal.SizeOf<DictHeader>();
            _MyWordSoFar = new MyWord(_dictHeader.maxWordLen);
        }

        public string SeekWord(string word)
        {
            return SeekWord(word, out var compResult);
        }

        /// <summary>
        /// Seek in dictionary to provided "word".
        /// if found in dict, returns the same string
        /// if not found, returns the word just beyond where the word would be found.
        /// IOW, 2 consecutive words in dictionary: abcdefone, abcdeftwo (and dict does not have abc,abcdefg)
        /// Search for 
        ///     abc => returns abcdef (abc not found)
        ///     abcdef=> returns abcdef (match)
        ///     abcdefg=> returns abcdefone
        ///     acbdefs=> returns abcdeftwo
        /// 
        ///// </summary>
        /// </summary>
        /// <param name="word"></param>
        /// <returns></returns>
        public string SeekWord(string word, out int compResult)
        {
            word = word.ToLower();
            var result = string.Empty;
            compResult = 0;
            byte let1 = 0;
            byte let2 = 0; //'a'
            if (word.Length > 1)
            {
                let1 = (byte)(word[0] - 97);
            }
            let2 = 0; //'a'
            if (word.Length > 1)
            {
                let2 = (byte)(word[1] - 97);
            }
            SetDictPosTo2Letters(let1, let2);
            result = GetNextWord(out compResult, WordStop: word);
            return result;
        }

        void SetDictPosTo2Letters(byte let1, byte let2 = 0)
        {
            _havePartialNib = false;
            _nibndx = _dictHeader.nibPairPtr[let1 * 26 + let2].nibbleOffset;
            _MyWordSoFar.SetWord((byte)(let1 + 97), (byte)(let2 + 97));
            if ((int)(_nibndx & 1) > 0)
            {
                GetNextNib();
            }
        }
        byte GetNextNib()
        {
            byte result;
            if (_havePartialNib)
            {
                result = _partialNib;
            }
            else
            {
                var ndx = _dictHeaderSize + _nibndx / 2;
                if (ndx < _dictBytes.Length)
                {
                    _partialNib = _dictBytes[ndx];
                    result = (byte)(_partialNib >> 4);
                    _partialNib = (byte)(_partialNib & 0xf);
                }
                else
                {
                    result = DictHeader.EOFChar;
                }
            }
            _nibndx++;
            _havePartialNib = !_havePartialNib;
            //                LogMessage($"  GetNextNib {nibndx} {result}");
            return result;
        }
        public string GetNextWord()
        {
            return GetNextWord(out int compareResult, WordStop: null, cntSkip: 0);
        }

        internal string GetNextWord(string WordStop = null, int cntSkip = 0)
        {
            return GetNextWord(out int compareResult, WordStop, cntSkip);
        }

        /// <summary>
        /// must init dic first SeekWord.
        /// </summary>
        /// <param name="WordStop">must init dict first. then will stop when dict word >= WordStop</param>
        /// <param name="cntSkip"> nonzero means skip this many words (used for RandomWord). 
        /// 0 means return next word. 1 means skip one word. For perf: don't have to convert to string over and over</param>
        /// <param name="compareResult">if provided, </param>
        /// <returns></returns>
        internal string GetNextWord(out int compareResult, string WordStop = null, int cntSkip = 0)
        {
            Debug.Assert((WordStop == null) || cntSkip == 0);
            byte nib = 0;
            MyWord stopAtWord = null;
            compareResult = 0;
            if (!string.IsNullOrEmpty(WordStop))
            {
                stopAtWord = new MyWord(WordStop);
            }
            var done = false;
            int loopCount = 0;
            while (!done)
            {
                var lenSoFar = 0;
                while ((nib = GetNextNib()) == 0xf)
                {
                    lenSoFar += nib;
                }
                if (nib == DictHeader.EOFChar)
                {
                    //              LogMessage($"Got EOD {_nibndx}");
                    return string.Empty;
                }
                lenSoFar += nib;
                if (lenSoFar < _MyWordSoFar.WordLength)
                {
                    _MyWordSoFar.SetLength(lenSoFar);
                }
                while ((nib = GetNextNib()) != 0)
                {
                    char newchar;
                    if (nib == DictHeader.escapeChar)
                    {
                        nib = GetNextNib();
                        newchar = _dictHeader.tab2[nib];
                    }
                    else
                    {
                        if (nib == DictHeader.EOFChar)
                        {
                            //                        LogMessage($"GOT EODCHAR {_nibndx:x2}");
                            break;
                        }
                        newchar = _dictHeader.tab1[nib];
                    }
                    _MyWordSoFar.AddByte((byte)newchar);
                }
                if (nib == DictHeader.EOFChar)
                {
                    return string.Empty;
                }
                if (stopAtWord != null)
                {
                    var cmp = _MyWordSoFar.CompareTo(stopAtWord);
                    if (cmp >= 0)
                    {
                        compareResult = cmp;
                        break;
                    }
                }
                else
                {
                    if (cntSkip != 0)
                    {
                        if (loopCount++ == cntSkip)
                        {
                            break;
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }
            return _MyWordSoFar.GetWord();
        }

        public void FindAnagrams(string word, Action<string> act)
        {
            MyWord myWord = new MyWord(word);
            // sort bytes
            for (int i = 0; i < myWord.WordLength; i++)
            {
                for (int j = 0; j < i; j++)
                {
                    if (myWord._wordBytes[i] < myWord._wordBytes[j])
                    {
                        var tmp = myWord._wordBytes[i];
                        myWord._wordBytes[i] = myWord._wordBytes[j];
                        myWord._wordBytes[j] = tmp;
                    }
                }
            }
            var lstAnagrams = new HashSet<string>();
            LogMessage($"DoAnagram {myWord}");
            var origLen = myWord.WordLength;
            int nCnt = 0;
            RecurFindAnagram(0);
            void RecurFindAnagram(int nLevel)
            {
                for (int i = nLevel; i < myWord.WordLength; i++)
                {
                    if (nLevel > 1)// tree pruning
                    {
                        myWord.SetLength(nLevel);
                        var testWord = myWord.GetWord();
                        var partial = SeekWord(testWord, out var compResult);
                        myWord.SetLength(origLen);
                        if (!partial.StartsWith(testWord))
                        {
                            LogMessage($"prune {nLevel}  {testWord}  {partial}");
                            return;
                        }
                    }
                    byte tmp = myWord._wordBytes[i]; // swap nlevel and i. These will be equal 1st time through for identity permutation
                    myWord._wordBytes[i] = myWord._wordBytes[nLevel];
                    myWord._wordBytes[nLevel] = tmp;
                    if (nLevel + 1 < myWord.WordLength)
                    {
                        RecurFindAnagram(nLevel + 1);
                    }
                    else
                    { // got full permutation
                        var candidate = myWord.GetWord();
                        nCnt++;
                        LogMessage($"FAnag {nCnt,3} {candidate}");
                        //                        LogMessage($"   cand {nLevel} {candidate}");
                        if (IsWord(candidate))
                        {
                            if (!lstAnagrams.Contains(candidate))
                            {
                                LogMessage($"   GotAnag {nLevel} {candidate}");
                                lstAnagrams.Add(candidate);
                                act(candidate);
                            }
                        }
                    }
                    // restore swap
                    //                        var tmp2 = myWord._wordBytes[nLevel];
                    myWord._wordBytes[nLevel] = myWord._wordBytes[i];
                    myWord._wordBytes[i] = tmp;
                }
            }
        }

        public bool IsWord(string word)
        {
            bool isWord = false;
            if (!string.IsNullOrEmpty(word))
            {
                word = word.ToLower();
                switch (word.Length)
                {
                    case 1:
                        if (word == "a" || word == "i")
                        {
                            isWord = true;
                        }
                        break;
                    default:
                        var testWord = SeekWord(word, out var cmp);
                        if (cmp == 0)
                        {
                            isWord = true;
                        }
                        break;
                }
            }
            return isWord;
        }

        public string RandomWord()
        {
            var rnum = _random.Next(_dictHeader.wordCount);
            int sum = 0, i = 0, j = 0;
            for (i = 0; i < 26; i++)
            {
                for (j = 0; j < 26; j++)
                {
                    var cnt = _dictHeader.nibPairPtr[i * 26 + j].cnt;
                    if (sum + cnt < rnum)
                    {
                        sum += cnt;
                    }
                    else
                    {
                        SetDictPosTo2Letters((byte)i, (byte)j);
                        var r = GetNextWord(cntSkip: rnum - sum);
                        return r;
                    }
                }
            }
            throw new InvalidOperationException();
        }

        internal IEnumerable<string> FindMatchRegEx(string strPattern)
        {
            var wrd = SeekWord("a");
            while (!string.IsNullOrEmpty(wrd = GetNextWord()))
            {
                if (System.Text.RegularExpressions.Regex.IsMatch(wrd, strPattern))
                {
                    yield return wrd;
                }
            }
        }
    }


    /// <summary>
    /// represent a word as a byte array, reducing need to convert to char/string for perf
    /// </summary>
    internal class MyWord : IComparable
    {
        readonly private int maxWordLen;
        internal byte[] _wordBytes;
        int _currentLength;

        MyWord()
        {
            this.maxWordLen = 30;
            this._wordBytes = new byte[this.maxWordLen];
            _currentLength = 0;
        }
        public MyWord(int maxWordLen) : this()
        {
        }

        public MyWord(string word) : this()
        {
            SetWord(word);
        }

        public void SetWord(string word)
        {
            _currentLength = word.Length;
            for (int ndx = 0; ndx < word.Length; ndx++)
            {
                _wordBytes[ndx] = (byte)word[ndx];
            }
        }
        public void SetWord(byte byte1, byte byte2)
        {
            _currentLength = 2;
            _wordBytes[0] = byte1;
            _wordBytes[1] = byte2;
        }
        public string GetWord()
        {
            return Encoding.ASCII.GetString(_wordBytes, 0, _currentLength);
        }
        public void AddByte(byte b)
        {
            _wordBytes[_currentLength++] = b;
        }
        public int WordLength => _currentLength;
        public void SetLength(int Length)
        {
            _currentLength = Length;
        }

        public int CompareTo(object obj)
        {
            int retval = 0;
            if (obj is MyWord other)
            {
                for (int i = 0; i < Math.Min(this._currentLength, other._currentLength); i++)
                {
                    if (this._wordBytes[i] != other._wordBytes[i])
                    {
                        retval = this._wordBytes[i].CompareTo(other._wordBytes[i]);
                        if (retval != 0)
                        {
                            break;
                        }
                    }
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
        public override string ToString()
        {
            return GetWord();
        }
    }

}
