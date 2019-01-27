using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using MakeDictionary;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WordamentTests
{
    [TestClass]
    public class TestMakeDict
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void TestMakedDict()
        {
            Console.WriteLine($"{TestContext.TestName}  {DateTime.Now.ToString("MM/dd/yy hh:mm:ss")}");
            var rsrc = Dictionary.Properties.Resources.dict1;
            Console.WriteLine($"Got resources size = {rsrc.Length}  {Dictionary.Properties.Resources.dict2.Length}");

            for (uint dictNum = 1; dictNum < 2; dictNum++)
            {
                var lstWords = new List<string>();
                using (var dictWrapper = new OldDictWrapper(dictNum))
                {
                    lstWords.AddRange(dictWrapper.GetWords("*"));
                }
                //            var fileName = @"C:\Users\calvinh\Source\Repos\Wordament\MakeDictionary\Resources\dict.bin";
                var fileName = Path.Combine(Environment.CurrentDirectory, $@"dict{dictNum}.bin");
                MakeDictionary.MakeDictionary.MakeBinFile(lstWords.Take(10000000), fileName);

                var dictBytes = File.ReadAllBytes(fileName);
                var dictHeader = DictHeader.MakeHeaderFromBytes(dictBytes);
                Console.WriteLine($"{fileName} dump HdrSize= {Marshal.SizeOf(dictHeader)}  (0x{Marshal.SizeOf(dictHeader):x8})");
                Console.WriteLine($"Entire dump len= {dictBytes.Length}");
                Console.WriteLine(DumpBytes(dictBytes));

                for (int i = 0; i < 26; i++)
                {
                    for (int j = 0; j < 26; j++)
                    {
                        Console.WriteLine($"{Convert.ToChar(i + 65)} {Convert.ToChar(j + 65)} {dictHeader.nibPairPtr[i * 26 + j].nibbleOffset:x4}  {dictHeader.nibPairPtr[i * 26 + j].cnt}");
                    }
                }

                var newlstWord = MakeDictionary.MakeDictionary.ReadDict(fileName);
                Assert.AreEqual(newlstWord.Count, lstWords.Count());
                for (int i = 0; i < lstWords.Count; i++)
                {
                    Assert.AreEqual(lstWords[i], newlstWord[i]);
                }
            }
        }

        [TestMethod]
        public void TestGetResources()
        {
            Console.WriteLine($"{TestContext.TestName}  {DateTime.Now.ToString("MM/dd/yy hh:mm:ss")}");
            var x = Dictionary.Dictionary.GetResource();
            Console.WriteLine(DumpBytes(x));
        }

        public string DumpBytes(byte[] bytes, bool fIncludeCharRep = true)
        {
            StringBuilder sb = new StringBuilder();
            var addr = 0;
            var padLength = (16 - (bytes.Length % 16)) % 16;
            sb.AppendLine($"padlen = {padLength}");
            for (int i = 0; i < bytes.Length + padLength; i++, addr++)
            {
                if (i % 16 == 0) // beginning of new line
                {
                    sb.Append($"{addr:x8}  ");
                }
                else if (i % 8 == 0)
                {
                    sb.Append(" ");
                }

                var dat = i < bytes.Length ? bytes[i].ToString("x2") : "  ";
                sb.Append($" {dat}");
                if (i % 16 == 15) // we did the last on the line. Add the char rep
                {
                    if (fIncludeCharRep)
                    {
                        var charrep = string.Empty;
                        for (int j = i - 15; j <= i; j++)
                        {
                            if (j < bytes.Length)
                            {
                                var val = j < bytes.Length ? bytes[j] : 32;
                                var chr = Convert.ToChar(val);
                                if (!char.IsSymbol(chr) && !char.IsLetterOrDigit(chr) && !char.IsPunctuation(chr) && chr != ' ')
                                {
                                    chr = '.';
                                }
                                charrep += chr;
                            }
                        }
                        sb.AppendLine($"  {charrep}");
                    }
                    else
                    {
                        sb.AppendLine();
                    }
                }
            }
            return sb.ToString();
        }
    }
}
