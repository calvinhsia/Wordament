﻿using DictionaryData;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
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
            LogMessage($"Lookup Table 1: '{dictHeader.tab1}'");
            LogMessage($"Lookup Table 2: '{dictHeader.tab2}'");
            dictHeader.nibPairPtr = new DictHeaderNibbleEntry[26 * 26 * 26];
            var nibpairNdx = 0;
            var let0 = 'a';
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
                    if (word[0] == let0 && (word.Length < 2 || word[1] == let1) && (word.Length < 3 || word[2] == let2))
                    {
                        // same bucket
                    }
                    else
                    { // diff bucket
                        var orignibPairNdx = nibpairNdx;
                        let0 = word[0];
                        let1 = word.Length > 1 ? word[1] : 'a';
                        let2 = word.Length > 2 ? word[2] : 'a';

                        nibpairNdx = ((let0 - 97) * 26 + let1 - 97) * 26 + let2 - 97;
                        // fill all missing buckets between too
                        for (int i = orignibPairNdx + 1; i <= nibpairNdx; i++)
                        {
                            dictHeader.nibPairPtr[i].nibbleOffset = curNibNdx;
                        }
                        LogMessage($"AddBucket  {let0} {let1} {let2} {word} {curNibNdx:x4}");
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
                if (maxWordLen > 30)
                {
                    throw new InvalidOperationException("Word length > 30");
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
            LogMessage($"{fileName} dump HdrSize= {Marshal.SizeOf(dictHeader)}  (0x{Marshal.SizeOf(dictHeader):x8}) MaxWordLen = {dictHeader.maxWordLen}");
            LogMessage($"Entire dump len= {dictBytes.Length}");

            var cntwrds = 0;
            for (int i = 0; i < 26; i++)
            {
                for (int j = 0; j < 26; j++)
                {
                    for (int k = 0; k < 26; k++)
                    {
                        cntwrds += dictHeader.nibPairPtr[(i * 26 + j) * 26 + k].cnt;
                        LogMessage($"{Convert.ToChar(i + 65)} {Convert.ToChar(j + 65)} {Convert.ToChar(k + 65)} {dictHeader.nibPairPtr[(i * 26 + j) * 26 + k].nibbleOffset:x4}  {dictHeader.nibPairPtr[(i * 26 + j) * 26 + k].cnt}");
                    }
                }
            }
            LogMessage($"TotWrds = {dictHeader.wordCount}   TotNibtblcnt={cntwrds}");
            return dictBytes;
        }
    }

    public class OldDictWrapper : IDisposable
    {
        const string dictCppDllName = @"Dictionary.dll"; // copy to output=true
        internal delegate int DllGetClassObject(
            [In, MarshalAs(UnmanagedType.LPStruct)] Guid ClassId,
            [In, MarshalAs(UnmanagedType.LPStruct)] Guid riid,
            out IntPtr ppvObject);

        delegate int CanUnloadNowRoutine();
        CanUnloadNowRoutine _deldllCanUnloadNow;

        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
        static private extern int FreeLibrary(IntPtr hModule);

        [DllImport("kernel32.dll", EntryPoint = "GetModuleHandle", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern IntPtr GetModuleHandle([MarshalAs(UnmanagedType.LPWStr)] string lpModuleName);

        const string IUnknownGuid = "00000001-0000-0000-C000-000000000046";

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr LoadLibrary(string dllToLoad);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string procname);

        [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid(IUnknownGuid)]
        private interface IClassFactory
        {
            [PreserveSig]
            int CreateInstance(IntPtr pUnkOuter, ref Guid riid, out IntPtr ppvObject);
            int LockServer(int fLock);
        }
        IntPtr _hModule = IntPtr.Zero;
        public DictionaryCPP.CDict _dict;

        /// <summary>Creates com object with the given clsid in the specified file</summary>
        /// <param name="fnameComClass">The path of the module</param>
        /// <param name="clsidOfComObj">The CLSID of the com object</param>
        /// <param name="riid">The IID of the interface requested</param>
        /// <param name="pvObject">The interface pointer. Upon failure pvObject is IntPtr.Zero</param>
        /// <returns>An HRESULT</returns>
        [HandleProcessCorruptedStateExceptions]
        internal int CoCreateFromFile(string fnameComClass, Guid clsidOfComObj, Guid riid, out IntPtr pvObject)
        {
            pvObject = IntPtr.Zero;
            int hr = HResult.E_FAIL;
            try
            {
                _hModule = LoadLibrary(fnameComClass);
                if (_hModule != IntPtr.Zero)
                {
                    IntPtr optrDllGetClassObject = GetProcAddress(_hModule, "DllGetClassObject");
                    if (optrDllGetClassObject != IntPtr.Zero)
                    {
                        var delDllGetClassObject = Marshal.GetDelegateForFunctionPointer<DllGetClassObject>(optrDllGetClassObject);
                        var optrDllCanUnloadNow = GetProcAddress(_hModule, "DllCanUnloadNow");
                        _deldllCanUnloadNow = Marshal.GetDelegateForFunctionPointer<CanUnloadNowRoutine>(optrDllCanUnloadNow);

                        IntPtr pClassFactory = IntPtr.Zero;
                        Guid iidIUnknown = new Guid(IUnknownGuid);
                        hr = delDllGetClassObject(clsidOfComObj, iidIUnknown, out pClassFactory);
                        if (hr == HResult.S_OK)
                        {
                            var classFactory = (IClassFactory)Marshal.GetTypedObjectForIUnknown(pClassFactory, typeof(IClassFactory));
                            hr = classFactory.CreateInstance(IntPtr.Zero, ref riid, out pvObject);
                            Marshal.ReleaseComObject(classFactory);
                            Marshal.Release(pClassFactory);
                        }
                    }
                    else
                    {
                        hr = Marshal.GetHRForLastWin32Error();
                        Debug.Assert(false, $"Unable to find DllGetClassObject: {hr}");
                    }
                }
                else
                {
                    hr = Marshal.GetHRForLastWin32Error();
                    Debug.Assert(false, $"Unable to load {fnameComClass}: {hr}");
                }
            }
            catch (Exception ex)
            {
                var x = ex.ToString(); // HandleProcessCorruptedStateExceptions
                throw new InvalidOperationException(x);

            }
            return hr;
        }

        public OldDictWrapper(uint dictNum)
        {
            // old way: requires Regsvr32
            //_dict = new Dictionary.CDict()
            //{
            //    DictNum = dictNum
            //};

            // new way: use DllGetClassObject, IClassFactory directly. No registration needed.
            // tlbimp Dictionary.dll /namespace:DictionaryCPP /out:DictionaryCPP.Interop.dll /verbose
            //            var dictCppDllName = @"C:\Users\calvinh\source\repos\Wordament\MakeDictionary\Dictionary.dll";
            //var g1 = typeof(DictionaryCPP.CDict).GUID; //0CED18E4-8870-4F62-B1CB-E50C3BCA8FB3
            //var g2 = typeof(DictionaryCPP.IDict).GUID; //0CED18E4-8870-4F62-B1CB-E50C3BCA8FB3
            var g3 = typeof(DictionaryCPP.CDictClass).GUID; //3ED98B67-96FC-42A1-A361-2141CC07D1C4
            //var g3 = new Guid("3ED98B67-96FC-42A1-A361-2141CC07D1C4");

            //NOTE: Check Bitness: Dictionary.dll is a 32 bit DLL from revers engineering the Wordperfect dictionary
            // thus this code must be run as 32 bit
            if (IntPtr.Size != 4)
            {
                throw new InvalidOperationException($"Must run as 32 bit (TestExplorer->Settings Menu->ProcArch for AnyCPU projs=>X86  {dictCppDllName}");
            }
            var hr = CoCreateFromFile(dictCppDllName, g3, typeof(DictionaryCPP.IDict).GUID, out var pObject);
            if (hr != HResult.S_OK)
            {
                throw new InvalidOperationException($"Could not find old dict {dictCppDllName}");
            }
            _dict = (DictionaryCPP.CDict)Marshal.GetObjectForIUnknown(pObject);
            Marshal.Release(pObject);
            _dict.DictNum = dictNum;
        }

        public IEnumerable<string> FindAnagrams(string word)
        {
            _dict.FindAnagram(word, nSubWords: 0);
            foreach (var wrd in _dict.Words)
            {
                var res = wrd as string;
                Marshal.ReleaseComObject(wrd);
                yield return res;
            }
            Marshal.ReleaseComObject(_dict.Words);
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
            Marshal.ReleaseComObject(_dict);
            _dict = null;

            if (_deldllCanUnloadNow() == 0)
            {
                FreeLibrary(_hModule);
                var res = GetModuleHandle(dictCppDllName);
                if (res != IntPtr.Zero)
                {
                    throw new InvalidOperationException($"Didn't unload {dictCppDllName}");
                }
            }
            else
            {
                throw new InvalidOperationException($"Couldn't unload {dictCppDllName}");
                //                Debug.Assert(GetModuleHandle(dictCppDllName) == IntPtr.Zero);

            }
        }
    }
}
