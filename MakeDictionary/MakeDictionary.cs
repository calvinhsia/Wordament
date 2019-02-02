using DictionaryData;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MakeDictionary
{

    /// <summary>
    /// Convert from the old STA COM based C++ dictionary to a C# based
    ///   
    /// </summary>
    public class MakeDictionary
    {
        internal static Action<string> logMessageAction;
        internal static void LogMessage(string msg)
        {
            logMessageAction?.Invoke(msg);
        }
        public static void MakeBinFile(IEnumerable<string> words, string fileName, uint dictNum)
        {
            // output: "C:\Users\calvinh\Source\Repos\Wordament\WordamentTests\bin\Debug\dict1.bin"
            // xfer "C:\Users\calvinh\Source\Repos\Wordament\Dictionary\Resources\dict1.bin"
            // XCOPY /dy C:\Users\calvinh\Source\Repos\Wordament\WordamentTests\bin\Debug\*.bin C:\Users\calvinh\Source\Repos\Wordament\Dictionary\Resources
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }
            DictHeader dictHeader = new DictHeader
            {
                tab1 = " acdegilmnorstu", // A 0 in the nib stream indicates end of word, so the 0th entry isn't used: 15 is escape to next table
                tab2 = " bfhjkpqvwxzy", // 0th entry isn't used
            };
            dictHeader.nibPairPtr = new DictHeaderNibbleEntry[26 * 26];
            var nibpairNdx = 0;
            var let1 = 'a';
            var let2 = 'a';
            var wordSofar = string.Empty;
            var curNibNdx = 0;
            var nibsAdded = new List<byte>();
            using (var fs = File.Open(fileName, FileMode.CreateNew))
            {
                var havePartialByte = false;
                byte partialByte = 0;
                void AddNib(byte nib)
                {
                    nibsAdded.Add(nib);
                    Debug.Assert(nib < 16);
                    //                    LogMessage($"   Adding nib {curNibNdx} {nib}");
                    curNibNdx++;
                    if (!havePartialByte)
                    {
                        partialByte = (byte)(nib << 4);
                    }
                    else
                    {
                        partialByte += nib;
                        fs.WriteByte(partialByte);
                    }
                    havePartialByte = !havePartialByte;
                }
                void addChar(char chr)
                {
                    var ndx = 0;
                    if (chr != 0)
                    {
                        ndx = dictHeader.tab1.IndexOf(chr);
                    }
                    if (ndx >= 0)
                    {
                        AddNib((byte)ndx);
                    }
                    else
                    {
                        if (chr == 0) // EOW
                        {
                            ndx = 0;
                        }
                        else
                        {
                            ndx = dictHeader.tab2.IndexOf(chr);
                        }
                        Debug.Assert(ndx >= 0 && ndx < 15);
                        AddNib(DictHeader.escapeChar);
                        AddNib((byte)ndx);
                    }
                }
                fs.Seek(Marshal.SizeOf(dictHeader), SeekOrigin.Begin);
                //var str = "this is a test";
                //fs.Write(System.Text.ASCIIEncoding.ASCII.GetBytes(str), 0, str.Length);
                var maxWordLen = 0;
                foreach (var word in words)
                {
                    dictHeader.wordCount++;
                    if (word.Length > maxWordLen)
                    {
                        maxWordLen = word.Length;
                    }
                    var curWordNdx = curNibNdx;
                    if (word[0] == let1 && (word.Length < 2 || word[1] == let2))
                    {
                        // same bucket
                    }
                    else
                    { // diff bucket
                        let1 = word[0];
                        let2 = word.Length > 1 ? word[1] : 'a';
                        nibpairNdx = (let1 - 97) * 26 + let2 - 97;
                        dictHeader.nibPairPtr[nibpairNdx].nibbleOffset = curNibNdx;
                        LogMessage($"AddBucket  {let1} {let2} {word} {curNibNdx:x4}");
                    }
                    dictHeader.nibPairPtr[nibpairNdx].cnt++;
                    // each word starts with the length to keep from the prior word. e.g. from "common" to "computer", the 1st 3 letters are the same, so lenToKeep = 3
                    // All the encoded data after the header 
                    // encoding an int length 
                    // So the length consists of a nib indicating how many chars to keep from prior word. If that nibPrior == 15, then the keep = nibPrior + the next nib
                    var nkeepSoFar = 0;
                    for (int i = 0; i < wordSofar.Length; i++)
                    {
                        if (i < word.Length && wordSofar[i] == word[i])
                        {
                            nkeepSoFar++;
                        }
                        else
                        {
                            break;
                        }
                    }
                    var tempKeepSoFar = nkeepSoFar;
                    while (tempKeepSoFar >= 15)
                    {
                        AddNib(15);
                        LogMessage($"Long word {word}");
                        tempKeepSoFar -= 15;
                    }
                    AddNib((byte)tempKeepSoFar);

                    wordSofar = word;
                    for (int i = nkeepSoFar; i < word.Length; i++)
                    {
                        addChar(word[i]);
                    }
                    addChar((char)0);// indicate EndOfWord

                    var strNibsAdded = string.Empty;
                    foreach (var nib in nibsAdded)
                    {
                        strNibsAdded += $" {nib:x}";
                    }
                    LogMessage($"Adding word {dictNum} {dictHeader.wordCount,6} {curWordNdx,5:x4} {nkeepSoFar,3:n0} {word.Length,3} {word} {strNibsAdded}");
                    nibsAdded.Clear();

                }
                if (havePartialByte)
                {
                    AddNib(0); // write out last nibble
                }
                dictHeader.maxWordLen = maxWordLen;
                var bytesHeader = dictHeader.GetBytes();
                fs.Seek(0, SeekOrigin.Begin);
                fs.Write(dictHeader.GetBytes(), 0, bytesHeader.Length);
            }
        }

        public static byte[] DumpDict(string fileName)
        {
            var dictBytes = File.ReadAllBytes(fileName);
            var dictHeader = DictHeader.MakeHeaderFromBytes(dictBytes);
            LogMessage($"{fileName} dump HdrSize= {Marshal.SizeOf(dictHeader)}  (0x{Marshal.SizeOf(dictHeader):x8}) MaxWordLen = {dictHeader.maxWordLen}");
            LogMessage($"Entire dump len= {dictBytes.Length}");

            var cntwrds = 0;
            for (int i = 0; i < 26; i++)
            {
                for (int j = 0; j < 26; j++)
                {
                    cntwrds += dictHeader.nibPairPtr[i * 26 + j].cnt;
                    LogMessage($"{Convert.ToChar(i + 65)} {Convert.ToChar(j + 65)} {dictHeader.nibPairPtr[i * 26 + j].nibbleOffset:x4}  {dictHeader.nibPairPtr[i * 26 + j].cnt}");
                }
            }
            LogMessage($"TotWrds = {dictHeader.wordCount}   TotNibtblcnt={cntwrds}");
            return dictBytes;
        }
    }

    public class OldDictWrapper : IDisposable
    {
        public Dictionary.CDict _dict;
        public OldDictWrapper(uint dictNum)
        {
            _dict = new Dictionary.CDict()
            {
                DictNum = dictNum
            };

        }

        public IEnumerable<string> FindAnagrams(string word)
        {
            _dict.FindAnagram(word, nSubWords: 0);
            foreach (var wrd in _dict.Words)
            {
                yield return wrd as string;
            }
        }
        public IEnumerable<string> GetWords(string start)
        {
            var wrd1 = _dict.FindMatch(start);
            yield return wrd1;
            while (true)
            {
                var wrd = _dict.NextMatch();
                if (string.IsNullOrEmpty(wrd))
                {
                    break;
                }
                yield return wrd;
            }
        }
        public string RandWord(int seed)
        {
            return _dict.RandWord(seed);
        }
        public bool IsWord(string word)
        {
            return _dict.IsWord(word);
        }

        public void Dispose()
        {
            _dict = null;
        }
    }
}
