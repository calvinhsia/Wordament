using System;
using System.Diagnostics;
using System.Linq;

namespace DictionaryLib
{
    /// <summary>
    /// represent a word as a read/write byte array
    /// A string is immutable
    /// This also reduces need to convert to char/string for perf. Not null terminated
    /// 
    /// </summary>
    [DebuggerDisplay("{GetWord()}")]
    internal class MyWord : IComparable
    {
        public static MyWord Empty = new MyWord();
        private byte[] _wordBytes = new byte[DictionaryLib.MaxWordLen];
        int _currentLength;
        public MyWord()
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
                _wordBytes[ndx] = (byte)word[ndx];
            }
        }
        public void SetWord(byte byte0, byte byte1, byte byte2)
        {
            _currentLength = 3;
            _wordBytes[0] = byte0;
            _wordBytes[1] = byte1;
            _wordBytes[2] = byte2;
        }
        /// <summary>
        /// Get the length of the word. If DesiredLength is non-zero, return the min(DesiredLength, CurrentLength)
        /// </summary>
        /// <param name="DesiredLength"></param>
        /// <returns></returns>
        public string GetWord(int DesiredLength = 0)
        {
            var len = _currentLength;
            if (DesiredLength != 0)
            {
                len = Math.Min(DesiredLength, len);
            }
            var str = string.Empty;
            for (int i = 0; i < len; i++)
            {
                str += (char)_wordBytes[i];
            }
            //            return Encoding.ASCII.GetString(_wordBytes, 0, len);
            return str;
        }

        public void AddByte(byte b)
        {
            _wordBytes[_currentLength++] = b;
        }
        public int WordLength => _currentLength;
        public void SetLength(int Length)
        {
            _currentLength = Length;
        }
        public int IndexOf(byte b)
        {
            var res = -1;
            for (int i = 0; i < WordLength; i++)
            {
                if (_wordBytes[i] == b)
                {
                    res = i;
                    break;
                }
            }
            return res;
        }

        public int CompareTo(object obj)
        {
            int retval = 0;
            if (obj is MyWord other)
            {
                for (int i = 0; i < Math.Min(this._currentLength, other._currentLength); i++)
                {
                    /*
                    var thisone = this._wordBytes[i];
                    var thatone = other._wordBytes[i];
                    if (thisone != thatone)
                    {
                        retval = thisone.CompareTo(thatone);
                        if (retval != 0)
                        {
                            break;
                        }
                    }
                    /*/
                    if (this[i] != other[i])
                    {
                        retval = this[i].CompareTo(other[i]);
                        if (retval != 0)
                        {
                            break;
                        }
                    }
                    //*/
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
        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }
        public override bool Equals(object obj)
        {
            if (obj is MyWord myWord)
            {
                return this.CompareTo(myWord) == 0;
            }
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return GetWord();
        }

        internal void SetAllBytes(byte qmarkChar)
        {
            for (int i = 0; i < _wordBytes.Length; i++)
            {
                _wordBytes[i] = qmarkChar;
            }
        }

        internal void SortBytes()
        {
            // sort bytes, with null at the end             
            _wordBytes = _wordBytes.OrderBy(b => b == '\0' ? 127 : b).ToArray();
        }

        public byte this[int key] { get { return this._wordBytes[key]; } set { this._wordBytes[key] = value; } }
    }
}