using DictionaryData;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("WordamentTests")]

namespace DictionaryLib
{
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
        public const byte LetterA = 97; // 'a'
        public const int NumLetters = 26; // # letters in alphabet
        public const char qmarkChar = '_';// + 1 - LetterA;

        internal DictHeader _dictHeader;
        internal int _dictHeaderSize;
        internal DictionaryType _dictionaryType;
        internal byte[] _dictBytes;
        internal Random _random;

        byte _partialNib = 0;
        bool _havePartialNib = false;
        int _nibndx;
        internal int _GetNextWordCount;

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
            return SeekWord(word, out var _);
        }

        /// <summary>
        /// Seek in dictionary to provided "word".
        /// if found in dict, returns the same string
        /// if not found, returns the word just beyond where the word would be found.
        /// IOW, 2 consecutive words in dictionary: abcdefone, abcdeftwo (and dict does not have abc,abcdefg)
        /// Search for 
        ///     abc => returns abcdef (abc not found)
        ///     abcdefone=> returns abcdefone (match)
        ///     abcdefg=> returns abcdefone
        ///     abcdefs=> returns abcdeftwo
        /// 
        ///// </summary>
        /// </summary>
        /// <param name="word"></param>
        /// <returns></returns>
        public string SeekWord(string word, out int compResult)
        {
            word = word.ToLower();
            byte let0 = LetterA;
            byte let1 = LetterA;
            byte let2 = LetterA;
            if (word.Length > 0)
            {
                let0 = (byte)(word[0]);
            }
            if (word.Length > 1)
            {
                let1 = (byte)(word[1]);
            }
            if (word.Length > 2)
            {
                let2 = (byte)(word[2]);
            }
            SetDictPos(let0, let1, let2);
            var result = GetNextWord(out compResult, WordStop: new MyWord(word));
            return result.GetWord();
        }

        void SetDictPos(byte let0, byte let1 = LetterA, byte let2 = LetterA)
        {
            _havePartialNib = false;
            _nibndx = _dictHeader.nibPairPtr[((let0 - LetterA) * NumLetters + let1 - LetterA) * NumLetters + let2 - LetterA].nibbleOffset;
            _GetNextWordCount = 0;
            _MyWordSoFar.SetWord(let0, let1, let2);
            if ((int)(_nibndx & 1) > 0)
            {
                GetNextNib();
            }
        }

        void SetDictPos(MyWord mword)
        {
            byte let0;
            byte let1 = LetterA;
            byte let2 = LetterA;
            switch (mword.WordLength)
            {
                case 0:
                    throw new InvalidOperationException("word length 0?");
                default:
                    let0 = mword._wordBytes[0];
                    if (mword.WordLength > 1)
                    {
                        let1 = mword._wordBytes[1];
                    }
                    if (mword.WordLength > 2)
                    {
                        let2 = mword._wordBytes[2];
                    }
                    break;
            }
            SetDictPos(let0, let1, let2);
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
            return GetNextWord(out int _, WordStop: null, cntSkip: 0)?.GetWord();
        }

        internal MyWord GetNextWord(MyWord WordStop = null, int cntSkip = 0)
        {
            return GetNextWord(out int _, WordStop, cntSkip);
        }

        /// <summary>
        /// must init dic first SeekWord.
        /// </summary>
        /// <param name="WordStop">must init dict first. then will stop when dict word >= WordStop</param>
        /// <param name="cntSkip"> nonzero means skip this many words (used for RandomWord). 
        /// 0 means return next word. 1 means skip one word. For perf: don't have to convert to string over and over</param>
        /// <param name="compareResult">if provided, </param>
        /// <returns></returns>
        internal MyWord GetNextWord(out int compareResult, MyWord WordStop = null, int cntSkip = 0)
        {
            Debug.Assert((WordStop == null) || cntSkip == 0);
            byte nib;
            MyWord stopAtWord = null;
            compareResult = 0;
            if (WordStop != null)
            {
                stopAtWord = WordStop;
            }
            var done = false;
            int loopCount = 0;
            while (!done)
            {
                _GetNextWordCount++;
                var lenSoFar = 0;
                while ((nib = GetNextNib()) == 0xf)
                {
                    lenSoFar += nib;
                }
                if (nib == DictHeader.EOFChar)
                {
                    //              LogMessage($"Got EOD {_nibndx}");
                    return MyWord.Empty;
                }
                lenSoFar += nib;
                if (lenSoFar < _MyWordSoFar.WordLength)
                {
                    _MyWordSoFar.SetLength(lenSoFar);
                }
                while ((nib = GetNextNib()) != 0)
                {
                    char newchar;
                    if (nib != DictHeader.EOFChar)
                    {
                        if (nib != DictHeader.escapeChar)
                        {
                            newchar = _dictHeader.tab1[nib];
                        }
                        else
                        {
                            nib = GetNextNib();
                            newchar = _dictHeader.tab2[nib];
                        }
                    }
                    else
                    {
                        //                        LogMessage($"GOT EODCHAR {_nibndx:x2}");
                        return MyWord.Empty;
                    }
                    _MyWordSoFar.AddByte((byte)newchar);
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
            return _MyWordSoFar;
        }

        /// <summary>
        /// given a bunch of letters, find all words in dictionary that contain only those letters (could be dup letters)
        /// e.g. for "admit", returns "madam", "dam", "timid"
        /// </summary>
        /// <param name="inputLetters"></param>
        public IEnumerable<string> FindSubWordsFromLetters(string inputLetters, AnagramType anagramType)
        {
            if (inputLetters.Length > 2)
            {
                LogMessage($"Input {inputLetters}"); //"discountttt"
                                                     // make it no duplicates and disinct and sorted
                inputLetters = new string(inputLetters.Distinct().OrderBy(l => l).ToArray());
                LogMessage($"Sorted {inputLetters}");  //"cdinostu"
                var lastinputLetter = inputLetters[inputLetters.Length - 1];
                SeekWord(inputLetters.Substring(0, 2));
                while (true)
                {
                    var testWord = GetNextWord();
                    if (string.IsNullOrEmpty(testWord))
                    {
                        LogMessage("Reached end of dictionary");
                        break;
                    }
                    if (testWord[0] > lastinputLetter)
                    {
                        LogMessage($"ending because got word beyond last letter {lastinputLetter} {testWord}");
                        break;
                    }
                    if (testWord.Length >= (int)anagramType)
                    {
                        var testWordLetters = new string(testWord.Distinct().OrderBy(l => l).ToArray());
                        var isGood = true;
                        foreach (var let in testWordLetters)
                        {
                            if (!inputLetters.Contains(let))
                            {
                                isGood = false;
                                break;
                            }
                        }
                        if (isGood)
                        {
                            LogMessage($"got word {testWord} {testWordLetters}");
                            yield return testWord;
                        }
                    }
                }
            }
        }

        public enum AnagramType
        {
            /// <summary>
            /// resulting words must be same length as original word
            /// </summary>
            WholeWord,
            /// <summary>
            /// Resulting words have length > 3, 4, 5, etc. and are <= original word length 
            /// </summary>
            SubWord2 = 2,
            SubWord3 = 3,
            SubWord4 = 4,
            SubWord5 = 5,
            SubWord6 = 6,
            SubWord7 = 7,
            SubWord8 = 8,
            SubWord9 = 9,
            SubWord10 = 10,
            SubWord11 = 11,
            SubWord12 = 12,
            SubWord13 = 13,
            SubWord14 = 14,
            SubWord15 = 15,
            SubWord16 = 16,
            SubWord17 = 17,
        }
        public int _nRecursionCnt = 0;

        public List<string> FindAnagrams(string word, AnagramType anagramType)
        {
            var lst = new List<string>();
            FindAnagrams(word, anagramType, (w) =>
             {
                 lst.Add(w);
                 return true;
             });
            return lst;
        }


        public void FindAnagrams(string word, AnagramType anagramType, Func<string, bool> act)
        {
            MyWord myWord = new MyWord(word);
            // sort bytes, with null at the end
            myWord._wordBytes = myWord._wordBytes.OrderBy(b => b == '\0' ? 127 : b).ToArray();
            //for (int i = 0; i < myWord.WordLength; i++)
            //{
            //    for (int j = 0; j < i; j++)
            //    {
            //        if (myWord._wordBytes[i] < myWord._wordBytes[j])
            //        {
            //            var tmp = myWord._wordBytes[i];
            //            myWord._wordBytes[i] = myWord._wordBytes[j];
            //            myWord._wordBytes[j] = tmp;
            //        }
            //    }
            //}
            var lstAnagrams = new HashSet<string>();
            LogMessage($"Do Anagram {myWord}");
            var isAborting = false;
            var lenFromAnagramType = (int)anagramType;
            RecurFindAnagram(0);
            void RecurFindAnagram(int nLevel)
            {
                _nRecursionCnt++;
                if (nLevel < myWord.WordLength)
                {
                    if (nLevel > 1)// tree pruning
                    {
                        var testWord = myWord.GetWord(DesiredLength: nLevel);
                        var partial = SeekWord(testWord, out var compResult);
                        if (!partial.StartsWith(testWord))
                        {
                            //                            LogMessage($"prune {nLevel}  {testWord}  {partial}");
                            return;
                        }

                        if (anagramType != AnagramType.WholeWord)
                        {
                            if (nLevel >= lenFromAnagramType)
                            {
                                if (compResult == 0)
                                {
                                    FoundAnagram(partial);
                                }
                            }
                        }
                    }
                    for (int i = nLevel; i < myWord.WordLength && !isAborting; i++)
                    {
                        byte tmp = myWord._wordBytes[i]; // swap nlevel and i. These will be equal 1st time through for identity permutation
                        myWord._wordBytes[i] = myWord._wordBytes[nLevel];
                        myWord._wordBytes[nLevel] = tmp;
                        RecurFindAnagram(nLevel + 1);
                        // restore swap
                        myWord._wordBytes[nLevel] = myWord._wordBytes[i];
                        myWord._wordBytes[i] = tmp;
                    }
                }
                else
                { // got full permutation
                    var candidate = myWord.GetWord();
                    //                  LogMessage($"Anag Cand {_nRecursionCnt,3} {candidate}");
                    if (IsWord(candidate))
                    {
                        if (lenFromAnagramType == 0 || candidate.Length >= lenFromAnagramType)
                        {
                            FoundAnagram(candidate);
                        }
                    }
                }
                void FoundAnagram(string candidate)
                {
                    if (!lstAnagrams.Contains(candidate)) // duplicate prevention
                    {
                        lstAnagrams.Add(candidate);
                        if (!act(candidate))
                        {
                            isAborting = true;
                        }
                    }
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
                        SeekWord(word, out var cmp);
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
            int sum = 0, i, j, k;
            for (i = 0; i < NumLetters; i++)
            {
                for (j = 0; j < NumLetters; j++)
                {
                    for (k = 0; k < NumLetters; k++)
                    {
                        var cnt = _dictHeader.nibPairPtr[(i * NumLetters + j) * NumLetters + k].cnt;
                        if (sum + cnt < rnum)
                        {
                            sum += cnt;
                        }
                        else
                        {
                            SetDictPos((byte)(i + LetterA), (byte)(j + LetterA), (byte)(k + LetterA));
                            var r = GetNextWord(cntSkip: rnum - sum).GetWord();
                            return r;
                        }
                    }
                }
            }
            throw new InvalidOperationException();
        }

        internal IEnumerable<string> FindMatchRegEx(string strPattern)
        {
            string wrd;
            SeekWord("a");
            while (!string.IsNullOrEmpty(wrd = GetNextWord()))
            {
                if (Regex.IsMatch(wrd, strPattern))
                {
                    yield return wrd;
                }
            }
        }

        //JGLQIN XR QYL DBYXLPLULTQ GE QYL RNTQYLRXR GE YNDBXTQYR DTA CXRBHXQR.  BDIF RDTACHIZ
        public string CryptoGram(string strCryptogram)
        {
            var result = string.Empty;
            LogMessage($"Doing crypt {strCryptogram}");
            var encryptedWords = strCryptogram.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var lstEncryptedWords = new List<MyWord>();
            var encryptedByteDist = new Dictionary<int, int>(); // ltr=0-25, cnt
            var maxWordLen = 0;
            foreach (var wrd in encryptedWords)
            {
                var cleanWrd = new string(wrd.Where(c => char.IsLetter(c)).ToArray());
                if (!string.IsNullOrEmpty(cleanWrd))
                {
                    var mword = new MyWord(cleanWrd.ToLower());
                    lstEncryptedWords.Add(mword);
                    if (mword.WordLength > maxWordLen)
                    {
                        maxWordLen = mword.WordLength;
                    }
                    for (int i = 0; i < mword.WordLength; i++)
                    {
                        var ndx = mword._wordBytes[i] - LetterA;
                        if (encryptedByteDist.ContainsKey(ndx))
                        {
                            encryptedByteDist[ndx]++;
                        }
                        else
                        {
                            encryptedByteDist[ndx] = 1;
                        }
                    }
                }
            }
            lstEncryptedWords = lstEncryptedWords.OrderByDescending(w => w.WordLength).Distinct().ToList();
            lstEncryptedWords.ForEach(s => LogMessage($"Got {s}"));
            foreach (var kvp in encryptedByteDist.OrderByDescending(p => p.Value))
            {
                LogMessage($"ByteDist {kvp.Key,2} {Convert.ToChar(kvp.Key + LetterA)} {kvp.Value}");
            }
            byte[] cryptKey = new byte[26];
            // don't want to use regex: too general and too many conversions from byte to string
            // so we'll use MyWord, and replace unknown letters with a marker Qmark.
            var lstQMarkWords = new List<MyWord>();
            foreach (var encryptedWrd in lstEncryptedWords.Where(m => m.WordLength > 3))
            {
                var m = new MyWord(encryptedWrd.WordLength);
                for (int i = 0; i < encryptedWrd.WordLength; i++)
                {
                    var chr = encryptedWrd._wordBytes[i];
                    m.AddByte(chr);
                }
            }
            return result;
        }

        /// <summary>
        /// Given a word with embedded QMarks, find a match
        /// </summary>
        /// <param name="mword"></param>
        /// <returns></returns>
        internal void FindQMarkMatches(MyWord mword, Func<MyWord, bool> act = null)
        {
            //            var res = string.Empty;
            var ndxFirstrQmark = mword.IndexOf((byte)qmarkChar);
            var wordStop = CalculateStopAtWord(mword, ndxFirstrQmark);
            switch (ndxFirstrQmark)
            {
                case -1: // no qmark
                    SetDictPos(mword);
                    break;
                case 0:
                    SetDictPos(LetterA); // 1st is qmark, so search entire dict
                    break;
                case 1:
                    SetDictPos(mword._wordBytes[0], mword._wordBytes[1]);
                    break;
                case 2:
                    SetDictPos(mword._wordBytes[0], mword._wordBytes[1]);
                    break;
                default:
                    SetDictPos(mword._wordBytes[0], mword._wordBytes[1], mword._wordBytes[2]);
                    break;
            }
            var done = false;
            while (!done)
            {
                var tryWord = GetNextWord(WordStop: wordStop);
                if (tryWord == null)
                {
                    break;
                }
                if (tryWord.WordLength == mword.WordLength)
                {
                    var isMatch = true;
                    for (int i = 0; i < tryWord.WordLength; i++)
                    {
                        if (mword._wordBytes[i] != qmarkChar)
                        {
                            if (mword._wordBytes[i] != tryWord._wordBytes[i])
                            {
                                isMatch = false;
                                break;
                            }
                        }
                    }
                    if (isMatch)
                    {
                        if (act != null)
                        {
                            if (!act(tryWord))
                            {
                                done = true;
                            }
                        }
                    }
                }
                switch (ndxFirstrQmark)
                {
                    case -1: // noqmark
                        break;
                    case 0:
                        break;
                    default:
                        var cmpResult = mword.GetWord(DesiredLength: ndxFirstrQmark).CompareTo(tryWord.GetWord());
                        if (cmpResult < 0)
                        {
                            done = true;
                        }
                        break;
                }
            }
        }
        MyWord CalculateStopAtWord(MyWord mword, int LenToUse)
        {
            MyWord result = null;
            switch (LenToUse)
            {
                case 0:
                    break;
                case 1:
                    if (mword._wordBytes[0] != 'z')
                    {
                        result = new MyWord(1);
                        result._wordBytes[0] = (byte)(mword._wordBytes[0] + 1);
                    }
                    break;
                default:
                    result = new MyWord(2);
                    if (mword._wordBytes[1] == 'z')
                    {
                        if (mword._wordBytes[0] != 'z')
                        {
                            result._wordBytes[0] = (byte)(mword._wordBytes[1] + 1);
                            result._wordBytes[1] = LetterA;
                        }
                    }
                    else
                    {
                        result._wordBytes[0] = mword._wordBytes[0];
                        result._wordBytes[1] = (byte)(mword._wordBytes[1] + 1);
                    }
                    break;

            }
            return result;
        }
    }



    /// <summary>
    /// represent a word as a byte array, reducing need to convert to char/string for perf. Not null terminated
    /// </summary>
    [DebuggerDisplay("{GetWord()}")]
    internal class MyWord : IComparable
    {
        public static MyWord Empty;
        readonly int maxWordLen = 30;
        internal byte[] _wordBytes;
        int _currentLength;

        MyWord()
        {
            this._wordBytes = new byte[maxWordLen];
            _currentLength = 0;
        }
        public MyWord(int maxWordLen) : this()
        {
            this.maxWordLen = maxWordLen;
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
        public void SetWord(byte byte0, byte byte1, byte byte2)
        {
            _currentLength = 3;
            _wordBytes[0] = byte0;
            _wordBytes[1] = byte1;
            _wordBytes[2] = byte2;
            //if (byte2== DictionaryLib.LetterA)
            //{
            //    _currentLength = 2;
            //}
        }
        /// <summary>
        /// Get the length of the word. If DesiredLength is non-zero, return the min(DesiredLength, CurrentLength)
        /// </summary>
        /// <param name="DesiredLength"></param>
        /// <returns></returns>
        public string GetWord(int DesiredLength = 0)
        {
            var len = _currentLength;
            if (DesiredLength != 0)
            {
                len = Math.Min(DesiredLength, len);
            }
            return Encoding.ASCII.GetString(_wordBytes, 0, len);
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
        public int IndexOf(byte b)
        {
            var res = -1;
            for (int i = 0; i < WordLength; i++)
            {
                if (_wordBytes[i] == b)
                {
                    res = i;
                    break;
                }
            }
            return res;
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
