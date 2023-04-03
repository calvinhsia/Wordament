# Wordament
English language Dictionary and Wordament-like game
English language word games require a dictionary of words.
In the 1980's I reverse engineered a spelling dictionary to get a list of words, and I encoded the words to use very little memory.
Even today, consuming less memory can mean a more performant application.
The words are encoded in alphabetical order.

Each word entry starts with 4 bits indicating how much of the prior word to keep for the next word, and with most common letters taking 4 bits.

DictionaryLib_Calvin_Hsia is available on nuget.org.

    <PackageReference Include="DictionaryLib_Calvin_Hsia" Version="1.0.3" />
There are 2 dictionaries encoded, Large has ~170,000 words and Small has ~52,000

Usage:
```
            var dict = new DictionaryLib.DictionaryLib(DictionaryLib.DictionaryType.Small);
            str = $"Word of the day '{dict.RandomWord()}'";

```
