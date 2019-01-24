using System;
using System.Collections.Generic;
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
            _dict.DictNum = 1;
        }
        public IEnumerable<string> GetWord(string start)
        {
            var wrd1 = _dict.FindMatch(start);
            yield return wrd1;
            yield return _dict.NextMatch();
            //var wrds = _dict.Words;
            //foreach (var wrd in _dict.Words)
            //{

            //}
        }
    }
}
