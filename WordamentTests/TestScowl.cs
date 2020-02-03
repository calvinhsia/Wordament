using DictionaryLib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace WordamentTests
{

    [TestClass]
    public class TestScowl : TestBase
    {
        [TestMethod]
        public void TestScowlExtract()
        {
            var wordsScowl = GetScowlWords(
                desiredLevel: 35,
                desiredFiles: new[] { "american", "english" },
                undesiredFiles: new[] { "proper", "upper", "variant", "contractions", "abbrev" }
                );

            var dictlibDict = new DictionaryLib.DictionaryLib(DictionaryType.Large);
            var dictlibWords = new SortedSet<string>();
            while (true)
            {
                var word = dictlibDict.GetNextWord();
                if (string.IsNullOrEmpty(word))
                {
                    break;
                }
                dictlibWords.Add(word);
            }

            TestContext.WriteLine($"Count dictlib   {dictlibWords.Count()}");
            TestContext.WriteLine($"Count wordsScowl {wordsScowl.Count()}");
            var wordsInDictLibNotInScowl = dictlibWords.Where(w => !wordsScowl.Contains(w));
            var wordsInScowlNotInDictLib = wordsScowl.Where(w => !dictlibWords.Contains(w));
            TestContext.WriteLine($"Count wordsInDictLibNotInScowl {wordsInDictLibNotInScowl.Count()}");
            TestContext.WriteLine($"Count wordsInScowlNotInDictLib {wordsInScowlNotInDictLib.Count()}");
            foreach (var word in wordsInDictLibNotInScowl)
            {
                if (word.Length >= 0)
                {
                    TestContext.WriteLine($"wordInDNotInS {word}");
                }
            }

            foreach (var word in wordsInScowlNotInDictLib)
            {
                if (word.Length >= 0)
                {
                    TestContext.WriteLine($"wordInSNotInD {word}");
                }
            }

        }

        public static SortedSet<string> GetScowlWords(int desiredLevel, string[] desiredFiles, string[] undesiredFiles)
        {
            // http://wordlist.aspell.net/
            var pathScowl = @"C:\Users\calvinh\source\repos\Wordament\Scowl\Final";
            var dict = new SortedDictionary<string, List<string>>(); // word -> lst<files>
            var result = new SortedSet<string>();
            foreach (var file in Directory.EnumerateFiles(pathScowl, "*.*"))
            {
                var level = Path.GetExtension(file).Replace(".", string.Empty);
                if (int.TryParse(level, out var nLevel))
                {
                    if (nLevel <= desiredLevel)
                    {
                        var justFile = System.IO.Path.GetFileName(file);
                        foreach (var word in File.ReadAllLines(file).Where(l => !string.IsNullOrEmpty(l)))
                        {
                            if (!word.Contains("�")) // 'eclair
                            {
                                if (!char.IsUpper(word[0]) && !word.Contains("'"))
                                {
                                    if (!dict.TryGetValue(word, out var lstFiles))
                                    {
                                        lstFiles = new List<string>();
                                        dict[word] = lstFiles;
                                    }
                                    lstFiles.Add(justFile);
                                }
                            }
                        }
                    }
                }
            }
#if false
abetters, australian-words.50,british-words.50,british_z-words.50,variant_1-words.50
abetter's, australian-words.50,british-words.50,british_z-words.50,variant_1-words.50
abetting, english-words.35
abettor, american-words.50,australian_variant_1-words.50,british_variant_1-words.50,canadian-words.50,variant_1-words.50
abettors, american-words.50,australian_variant_1-words.50,british_variant_1-words.50,canadian-words.50,variant_1-words.50
abettor's, american-words.50,australian_variant_1-words.50,british_variant_1-words.50,canadian-words.50,variant_1-words.50
abevacuation, english-words.95
aaerialness, australian-words.95,british-words.95,british_z-words.95,canadian-words.95
#endif
            foreach (var wordItem in dict)
            {
                var files = string.Join(",", wordItem.Value);
                if (!undesiredFiles.Where(unde => files.Contains(unde)).Any())
                {
                    if (desiredFiles.Where(des => files.Contains(des)).Any())
                    {
                        result.Add(wordItem.Key);
                        //                        TestContext.WriteLine($"{wordItem.Key}, {files}");
                    }
                }
            }
            return result;
        }
    }
}
