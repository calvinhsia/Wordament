using BenchmarkDotNet.Attributes;
using DictionaryLib;
using System;

namespace DictionaryLibBenchmarks
{
    [MemoryDiagnoser]
    public class DictionaryLibWordLookupBenchmark
    {
        private DictionaryLib.DictionaryLib _dictSmall;
        private DictionaryLib.DictionaryLib _dictLarge;
        private string[] _testWords;
        private string[] _invalidWords;

        [GlobalSetup]
        public void Setup()
        {
            // Use a sample of words for lookup
            _dictSmall = new DictionaryLib.DictionaryLib(DictionaryType.Small);
            _dictLarge = new DictionaryLib.DictionaryLib(DictionaryType.Large);
            
            // Use a sample of valid words for lookup
            _testWords = new[] { "apple", "banana", "cat", "dog", "elephant", "fish", "goat", "hat", "ice", "jungle" };
            
            // Use a sample of invalid words for negative testing
            _invalidWords = new[] { "xyzabc", "notreal", "fakewrd", "invalid", "missing", "absent", "gone", "lost", "void", "empty" };
        }

        [Benchmark]
        public void SeekWord_SmallDict_ValidWords()
        {
            foreach (var word in _testWords)
            {
                _dictSmall.SeekWord(word);
            }
        }

        [Benchmark]
        public void SeekWord_LargeDict_ValidWords()
        {
            foreach (var word in _testWords)
            {
                _dictLarge.SeekWord(word);
            }
        }

        [Benchmark]
        public void SeekWord_SmallDict_InvalidWords()
        {
            foreach (var word in _invalidWords)
            {
                _dictSmall.SeekWord(word);
            }
        }

        [Benchmark]
        public void IsWord_SmallDict_ValidWords()
        {
            foreach (var word in _testWords)
            {
                _dictSmall.IsWord(word);
            }
        }

        [Benchmark]
        public void IsWord_LargeDict_ValidWords()
        {
            foreach (var word in _testWords)
            {
                _dictLarge.IsWord(word);
            }
        }

        [Benchmark]
        public void RandomWord_SmallDict()
        {
            for (int i = 0; i < 10; i++)
            {
                _dictSmall.RandomWord();
            }
        }

        [Benchmark]
        public void RandomWord_LargeDict()
        {
            for (int i = 0; i < 10; i++)
            {
                _dictLarge.RandomWord();
            }
        }
    }
}