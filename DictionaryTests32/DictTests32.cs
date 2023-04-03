using DictionaryLib;
using MakeDictionary;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using WordamentTests;

namespace DictionaryTests32
{
    [TestClass]
    public class DictTests32 : TestBase
    {
        [TestMethod]
        public void TestDumpOldWords()
        {
            TestContext.WriteLine($" don't use old small dict any more: use scowl instead, so only use old large dict");
            var lstOldWords = GetOldDictWords((int)DictionaryType.Large);
            foreach (var word in lstOldWords)
            {
                TestContext.WriteLine(word);
#if false
hysteria
hysterias
hysteric
hysterical
hysterically
hysterics
hz
i
ia
iamb
iambi
iambs
ibex
ibid
ibis
ibm
icbm
ice

#endif
            }

        }
        [TestMethod]
        public void TestDictIsWord()
        {
            var lstOldWords = GetOldDictWords(2);
            var dict = new DictionaryLib.DictionaryLib(DictionaryType.Small);
            var dictLarge = new DictionaryLib.DictionaryLib(DictionaryType.Large);
            Assert.IsTrue(dict.IsWord("an"));
            Assert.IsTrue(dict.IsWord("and"));
            Assert.IsTrue(dict.IsWord("at"));
            Assert.IsTrue(dict.IsWord("of"));
            Assert.IsTrue(dict.IsWord("we"));
            Assert.IsTrue(dict.IsWord("is"));
            foreach (var word in new[]
            {
                "fliest",
                "sorta",
                "oat",
                "center",
                "mister",
            })
            {
                LogMessage($"{word,-10} Small: {dict.IsWord(word)}  Large:  {dictLarge.IsWord(word)}");
            }

            var w = dict.IsWord("sinoiaterrpze");
            Assert.IsFalse(w);
            foreach (var word in lstOldWords)
            {
                if (word.Length > 1 && word.Length < dict._dictHeader.maxWordLen)
                {
                    //Assert.IsTrue(dict.IsWord(word), $"{word}");
                    LogMessage($"{dict.IsWord(word)} {word}");
                }
            }

            Assert.IsTrue(dict.IsWord("Abandon"));
            var sentence = "four score and seven years ago our fathers brought forth on this continent a new nation conceived in liberty and dedicated to the proposition that all men are created equal";

            foreach (var wrd in sentence.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                Assert.IsTrue(dict.IsWord(wrd), $"{wrd}");
                Assert.IsFalse(dict.IsWord("dd" + wrd), $"{wrd}");
            }

            Assert.IsTrue(dict.IsWord("contemporary"));
            Assert.IsTrue(dict.IsWord("police"));
            Assert.IsFalse(dict.IsWord("pollice"));
            Assert.IsFalse(dict.IsWord("zzsil"));


        }
        private List<string> GetOldDictWords(uint dictNum)
        {
            var lstWords = new List<string>();
            using (var dictWrapper = new OldDictWrapper(dictNum))
            {
                lstWords.AddRange(dictWrapper.GetWords("*"));
            }
            return lstWords;
        }
        [TestMethod]
        [Ignore]
        public void TestMakedDict()
        {
            LogMessage($"{TestContext.TestName}  {DateTime.Now:MM/dd/yy hh:mm:ss}");
            //var lstSmall = GetOldDictWords((uint)DictionaryType.Small);

            var lstSmall = TestScowl.GetScowlWords(
                desiredLevel: 35,
                desiredFiles: new[] { "american", "english" },
                undesiredFiles: new[] { "proper", "upper", "variant", "contractions", "abbrev" }
                ).ToList();


            var lstlarge = GetOldDictWords((uint)DictionaryType.Large);
            var hashLarge = new HashSet<string>();
            foreach (var wrd in lstlarge)
            {
                hashLarge.Add(wrd);
            }
            lstSmall.Remove("fliest");
            hashLarge.Remove("fliest");
            lstSmall.Remove("sorta");
            lstSmall.Remove("uteri");
            lstSmall.Remove("maria");
            lstSmall.Remove("non");
            lstSmall.Remove("gonna");
            lstSmall.Remove("genii");
            lstSmall.Remove("roes");
            lstSmall.Remove("mes");
            lstSmall.Remove("dos");
            lstSmall.Add("oat");
            lstSmall.Add("center");
            lstSmall.Add("mister");

            foreach (var word in new[]
            {
                "fliest",
                "sorta",
                "oat",
                "center",
                "mister",
            })
            {
                LogMessage($"{word,-10} Small: {lstSmall.Contains(word)}  Large:  {hashLarge.Contains(word)}");
            }


            foreach (var wrd in lstSmall)
            {
                if (!hashLarge.Contains(wrd))
                {
                    hashLarge.Add(wrd);
                    //                    Console.WriteLine($"sm not in lrg = {wrd}");
                }
            }
            foreach (var str in new[]
            {
                "miscinceptions",
                "substantialia",
                "nonconfin",
                "surdity",
                "misinforman",
                "consessione",
                "consessiones",
                "nonparticipat",
                "seventeenthes",
                "qtly", // QTLY 44, Mendaciously 43  ysoc luia tqsd lmen
            })
            {
                hashLarge.Remove(str);
            }
            //todo: remove words like "jurisdictione","nonparticipat"

            // when changing contents of dictionary, this test will fail until you update the resources, 
            // XCOPY /dy C:\Users\calvinh\Source\Repos\Wordament\WordamentTests\bin\Debug\*.bin C:\Users\calvinh\Source\Repos\Wordament\DictionaryLib\Resources
            // Then rebuild all

            for (uint dictNum = 2; dictNum >= 1; dictNum--)
            {
                List<string> lstWords = null;
                if ((DictionaryType)dictNum == DictionaryType.Small)
                {
                    lstWords = lstSmall.OrderBy(w => w).ToList();
                }
                else
                {
                    lstWords = hashLarge.OrderBy(s => s).ToList();
                }
                LogMessage($"DictSect {dictNum} NumWords = {lstWords.Count}");
                //            var fileName = @"C:\Users\calvinh\Source\Repos\Wordament\MakeDictionary\Resources\dict.bin";
                var fileName = Path.Combine(Environment.CurrentDirectory, $@"dict{dictNum}.bin");
                MakeDictionary.MakeDictionary.MakeBinFile(lstWords, fileName, dictNum);

                LogMessage($"DictSect {dictNum}  Dictionary NibTable");
                var dictBytes = File.ReadAllBytes(fileName);
                MakeDictionary.MakeDictionary.DumpDict(fileName);

                LogMessage($"DictSect {dictNum}  Raw Bytes");
                LogMessage(DumpBytes(dictBytes));

                // note: this will now read the resources of Dictionary.dll, not the just generated dumpfile, so need to update it if dictHeader struct changes
                var dict = new DictionaryLib.DictionaryLib((DictionaryType)dictNum);
                var newlstWord = new List<string>();
                var word = dict.SeekWord("");
                while (!string.IsNullOrEmpty(word))
                {
                    newlstWord.Add(word);
                    word = dict.GetNextWord();
                }
                //while (true)
                //{
                //    result = result.GetNextResult();
                //    if (result == null)
                //    {
                //        break;
                //    }
                //    newlstWord.Add(result.Word);
                //}

                //                var newlstWord = DictionaryData.DictionaryUtil.ReadDict(dictBytes);
                for (int i = 0; i < lstWords.Count; i++)
                {
                    Assert.AreEqual(lstWords[i], newlstWord[i], $"dict {dictNum}");
                }
                Assert.AreEqual(newlstWord.Count(), lstWords.Count(), $"dict num {dictNum} ");
            }
        }
        public string DumpBytes(byte[] bytes, bool fIncludeCharRep = true)
        {
            StringBuilder sb = new();
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
