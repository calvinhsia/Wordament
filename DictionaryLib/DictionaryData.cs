using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DictionaryData
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
        [MarshalAs(UnmanagedType.I4)]
        public int nibbleOffset;
        // # of entries in this bucket (used for RandWord
        [MarshalAs(UnmanagedType.I2)]
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
        public const byte EOFChar = 0xff;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        public string tab1;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        public string tab2;

        [MarshalAs(UnmanagedType.I4)]
        public int wordCount; //total # of words in dictionary
        [MarshalAs(UnmanagedType.I4)]
        public int maxWordLen; //longest word length in dictionary


        /// <summary>
        /// 26*26 array of DictHeaderNibbleEntry 
        /// AA, AB, AC... BA,BB,BC.... the first one points to e.g. "aardvark", next to "abandon", etc.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 26 * 26)]
        public DictHeaderNibbleEntry[] nibPairPtr;

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
}
