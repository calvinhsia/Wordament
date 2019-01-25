using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MakeDictionary
{
    //ptr, cnt into encoded compressed dictionary data
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
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
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        public string tab1;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        public string tab2;
        [MarshalAs(UnmanagedType.I4)]
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
            for (int i = 0; i < this.lookupTab1.Length; i++)
            {
                if (this.lookupTab1[i] != that.lookupTab1[i])
                    return false;
            }
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
                //MakeBinFile(lstWords, fileName);
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
