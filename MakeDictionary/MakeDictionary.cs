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
    //bucket: ptr, cnt into encoded compressed dictionary data
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct DictHeaderNibbleEntry
    {
        // offset of nibble
        // to get offset from dict base, divide by 2. If odd, skip a nibble
        // There are 26*26 of these buckets, for each combination of 1st letter, 2nd letter, like aa, ab, ac...
        // each offset points to a length nibble, which is 1 for the minor transitions bb=>bc, but 0, for major transitions, cz=>da
        // this way, scanning through the data from one bucket to another is seamless.
        public int nibbleOffset;
        // # of entries in this bucket (used for RandWord
        public short cnt;
        public override string ToString()
        {
            return $"{nibbleOffset} {cnt}";
        }
        public override bool Equals(object obj)
        {
            var that = (DictHeaderNibbleEntry)obj;
            if (this.cnt != that.cnt || this.nibbleOffset != that.nibbleOffset)
            {
                return false;
            }
            return base.Equals(obj);
        }
        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }
    }

    // Encoded, compressed dictionary
    // data is a nibble (4 bits) 0-15.
    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    internal struct DictHeader
    {
        public const byte escapeChar = 0xf;
        public const byte EODChar = 0xff;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        public string tab1;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        public string tab2;

        [MarshalAs(UnmanagedType.I4)]
        public int wordCount; //total # of words in dictionary

        /// <summary>
        /// 26*26 array of DictHeaderNibbleEntry 
        /// AA, AB, AC... BA,BB,BC.... the first one points to e.g. "aardvark", next to "abandon", etc.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 26 * 26)]
        public DictHeaderNibbleEntry[] nibPairPtr;
        //[MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.I4, SizeConst =100)]
        //public fixed int data[100];

        public byte[] GetBytes()
        {
            var size = Marshal.SizeOf<DictHeader>();
            var ptr = Marshal.AllocHGlobal(size);
            var arr = new byte[size];
            Marshal.StructureToPtr<DictHeader>(this, ptr, fDeleteOld: false);
            Marshal.Copy(ptr, arr, 0, size);
            Marshal.FreeHGlobal(ptr);
            return arr;
        }

        public static DictHeader MakeHeaderFromBytes(byte[] bytes)
        {
            var size = Marshal.SizeOf<DictHeader>();
            var ptr = Marshal.AllocHGlobal(size);
            Marshal.Copy(bytes, 0, ptr, size);
            var x = Marshal.PtrToStructure<DictHeader>(ptr);
            Marshal.FreeHGlobal(ptr);
            return x;
        }
        // override object.Equals
        public override bool Equals(object obj)
        {
            //       
            // See the full list of guidelines at
            //   http://go.microsoft.com/fwlink/?LinkID=85237  
            // and also the guidance for operator== at
            //   http://go.microsoft.com/fwlink/?LinkId=85238
            //

            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }
            var that = (DictHeader)obj;
            if (this.tab1 != that.tab1)
                return false;
            if (this.wordCount != that.wordCount)
                return false;
            for (int i = 0; i < this.nibPairPtr.Length; i++)
            {
                if (!this.nibPairPtr[i].Equals(that.nibPairPtr[i]))
                    return false;
            }

            return true;
        }

        // override object.GetHashCode
        public override int GetHashCode()
        {
            // TODO: write your implementation of GetHashCode() here
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convert from the old STA COM based C++ dictionary to a C# based
    ///   
    /// </summary>
    public class MakeDictionary
    {
        public static void MakeBinFile(IEnumerable<string> words, string fileName)
        {
            // output: "C:\Users\calvinh\Source\Repos\Wordament\WordamentTests\bin\Debug\dict1.bin"
            // xfer "C:\Users\calvinh\Source\Repos\Wordament\Dictionary\Resources\dict1.bin"
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
            var letndx1 = 97; // from 'a'
            var letndx2 = 97; // from 'a'
            var let1 = Convert.ToChar(letndx1);
            var let2 = Convert.ToChar(letndx2);
            var wordSofar = string.Empty;
            var curNibNdx = 0;
            var priorNibNdx = 0;
            using (var fs = File.Open(fileName, FileMode.CreateNew))
            {
                var havePartialByte = false;
                byte partialByte = 0;
                void AddNib(byte nib)
                {
                    Debug.Assert(nib < 16);
                    //                    Console.WriteLine($"   Adding nib {curNibNdx} {nib}");
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
                foreach (var word in words)
                {
                    dictHeader.wordCount++;
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
                        Console.WriteLine($"Long word {word}");
                        tempKeepSoFar -= 15;
                    }
                    AddNib((byte)tempKeepSoFar);

                    Console.WriteLine($"Adding word {dictHeader.wordCount,6} {nkeepSoFar,3:n0} {word.Length,3} {word}");

                    wordSofar = word;
                    for (int i = nkeepSoFar; i < word.Length; i++)
                    {
                        addChar(word[i]);
                    }
                    addChar((char)0);// indicate EndOfWord

                    if (word[0] == let1 && (word.Length < 2 || word[1] == let2))
                    {
                        // same bucket
                        dictHeader.nibPairPtr[nibpairNdx].cnt++;
                        dictHeader.nibPairPtr[nibpairNdx].nibbleOffset = priorNibNdx;
                    }
                    else
                    { // diff bucket
                      //                    Console.WriteLine($"Add  {let1} {let2} {wordSofar} {curnibbleEntry}");
                        priorNibNdx = curNibNdx;
                        ++nibpairNdx;
                        if (let2 == 'z')
                        {
                            let2 = 'a';
                            letndx2 = Convert.ToInt32('a');
                            let1 = Convert.ToChar(++letndx1);
                        }
                        else
                        {
                            let2 = Convert.ToChar(++letndx2);
                        }
                    }
                }
                if (havePartialByte)
                {
                    AddNib(0); // write out last nibble
                }
                var bytesHeader = dictHeader.GetBytes();
                fs.Seek(0, SeekOrigin.Begin);
                fs.Write(dictHeader.GetBytes(), 0, bytesHeader.Length);
            }
        }

        public static List<string> ReadDict(string fileName)
        {
            var lstWords = new List<string>();
            var dictBytes = File.ReadAllBytes(fileName);
            var dictHeader = DictHeader.MakeHeaderFromBytes(dictBytes);
            var wordSoFar = string.Empty;
            var nibBaseNdx = Marshal.SizeOf(dictHeader);
            var nibndx = 0;
            byte partialNib = 0;
            var havePartialNib = false;
            byte GetNextNib()
            {
                byte result;
                if (havePartialNib)
                {
                    result = partialNib;
                }
                else
                {
                    var ndx = nibBaseNdx + nibndx / 2;
                    if (ndx < dictBytes.Length)
                    {
                        partialNib = dictBytes[ndx];
                        result = (byte)(partialNib >> 4);
                        partialNib = (byte)(partialNib & 0xf);
                    }
                    else
                    {
                        result = DictHeader.EODChar;
                    }
                }
                nibndx++;
                havePartialNib = !havePartialNib;
                //                Console.WriteLine($"  GetNextNib {nibndx} {result}");
                return result;
            }
            while (true)
            {
                byte nib = 0;
                var lenSoFar = 0;
                while ((nib = GetNextNib()) == 0xf)
                {
                    lenSoFar += nib;
                }
                if (nib == DictHeader.EODChar)
                {
                    Console.WriteLine($"Got EOD {nibndx}");
                    break;
                }
                lenSoFar += nib;
                if (lenSoFar < wordSoFar.Length)
                {
                    wordSoFar = wordSoFar.Substring(0, lenSoFar);
                }
                while ((nib = GetNextNib()) != 0)
                {
                    char newchar;
                    if (nib == DictHeader.escapeChar)
                    {
                        nib = GetNextNib();
                        newchar = dictHeader.tab2[nib];
                    }
                    else
                    {
                        if (nib == DictHeader.EODChar)
                        {
                            Console.WriteLine($"GOT EODCHAR {nibndx:x2}");
                            break;
                        }
                        newchar = dictHeader.tab1[nib];
                    }
                    wordSoFar += newchar;
                }
                if (nib == DictHeader.EODChar)
                {
                    break;
                }
                lstWords.Add(wordSoFar);
                Console.WriteLine($"Got Word  {lstWords.Count,6} {nibndx:x0} {lenSoFar,2}  {wordSoFar.Length,3} {wordSoFar}");
            }
            return lstWords;
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

        public void Dispose()
        {
            _dict = null;
        }
    }
}
