using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        public Dictionary.CDict _dict;
        public MakeDictionary(int dictNum)
        {
            _dict = new Dictionary.CDict()
            {
                DictNum = (uint)dictNum
            };
            MakeBinFile();
        }
        void MakeBinFile()
        {
            var fileName = @"c:\users\calvinh\dict.bin";
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }
            using (var fs = File.Open(fileName, FileMode.CreateNew))
            {
                fs.Write(new byte[] { 65, 66, 67 },0 ,3);
            }
        }
        public IEnumerable<string> GetWord(string start)
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
            //var wrds = _dict.Words;
            //foreach (var wrd in _dict.Words)
            //{

            //}
        }
    }
}
