﻿using DictionaryData;
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

        public static byte[] DumpDict(string fileName)
        {
            var dictBytes = File.ReadAllBytes(fileName);
            var dictHeader = DictHeader.MakeHeaderFromBytes(dictBytes);
            Console.WriteLine($"{fileName} dump HdrSize= {Marshal.SizeOf(dictHeader)}  (0x{Marshal.SizeOf(dictHeader):x8})");
            Console.WriteLine($"Entire dump len= {dictBytes.Length}");

            for (int i = 0; i < 26; i++)
            {
                for (int j = 0; j < 26; j++)
                {
                    Console.WriteLine($"{Convert.ToChar(i + 65)} {Convert.ToChar(j + 65)} {dictHeader.nibPairPtr[i * 26 + j].nibbleOffset:x4}  {dictHeader.nibPairPtr[i * 26 + j].cnt}");
                }
            }
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
