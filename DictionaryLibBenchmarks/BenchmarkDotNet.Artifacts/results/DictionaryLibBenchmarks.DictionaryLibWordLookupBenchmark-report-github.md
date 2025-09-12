```

BenchmarkDotNet v0.15.2, Windows 11 (10.0.22631.5768/23H2/2023Update/SunValley3)
AMD EPYC 7763 2.44GHz, 1 CPU, 16 logical and 8 physical cores
.NET SDK 10.0.100-rc.1.25451.107
  [Host]     : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2
  DefaultJob : .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512), X64 RyuJIT AVX2


```
| Method                          | Mean       | Error     | StdDev    | Gen0   | Allocated |
|-------------------------------- |-----------:|----------:|----------:|-------:|----------:|
| SeekWord_SmallDict_ValidWords   |   3.544 μs | 0.0176 μs | 0.0165 μs | 0.0725 |    1224 B |
| SeekWord_LargeDict_ValidWords   |  13.462 μs | 0.0922 μs | 0.0862 μs | 0.0610 |    1224 B |
| SeekWord_SmallDict_InvalidWords |   6.673 μs | 0.0893 μs | 0.0835 μs | 0.0687 |    1256 B |
| IsWord_SmallDict_ValidWords     |   3.737 μs | 0.0094 μs | 0.0079 μs | 0.0725 |    1224 B |
| IsWord_LargeDict_ValidWords     |  12.657 μs | 0.1168 μs | 0.1092 μs | 0.0610 |    1224 B |
| RandomWord_SmallDict            |  77.115 μs | 0.2427 μs | 0.2270 μs |      - |     408 B |
| RandomWord_LargeDict            | 110.928 μs | 0.3971 μs | 0.3714 μs |      - |     428 B |
