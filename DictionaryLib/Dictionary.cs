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
    {            // 0x41 - 0x5a == A-Z   0x61-0x7a == a-z

        public const byte Lettera = 97; // 'a'
        public const byte Letterz = Lettera + 26 - 1; // 'z'
        public const byte LetterA = 65; // 'A'
        public const byte LetterZ = LetterA + 26 - 1; // 'Z'
        public const byte ToLowerDiff = Lettera - LetterA; // 32: add this to convert from upper case to lower case

        public const int NumLetters = 26; // # letters in alphabet
        public const int MaxWordLen = 30; // longest word in any dictionaryy
        public const byte qmarkChar = (byte)'_';// + 1 - LetterA;

        internal DictHeader _dictHeader;
        internal int _dictHeaderSize;
        internal DictionaryType _dictionaryType;
        internal byte[] _dictBytes;
        internal Random _random;

        internal byte _partialNib = 0;
        internal bool _havePartialNib = false;
        internal int _nibndx;
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
            _MyWordSoFar = new MyWord();
        }

        public static byte ToLowerByte(byte b)
        {
            var ret = b;
            if (b < LetterZ)
            {
                b += ToLowerDiff;
            }
            if ((b < Lettera || b > Letterz) && b != qmarkChar)
            {
                throw new InvalidOperationException($"non alphabetic input");
            }
            return b;
        }

        public string SeekWord(string word)
        {
            return SeekWord(word, out var _);
        }
        public string SeekWord(string testWord, out int compResult)
        {
            var myTestWord = new MyWord(testWord);
            var node = SeekWord(myTestWord, out compResult);
            if (node != null)
            {
                return node.GetWord();
            }
            return null;
        }
        /// <summary>
        /// Seek in dictionary to provided "word". (Case insensitive)
        /// if found in dict, returns the same string
        /// if not found, returns the word just beyond where the word would be found.
        /// IOW, 2 consecutive words in dictionary: abcdefone, abcdeftwo (and dict does not have abc,abcdefg)
        /// Search for 
        ///     abc => returns abcdef (abc not found)
        ///     abcdefone=> returns abcdefone (match)
        ///     abcdefg=> returns abcdefone
        ///     abcdefs=> returns abcdeftwo
        /// If we're at the end of the dictionary, return string.empty
        ///// </summary>
        /// </summary>
        public MyWord SeekWord(MyWord word, out int compResult)
        {
            byte let0 = Lettera;
            byte let1 = Lettera;
            byte let2 = Lettera;
            if (word.WordLength > 0)
            {
                let0 = ToLowerByte((byte)(word[0]));
            }
            if (word.WordLength > 1)
            {
                let1 = ToLowerByte((byte)(word[1]));
            }
            if (word.WordLength > 2)
            {
                let2 = ToLowerByte((byte)(word[2]));
            }
            SetDictPos(let0, let1, let2);
            if (word.WordLength < 3)
            {
                _MyWordSoFar.SetLength(word.WordLength);
            }
            if (word.WordLength == 0) // the first word in dictionary is "a"
            {
                compResult = 0;
                return new MyWord("a");
            }
            if (word.WordLength == 1 && word[0] == 'i') // the word "i"
            {
                compResult = 0;
                return new MyWord("i");
            }
            var wordStop = new MyWord(word);
            if (_nibndx == 0 && let0 > 97) // if the nibndx shows 0 but we're past the "A"'s, then we're at the end of the dictionary
            {
                compResult = -1;
                return null;
            }
            var result = GetNextWord(out compResult, wordStop);
            return result;
        }

        internal void SetDictPos(byte let0, byte let1 = Lettera, byte let2 = Lettera)
        {
            _havePartialNib = false;
            var n = ((let0 - Lettera) * NumLetters + let1 - Lettera) * NumLetters + let2 - Lettera;
            _nibndx = _dictHeader.nibPairPtr[n].nibbleOffset;
            _GetNextWordCount = 0;
            _MyWordSoFar.SetWord(let0, let1, let2);
            if ((int)(_nibndx & 1) > 0)
            {
                GetNextNib();
            }
        }

        internal void SetDictPos(MyWord mword)
        {
            byte let0;
            byte let1 = Lettera;
            byte let2 = Lettera;
            switch (mword.WordLength)
            {
                case 0:
                    throw new InvalidOperationException("word length 0?");
                default:
                    let0 = mword[0];
                    if (mword.WordLength > 1)
                    {
                        let1 = mword[1];
                    }
                    if (mword.WordLength > 2)
                    {
                        let2 = mword[2];
                    }
                    break;
            }
            SetDictPos(let0, let1, let2);
        }

        internal byte GetNextNib()
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
            if (_nibndx > _dictBytes.Length * 2)
            {
                return 0;
            }
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
            compareResult = 0;
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
                    if (lenSoFar == 0 && // we could be transitioning to a new first letter
                        _GetNextWordCount > 1)  // if we're not on "ha" at the beginning of a triplet section
                    {
                        if (_MyWordSoFar[0] == (byte)'h') // from "h" ?
                        {
                            _MyWordSoFar.SetWord("i"); // the word "I"
                            compareResult = 1;
                            return _MyWordSoFar;
                        }
                    }
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
                if (WordStop != null)
                {
                    var cmp = _MyWordSoFar.CompareTo(WordStop);
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
            return _MyWordSoFar ?? MyWord.Empty;
        }
        public List<string> GenerateSubWords(string InitialWord, int MinLength = 3, bool LeftToRight = true, int MaxSubWords = int.MaxValue)
        {
            return GenerateSubWords(InitialWord, out var _, MinLength, LeftToRight, MaxSubWords);
        }

        /// <summary>
        /// Get a sorted list of all words that can be made from permuting the InitialWord ( >=MinLength)
        /// </summary>
        public List<string> GenerateSubWords(string InitialWord, out int numLookups, int MinLength = 3, bool LeftToRight = true, int MaxSubWords = int.MaxValue)
        {
            var numlookups = 0;
            var hashSetSubWords = new SortedSet<string>();
            //*
            var testWord = new MyWord();
            var rejectsCached = new HashSet<MyWord>();
            PermuteString(InitialWord, LeftToRight, act: null, actMyWord: (str) =>
            {
                for (int i = MinLength; i <= str.WordLength; i++)
                {
                    testWord.SetLength(i);
                    //                    Array.Copy(sourceArray: str._wordBytes, destinationArray: testWord._wordBytes, length: i);
                    for (int j = 0; j < i; j++)
                    {
                        testWord[j] = str[j];
                    }
                    if (rejectsCached.Contains(testWord))
                    {
                        break;
                    }
                    var partial = SeekWord(testWord, out var compResult);
                    numlookups++;
                    if (compResult == 0)
                    {
                        hashSetSubWords.Add(testWord.GetWord());
                    }
                    else
                    {
                        if (!partial.StartsWith(testWord)) // if "ids" isn't a word and the closest word is "idyllic" which doesn't start with "ids" then there's no point trying words longer than "ids" that start with "ids"
                        {
                            rejectsCached.Add(testWord);
                            break;
                        }
                        // "sci" is not a word, but the closest "science" starts with "sci", then continue
                    }
                }
                return hashSetSubWords.Count < MaxSubWords; // continue 
            });
            /*/
            PermuteString(InitialWord, LeftToRight, act: (str) =>
            {
                for (int i = MinLength; i <= str.Length; i++)
                {
                    var testWord = str.Substring(0, i);
                    var partial = SeekWord(testWord, out var compResult);
                    numlookups++;
                    if (compResult == 0)
                    {
                        hashSetSubWords.Add(testWord);
                    }
                    else
                    {
                        if (!partial.StartsWith(testWord)) // if "ids" isn't a word and the closest word is "idyllic" which doesn't start with "ids" then there's no point trying words longer than "ids" that start with "ids"
                        {
                            break;
                        }
                        // "sci" is not a word, but the closest "science" starts with "sci", then continue
                    }
                }
                return hashSetSubWords.Count < MaxSubWords; // continue 
            });
             //*/
            numLookups = numlookups;
            return hashSetSubWords.ToList();
        }
        /// <summary>
        /// All the dictionary entries in order. A binary search can be made.
        /// </summary>
        /// <returns></returns>
        public List<string> GetAllWords()
        {
            var res = new List<string>();
            SeekWord(string.Empty);
            while (true)
            {
                var word = GetNextWord();
                if (string.IsNullOrEmpty(word))
                {
                    break;
                }
                res.Add(word);
            }
            return res;
        }

        /// <summary>
        /// Given a bunch of letters, find all words in dictionary that contain only those letters (could be dup letters, so not an anagram)
        /// e.g. for "admit", returns "madam", "dam", "timid"
        /// e.g. for "
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
            myWord.SortBytes();
            //for (int i = 0; i < myWord.WordLength; i++)
            //{
            //    for (int j = 0; j < i; j++)
            //    {
            //        if (myWord[i] < myWord[j])
            //        {
            //            var tmp = myWord[i];
            //            myWord[i] = myWord[j];
            //            myWord[j] = tmp;
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
                        byte tmp = myWord[i]; // swap nlevel and i. These will be equal 1st time through for identity permutation
                        myWord[i] = myWord[nLevel];
                        myWord[nLevel] = tmp;
                        RecurFindAnagram(nLevel + 1);
                        // restore swap
                        myWord[nLevel] = myWord[i];
                        myWord[i] = tmp;
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
        /// <summary>
        /// Determine if word (case insensitive) is in dictionary. 
        /// </summary>
        public bool IsWord(string word)
        {
            bool isWord = false;
            if (!string.IsNullOrEmpty(word))
            {
                switch (word.Length)
                {
                    case 1:
                        if (word == "a" || word == "i" || word == "A" || word == "I")
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
        /// <summary>
        /// Gets a random word: will be lower case.
        /// </summary>
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
                            SetDictPos((byte)(i + Lettera), (byte)(j + Lettera), (byte)(k + Lettera));
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
        /// <summary>
        /// 
        /// </summary>
        /// <param name="strCryptogram">Upper Case</param>
        /// <returns></returns>
        public string CryptoGram(string strCryptogram)
        {
            var result = string.Empty;
            LogMessage($"Doing crypt {strCryptogram}");
            var encryptedWords = strCryptogram.Split(new[] { ' ', '\'' }, StringSplitOptions.RemoveEmptyEntries);
            var lstEncryptedWords = new List<MyWord>();
            var encryptedByteDist = new Dictionary<byte, int>(); // ltr=0-25, cnt
            var maxWordLen = 0;
            foreach (var wrd in encryptedWords) //include dupe crypt words for freq distribution calc
            {
                var cleanWrd = new string(wrd.Where(c => char.IsLetter(c)).ToArray()); // remove punctuation
                if (!string.IsNullOrEmpty(cleanWrd))
                {
                    var mword = new MyWord(cleanWrd.ToLower());
                    lstEncryptedWords.Add(mword);
                    if (mword.WordLength > maxWordLen)
                    {
                        maxWordLen = mword.WordLength;
                    }
                    for (byte i = 0; i < mword.WordLength; i++)
                    {
                        var ndx = (byte)(mword[i] - Lettera);
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
            lstEncryptedWords = lstEncryptedWords.OrderByDescending(w => w.WordLength).Distinct().ToList(); // exclude dupe crypt words to reduce work

            lstEncryptedWords.ForEach(s => LogMessage($"Got CryptWord {s}"));
            foreach (var kvp in encryptedByteDist.OrderByDescending(p => p.Value))
            {
                LogMessage($"ByteDist {kvp.Key,2} {Convert.ToChar(kvp.Key + Lettera)} {kvp.Value}");
            }
            var lstEncLets = encryptedByteDist
                .OrderByDescending(p => p.Value)
                .Select(p => (char)(p.Key + Lettera))
                .ToArray();

            var strEncLets = new string(lstEncLets); //encrypt "rlqxydtbgineachjpufz" order of freq
            var strLtrfreq = "etaonscdgilmrubfhjkpqvwxzy"; // guesstimate " acdegilmnorstu"," bfhjkpqvwxzy", 
            var cipher = new MyWord(); //init with 26 bytes
            cipher.SetLength(26);
            // alternative: look at e.g. 2 letter words, try "to","in", etc.
            int nTimes = 0;
            var GotAnswer = false;
            var tryWord = new MyWord();
            // we'll permute the string of freq letters (etaons, teaons, etc) from LeftToRight
            PermuteString(strLtrfreq, LeftToRight: true, act: (strLtrFreqPerm) =>
               {
                   //                   LogMessage($"Trying Perm #{nTimes,4} {strLtrFreqPerm}");

                   //                   for (int i = 0; i < strEncLets.Length; i++)
                   //                   {
                   //                       var ndxCipher = strEncLets[i] - LetterA;
                   //                       cipher[ndxCipher] = (byte)strLtrFreqPerm[i];
                   //                   }
                   //                   int numWordsWithMatches = TryCipher();
                   //                   if (numWordsWithMatches > .8 * lstEncryptedWords.Count) // >80% match rate. Todo adjust score with cryptwordlen
                   //                   {
                   //                       LogMessage($"GotAnswer {ApplyCipherToCryptogram()}");
                   //                       GotAnswer = true;
                   //                   }
                   //                   else
                   //                   {
                   ////                       LogMessage($"Reject cipher {ApplyCipherToCryptogram()}");
                   //                   }


                   var doneThisPermutation = false;
                   var numCipherCharsSuccessful = 0;
                   while (!doneThisPermutation)
                   {
                       // we now know that e.g. "r" is the most common letter in cryptogram, and "e" is a common english letter, so we 
                       // want to try substituing "r" with "e" and see if there are a high number of hits.
                       // for each encrypted letter by most freq to least freq
                       cipher.SetAllBytes(qmarkChar);
                       for (int indxLtrFreq = 0; indxLtrFreq < strEncLets.Length; indxLtrFreq++) // 26 ltrs in alphabet, but <=26 are in cipher
                       {
                           // each time thru loop we guess an additional char in cipher
                           var ndxCipher = strEncLets[indxLtrFreq] - Lettera;
                           var singleLetterGood = false;
                           while (!singleLetterGood && indxLtrFreq < strLtrfreq.Length)
                           {
                               cipher[ndxCipher] = (byte)strLtrFreqPerm[indxLtrFreq];
                               int numWordsWithMatches = TryCipher();
                               if (numWordsWithMatches > .8 * lstEncryptedWords.Count) // >80% match rate. Todo adjust score with cryptwordlen
                               {
                                   singleLetterGood = true;
                                   numCipherCharsSuccessful++;
                                   if (indxLtrFreq == strEncLets.Length - 1)
                                   {
                                       GotAnswer = true;
                                       var str = ApplyCipherToCryptogram();
                                       LogMessage($"GotAnswer {str}");
                                       doneThisPermutation = true;
                                   }
                               }
                               else
                               {
                                   LogMessage($"Rejecting perm # {nTimes} {indxLtrFreq} {ApplyCipherToCryptogram()}");
                                   //                                   LogMessage($"Reject cipher {ApplyCipherToCryptogram()}");
                                   doneThisPermutation = true;
                                   break;
                                   //cipher[ndxCipher] = qmarkChar; // revert the try 
                                   //if (indxLtrFreq == strEncLets.Length - 1)
                                   //{
                                   //}
                               }
                           }
                       }
                   }
                   return GotAnswer ? false : (++nTimes <= 114000320); // abort after too many (7! = 5040, 8!= 40320, 9! = 362880
               });

            //// don't want to use regex: too general and too many conversions from byte to string
            //// so we'll use MyWord, and replace unknown letters with a marker Qmark.
            //var lstQMarkWords = new List<MyWord>();
            //foreach (var encryptedWrd in lstEncryptedWords)
            //{
            //    var m = new MyWord();
            //    for (int i = 0; i < encryptedWrd.WordLength; i++)
            //    {
            //        var chr = encryptedWrd[i];
            //        m.AddByte(chr);
            //    }
            //}
            return result;
            string ApplyCipherToCryptogram()
            {
                var str = string.Empty;
                foreach (var chr in strCryptogram.ToLower())
                {
                    var theEncChar = chr;
                    if (char.IsLetter(theEncChar))
                    {
                        var plaintextChar = cipher[theEncChar - Lettera];
                        str += (char)plaintextChar;
                    }
                    else
                    {
                        str += theEncChar;
                    }
                    //var ndx = strLtrFreqPerm.IndexOf(chr);
                    //if (ndx >= 0)
                    //{
                    //    theEncChar = (char)cipher[ndx];
                    //}
                    //str += theEncChar;
                }
                return str;
            }

            int TryCipher()
            {
                int numWordsWithMatches = 0;
                // now let's try the cipher and see how many successes
                foreach (var encWrd in lstEncryptedWords)
                {
                    tryWord.SetLength(encWrd.WordLength);
                    int nQMarks = 0;
                    for (int i = 0; i < encWrd.WordLength; i++)
                    {
                        var plaintextLtr = cipher[encWrd[i] - Lettera];
                        if (plaintextLtr == qmarkChar)
                        {
                            nQMarks++;
                        }
                        tryWord[i] = plaintextLtr;
                    }
                    if (nQMarks != encWrd.WordLength) // if it's not all qmarks
                    {
                        FindQMarkMatches(tryWord, (mw) =>
                        {
                            numWordsWithMatches++;
                            //                                           LogMessage($"GotQM {encWrd}  {tryWord} {mw}");
                            return false; // we only want the 1st match: then abort looking
                        });
                    }
                    else
                    {
                        numWordsWithMatches++;// if it's all qmarks (e.g. 5 qmarks), then there is a match (there is at least 1 5 letter word in the dict)
                    }
                }
                return numWordsWithMatches;
            }
        }

        /// <summary>
        /// Can produce multiple duplicates if there are duplicate letters in the input
        /// Pass in either act or actMyWord. The latter can be faster by avoiding converting to a string
        /// </summary>
        /// <param name="inputString"></param>
        /// <param name="LeftToRight">the left part of the string changes the fastest</param>
        /// <param name="act">the action to execute on each permutation. Return false to abort</param>
        /// <param name="actMyWord">the action to execute on each permutation (parameter is MyWord). Return false to abort</param>
        public static void PermuteString(string inputString, bool LeftToRight, Func<string, bool> act, Func<MyWord, bool> actMyWord = null)
        {
            var myWord = new MyWord(inputString);
            bool fAbort = false;
            DoPermutation(0);
            void DoPermutation(int nLevel)
            {
                if (fAbort)
                {
                    return;
                }
                if (nLevel < myWord.WordLength)
                {
                    if (!LeftToRight)
                    {
                        for (int i = nLevel; i < myWord.WordLength && !fAbort; i++)
                        {
                            byte tmp = myWord[i]; // swap nlevel and i. These will be equal 1st time through for identity permutation
                            var swapNdx = nLevel;
                            myWord[i] = myWord[swapNdx];
                            myWord[swapNdx] = tmp;
                            DoPermutation(nLevel + 1);
                            // restore swap
                            myWord[swapNdx] = myWord[i];
                            myWord[i] = tmp;
                        }
                    }
                    else
                    {
                        //                        for (int i = nLevel; i < myWord.WordLength; i++)
                        for (int i = myWord.WordLength - 1 - nLevel; i >= 0 && !fAbort; i--)
                        {
                            byte tmp = myWord[i];
                            var swapNdx = myWord.WordLength - 1 - nLevel;
                            myWord[i] = myWord[swapNdx];
                            myWord[swapNdx] = tmp;
                            DoPermutation(nLevel + 1);
                            // restore swap
                            myWord[swapNdx] = myWord[i];
                            myWord[i] = tmp;
                        }
                    }
                }
                else
                {
                    if (act != null)
                    {
                        fAbort = !act(myWord.GetWord());
                    }
                    else
                    {
                        fAbort = !actMyWord(myWord);

                    }
                }
            }
        }


        /// <summary>
        /// Given a word with embedded QMarks, find a match. A QMark is "_" for easy reading
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
                    SetDictPos(Lettera); // 1st is qmark, so search entire dict
                    break;
                case 1:
                    SetDictPos(mword[0]);
                    break;
                case 2:
                    SetDictPos(mword[0], mword[1]);
                    break;
                default:
                    SetDictPos(mword[0], mword[1], mword[2]);
                    break;
            }
            var done = false;
            while (!done)
            {
                var tryWord = GetNextWord(WordStop: null);
                if (tryWord == null || tryWord.WordLength == 0)
                {
                    break;
                }
                if (wordStop != null)
                {
                    if (tryWord.CompareTo(wordStop) > 0)
                    {
                        break;
                    }
                }
                if (tryWord.WordLength == mword.WordLength)
                {
                    var isMatch = true;
                    for (int i = 0; i < tryWord.WordLength; i++)
                    {
                        if (mword[i] != qmarkChar)
                        {
                            if (mword[i] != tryWord[i])
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
                    if (mword[0] != Letterz)
                    {
                        result = new MyWord();
                        result[0] = (byte)(mword[0] + 1);
                        result.SetLength(1);
                    }
                    break;
                case 2:
                    result = new MyWord();
                    if (mword[1] == Letterz)
                    {
                        if (mword[0] == Letterz)
                        {
                            result = null; // both 'z', go to end of dict
                        }
                        else
                        {
                            result[0] = (byte)(mword[1] + 1);
                            result[1] = Lettera;
                        }
                    }
                    else
                    {
                        result[0] = mword[0];
                        result[1] = (byte)(mword[1] + 1);
                    }
                    result[2] = Lettera;
                    result.SetLength(3);
                    break;
                default:
                    result = new MyWord();
                    if (mword[2] == Letterz)
                    {
                        if (mword[1] == Letterz)
                        {
                            result = null; // at least 2 'z', go to end of dict
                        }
                        else
                        {
                            result[0] = (byte)(mword[1] + 1);
                            result[1] = Lettera;
                            result[2] = Lettera;
                        }
                    }
                    else
                    {
                        result[0] = mword[0];
                        result[1] = mword[1];
                        result[2] = (byte)(mword[2] + 1);
                    }
                    result.SetLength(3);
                    break;

            }
            return result;
        }
    }



}
