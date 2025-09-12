@echo off
echo Running DictionaryLib Benchmarks...
echo.
echo This will run performance benchmarks for dictionary operations including:
echo - SeekWord operations on small and large dictionaries
echo - IsWord operations on small and large dictionaries  
echo - RandomWord operations on small and large dictionaries
echo.
echo The benchmark will test with both valid and invalid words.
echo Results will be saved in the BenchmarkDotNet.Artifacts folder.
echo.
pause

cd DictionaryLibBenchmarks
dotnet run -c Release

echo.
echo Benchmark completed! Check the BenchmarkDotNet.Artifacts folder for detailed results.
pause