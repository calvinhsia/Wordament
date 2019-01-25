using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MakeDictionary
{
    //ptr, cnt into encoded compressed dictionary data
    struct DictHeaderNibbleEntry
    {
        // offset of nibble
        // to get offset from dict base, divide by 2. If odd, skip a nibble
        public int nibbleOffset;
        // # of entries in this bucket (used for RandWord
        public int cnt;
        public override string ToString()
        {
            return $"{nibbleOffset} {cnt}";
        }
    }

    // Encoded, compressed dictionary
    // data is a nibble (4 bits) 0-15.
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct DictHeader
    {
        public int wordCount; //total # of words in dictionary
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] lookupTab1;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] lookupTab2;
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
    }

    /// <summary>
    /// Convert from the old STA COM based C++ dictionary to a C# based
    ///   
    /// </summary>
    public class MakeDictionary
    {
        public MakeDictionary()
        {
            for (uint dictNum = 2; dictNum <= 2; dictNum++)
            {
                var lstWords = new List<string>();
                using (var dictWrapper = new OldDictWrapper(dictNum))
                {
                    lstWords.AddRange(dictWrapper.GetWords("*"));
                }
                //            var fileName = @"C:\Users\calvinh\Source\Repos\Wordament\MakeDictionary\Resources\dict.bin";
                var fileName = Path.Combine(Environment.CurrentDirectory, $@"dict{dictNum}.bin");
                MakeBinFile(lstWords, fileName);
            }
        }

        void MakeBinFile(List<string> words, string fileName)
        {
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }
            DictHeader dictHeader = new DictHeader();
            var tab1 = " acdegilmnorstu"; // 0th entry isn't used: 15 is escape to next table
            var tab2 = " bfhjkpqvwxzy"; // 0th entry isn't used
            dictHeader.lookupTab1 = new byte[16];
            dictHeader.lookupTab2 = new byte[16];
            void initTab(byte[] lookupTab, string data)
            {
                int ndx = 0;
                foreach (var c in data)
                {
                    var b = Convert.ToByte(c);
                    //var enc = Encoding.ASCII.GetBytes(new char[] { c });
                    lookupTab[ndx++] = b;
                }
            }
            initTab(dictHeader.lookupTab1, tab1);
            initTab(dictHeader.lookupTab2, tab2);

            using (var fs = File.Open(fileName, FileMode.CreateNew))
            {
                var bytesHeader = dictHeader.GetBytes();
                fs.Write(dictHeader.GetBytes(), 0, bytesHeader.Length);
                var letndx1 = 0; // from 'a'
                var letndx2 = 0; // from 'a'
                var let1 = Convert.ToChar(letndx1);
                var let2 = Convert.ToChar(letndx2);
                var curNib = 0;
                var nkeepSoFar = 0;
                var wordSofar = "a";
                foreach (var word in words)
                {
                    if (word[0] == let1 && (word.Length < 2 || word[1] == let2))
                    {
                        // same bucket
                        for (int i = 1; i < word.Length; i++)
                        {
                            if (wordSofar[i] == word[i])
                            {

                            }

                        }

                    }
                    else
                    { // diff bucket
                        if (let2 == 'z')
                        {
                            let2 = 'a';
                            let1 = Convert.ToChar(++letndx1);
                        }
                        else
                        {
                            let2 = Convert.ToChar(++letndx2);
                        }
                    }
                }
            }
            // now test it
            using (var fs = File.OpenRead(fileName))
            {
                var size = Marshal.SizeOf<DictHeader>();
                var bytes = new byte[size];
                var nbytes = fs.Read(bytes, 0, size);
                var newheader = DictHeader.MakeHeaderFromBytes(bytes);
            }

        }

        void oldstuff()
        {
            //var assembly = System.Reflection.IntrospectionExtensions.GetTypeInfo(typeof(MakeDictionary)).Assembly;
            //System.IO.Stream stream = assembly.GetManifestResourceStream(typeof(MakeDictionary), "dict.bin"); // 150M works
            //string text = "";
            //using (var reader = new System.IO.StreamReader(stream))
            //{
            //    text = reader.ReadToEnd();
            //}
            //var outpath = System.IO.Path.Combine(
            //    System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData),
            //    "tt.txt");
            //System.IO.File.WriteAllText(outpath, "some data");



            var mem = Marshal.AllocCoTaskMem(26 * 26 + 100);
            var ptr = mem;
            int n = 0;
            for (int i = 0; i < 26; i++)
            {
                for (int j = 0; j < 26; j++)
                {
                    Marshal.WriteByte(ptr + n++, (byte)j);
                }
            }
            for (int i = 0; i < 100; i++)
            {
                Marshal.WriteByte(ptr + n++, (byte)i);
            }
            DictHeader ss;
            var xxx = Marshal.PtrToStructure<DictHeader>(mem);
            Marshal.FreeCoTaskMem(mem);

            //for (int i = 0; i <100; i++)
            //{
            //    var b = xxx.data[i];
            //}

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
