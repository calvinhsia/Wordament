﻿using DictionaryData;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Dictionary
{
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
        internal Dictionary dictionary;
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
            //                Console.WriteLine($"  GetNextNib {nibndx} {result}");
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
    public class Dictionary
    {
        internal DictHeader _dictHeader;
        internal int _dictHeaderSize;
        internal DictionaryType _dictionaryType;
        internal byte[] _dictBytes;
        internal Random _random;

        byte _partialNib = 0;
        bool _havePartialNib = false;
        int _nibndx;

        string _wordSoFar = string.Empty;
        readonly MyWord _MyWordSoFar;
        StringBuilder _sbuilder = new StringBuilder();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dictType"></param>
        /// <param name="randSeed">0 means random seed TickCount. >0 means use as seed </param>
        public Dictionary(DictionaryType dictType, int randSeed = 0)
        {
            this._dictionaryType = dictType;
            if (dictType == DictionaryType.Large)
            {
                _dictBytes = Properties.Resources.dict1;
            }
            else
            {
                _dictBytes = Properties.Resources.dict2;
            }
            if (randSeed == 0)
            {
                randSeed = Environment.TickCount;
            }
            _random = new Random(randSeed);
            _dictHeader = DictionaryData.DictHeader.MakeHeaderFromBytes(_dictBytes);
            _dictHeaderSize = Marshal.SizeOf<DictHeader>();
            _MyWordSoFar = new MyWord(_dictHeader.maxWordLen);
        }
        public string FindMatch(string strMatch)
        {
            var result = string.Empty;
            strMatch = strMatch.ToLower();
            if (!string.IsNullOrEmpty(strMatch))
            {
                if (!strMatch.Contains("*"))
                {
                    if (IsWord(strMatch))
                    {
                        result = strMatch;
                    }
                }
                else
                {
                    if (strMatch[0] == '*')
                    {
                        SetDictPosition("a");
                        result = GetNextWord();
                    }
                    else
                    {
                        if (strMatch[1] == '*')
                        {
                            SetDictPosition(strMatch.Substring(0, 1));
                            result = GetNextWord();
                        }
                        else
                        {
                            var ndx = strMatch.IndexOf('*');
                            strMatch = strMatch.Substring(0, ndx);
                            SetDictPosition(strMatch);
                            while (true)
                            {
                                var tempResult = GetNextWord();
                                if (tempResult.Length > ndx && tempResult.Substring(0, ndx) == strMatch)
                                {
                                    result = tempResult;
                                    break;
                                }
                                if (tempResult.CompareTo(strMatch) > 0)
                                {
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            return result;
        }

        public DictionaryResult FindMatch2(string strMatch)
        {
            DictionaryResult dictionaryResult = null;
            if (strMatch == "*")
            {
                var dictState = new DictionaryState()
                {
                    dictionary = this,
                    havePartialNib = false,
                    nibndx = 0
                };
                dictionaryResult = new DictionaryResult()
                {
                    dictionaryState = dictState
                };

                //                lstResults = ReadDict();
            }
            return dictionaryResult;
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
                        var testWord = SetDictPosition(word);
                        var cmp = testWord.CompareTo(word);
                        if (cmp == 0)
                        {
                            isWord = true;
                        }
                        break;
                }
            }
            return isWord;
        }
        string SetDictPosition(string word)
        {
            var result = string.Empty;
            if (!string.IsNullOrEmpty(word))
            {
                var let1 = word[0] - 97;
                var let2 = 0; //'a'
                if (word.Length > 1)
                {
                    let2 = word[1] - 97;
                }
                SetDictPosition(let1, let2);
                result = GetNextWord(word);
            }
            return result;
        }
        void SetDictPosition(int let1, int let2 = 0)
        {
            _havePartialNib = false;
            _nibndx = _dictHeader.nibPairPtr[let1 * 26 + let2].nibbleOffset;
            _wordSoFar = new string(new[] { Convert.ToChar(let1 + 97), Convert.ToChar(let2 + 97) });

            _MyWordSoFar.SetWord(_wordSoFar);
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
            //                Console.WriteLine($"  GetNextNib {nibndx} {result}");
            return result;
        }

        public string GetNextWordold()
        {
            byte nib = 0;
            var lenSoFar = 0;
            while ((nib = GetNextNib()) == 0xf)
            {
                lenSoFar += nib;
            }
            if (nib == DictHeader.EOFChar)
            {
                //              Console.WriteLine($"Got EOD {_nibndx}");
                return string.Empty;
            }
            lenSoFar += nib;
            if (lenSoFar < _wordSoFar.Length)
            {
                _wordSoFar = _wordSoFar.Substring(0, lenSoFar);
            }
            _sbuilder.Clear();
            _sbuilder.Append(_wordSoFar);
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
                        //                        Console.WriteLine($"GOT EODCHAR {_nibndx:x2}");
                        break;
                    }
                    newchar = _dictHeader.tab1[nib];
                }
                _sbuilder.Append(newchar);
            }
            if (nib == DictHeader.EOFChar)
            {
                return string.Empty;
            }
            _wordSoFar = _sbuilder.ToString();
            return _wordSoFar;
        }
        public string GetNextWord()
        {
            return GetNextWord(WordStop: null);
        }

        /// <summary>
        /// must init dic first SetDictPosition.
        /// </summary>
        /// <param name="WordStop">must init dict first. then will stop when dict word >= WordStop</param>
        /// <param name="cntSkip"> nonzero means skip this many words (used for RandWord). 
        /// 0 means return next word. 1 means skip one word. For perf: don't have to convert to string over and over</param>
        /// <returns></returns>
        internal string GetNextWord(string WordStop = null, int cntSkip = 0)
        {
            Debug.Assert((WordStop == null) || cntSkip == 0);
            byte nib = 0;
            MyWord stopAtWord = null;
            if (!string.IsNullOrEmpty(WordStop))
            {
                stopAtWord = new MyWord(WordStop);
            }
            var done = false;
            while (!done)
            {
                var lenSoFar = 0;
                while ((nib = GetNextNib()) == 0xf)
                {
                    lenSoFar += nib;
                }
                if (nib == DictHeader.EOFChar)
                {
                    //              Console.WriteLine($"Got EOD {_nibndx}");
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
                            //                        Console.WriteLine($"GOT EODCHAR {_nibndx:x2}");
                            break;
                        }
                        newchar = _dictHeader.tab1[nib];
                    }
                    _MyWordSoFar.AddByte(Convert.ToByte(newchar));
                }
                if (nib == DictHeader.EOFChar)
                {
                    return string.Empty;
                }
                if (stopAtWord == null)
                {
                    break;
                }
                var cmp = _MyWordSoFar.CompareTo(stopAtWord);
                if (cmp >= 0)
                {
                    break;
                }
            }
            return _MyWordSoFar.GetWord();
        }
        public string RandomWord()
        {
            var rnum = _random.Next(_dictHeader.wordCount);
            var fGotIt = false;
            int sum = 0, i = 0, j = 0;
            for (i = 0; i < 26 && !fGotIt; i++)
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
                        fGotIt = true;
                        SetDictPosition(i, j);
                        while (sum++ < rnum)
                        {
                            GetNextWord();
                        }
                        return GetNextWord();
                    }
                }
            }
            throw new InvalidOperationException();
        }

        public DictionaryResult GetNextWord2(int nibOffset)
        {
            DictionaryResult dictionaryResult = new DictionaryResult();
            return dictionaryResult;
        }

        public DictionaryResult RandomWord2()
        {
            var res = new DictionaryResult()
            {
                Word = "rand"
            };

            return res;
        }
        public List<string> ReadDict()
        {
            var lstWords = new List<string>();

            while (true)
            {
                var word = GetNextWord();
                if (string.IsNullOrEmpty(word))
                {
                    break;
                }
                lstWords.Add(_wordSoFar);
                //                Console.WriteLine($"Got Word  {lstWords.Count,6} {_nibndx:x0} {lenSoFar,2}  {_wordSoFar.Length,3} {_wordSoFar}");
            }
            return lstWords;
        }
    }

    internal class MyWord : IComparable
    {
        readonly private int maxWordLen;
        readonly byte[] _wordBytes;
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
                _wordBytes[ndx] = Convert.ToByte(word[ndx]);
            }
        }
        public string GetWord()
        {
            var xx = new ASCIIEncoding();
            xx.GetString(_wordBytes);
            return Encoding.ASCII.GetString(_wordBytes, 0, _currentLength);
        }
        public void AddByte(byte b)
        {
            _wordBytes[_currentLength++] = b;
        }
        public int WordLength { get { return _currentLength; } }
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
