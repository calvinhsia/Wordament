using DictionaryData;
using System;
using System.Collections.Generic;
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
                var ndx = Marshal.SizeOf(dictionary._dictHeader) + nibndx / 2;
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
        internal DictionaryType _dictionaryType;
        internal byte[] _dictBytes;
        internal Random _random;
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
        }
        public List<string> FindMatch(string strMatch)
        {
            List<string> lstResults = null;
            if (strMatch == "*")
            {
                lstResults = ReadDict();
            }
            return lstResults;
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
                        SetDictPosition(word);
                        while (true)
                        {
                            var testWord = GetNextWord();
                            var cmp = testWord.CompareTo(word);
                            if (cmp == 0)
                            {
                                isWord = true;
                                break;
                            }
                            else if (cmp > 0)
                            {
                                break;
                            }
                        }
                        break;
                }
            }
            return isWord;
        }
        void SetDictPosition(string word)
        {
            if (!string.IsNullOrEmpty(word))
            {
                var let1 = word[0] - 97;
                var let2 = 0; //'a'
                if (word.Length > 1)
                {
                    let2 = word[1] - 97;
                }
                SetDictPosition(let1, let2);
            }
        }
        void SetDictPosition(int let1, int let2)
        {
            _havePartialNib = false;
            _nibndx = _dictHeader.nibPairPtr[let1 * 26 + let2].nibbleOffset;
            _wordSoFar = new string(new[] { Convert.ToChar(let1 + 97), Convert.ToChar(let2 + 97) });
            if ((int)(_nibndx & 1) > 0)
            {
                GetNextNib();
            }
        }
        public string GetNextWord()
        {
            byte nib = 0;
            var lenSoFar = 0;
            while ((nib = GetNextNib()) == 0xf)
            {
                lenSoFar += nib;
            }
            if (nib == DictHeader.EOFChar)
            {
                Console.WriteLine($"Got EOD {_nibndx}");
                return string.Empty;
            }
            lenSoFar += nib;
            if (lenSoFar < _wordSoFar.Length)
            {
                _wordSoFar = _wordSoFar.Substring(0, lenSoFar);
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
                        Console.WriteLine($"GOT EODCHAR {_nibndx:x2}");
                        break;
                    }
                    newchar = _dictHeader.tab1[nib];
                }
                _wordSoFar += newchar;
            }
            if (nib == DictHeader.EOFChar)
            {
                return string.Empty;
            }
            return _wordSoFar;
        }
        public string RandomWord()
        {

            /*
	nRand = (int)(m_nDictionaryTotalWordCount * (((double)rand()) / RAND_MAX));
	int *WordCounts = (int *)(m_DictBase + (26*26+1)*4);

	for (i =fGotit=ndx= 0 ; i < 26 ; i++) {
		for (j = 0 ; j<26 ; j++) {
			if (nCnt + WordCounts[ndx] < nRand) {
				nCnt+= WordCounts[ndx];
			} else {
				fGotit=1;
				break;
			}
			ndx++;
		}
		if (fGotit)
			break;
	}
	StartDict(97+i,97+j);
	while (nCnt < nRand) {
		nRand--;
		GetNextWord();
	}
             * */
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

        byte _partialNib = 0;
        bool _havePartialNib = false;
        int _nibndx;
        string _wordSoFar = string.Empty;
        byte GetNextNib()
        {
            byte result;
            if (_havePartialNib)
            {
                result = _partialNib;
            }
            else
            {
                var ndx = Marshal.SizeOf(_dictHeader) + _nibndx / 2;
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


}
