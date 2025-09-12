Write-Host "Running DictionaryLib Benchmarks..." -ForegroundColor Green
Write-Host ""
Write-Host "This will run performance benchmarks for dictionary operations including:" -ForegroundColor Yellow
Write-Host "- SeekWord operations on small and large dictionaries" -ForegroundColor White
Write-Host "- IsWord operations on small and large dictionaries" -ForegroundColor White
Write-Host "- RandomWord operations on small and large dictionaries" -ForegroundColor White
Write-Host ""
Write-Host "The benchmark will test with both valid and invalid words." -ForegroundColor Yellow
Write-Host "Results will be saved in the BenchmarkDotNet.Artifacts folder." -ForegroundColor Yellow
Write-Host ""
Read-Host "Press Enter to continue"

Set-Location DictionaryLibBenchmarks
dotnet run -c Release

Write-Host ""
Write-Host "Benchmark completed! Check the BenchmarkDotNet.Artifacts folder for detailed results." -ForegroundColor Green
Read-Host "Press Enter to exit"