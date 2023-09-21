# Simd Iteration

Test SIMD 512, 256, 128 registers for fast aggregate calculations.

Unfortunately my hardware doesn't support Vector512.

Anyway, the performance improvement is mindblowing.

> [!IMPORTANT]
> net8 is x146 times faster than net48 for calculate the Min and Max at the same time !!

## Results

- BenchmarkDotNet=v0.13.5, OS=Windows 10 (10.0.19044.3086/21H2/November2021Update)
-  AMD Ryzen 7 1700, 1 CPU, 16 logical and 8 physical cores
-  .NET SDK=8.0.100-rc.1.23455.8
-    [Host]             : .NET 8.0.0 (8.0.23.41904), X64 RyuJIT AVX2
-    .NET 7.0           : .NET 7.0.11 (7.0.1123.42427), X64 RyuJIT AVX2
-    .NET 8.0           : .NET 8.0.0 (8.0.23.41904), X64 RyuJIT AVX2
-    .NET Framework 4.8 : .NET Framework 4.8 (4.8.4644.0), X64 RyuJIT VectorSize=256
  


|        Method |            Runtime |  Size |           Mean | Allocated |
|-------------- |------------------- |------ |---------------:|----------:|
|    MinMaxLinq | .NET Framework 4.8 | 10000 | 118,675.226 ns |      65 B |
|    MinMaxLinq |           .NET 7.0 | 10000 |   2,350.046 ns |         - |
|    MinMaxLinq |           .NET 8.0 | 10000 |   1,228.518 ns |         - |
|    MinMaxSimd |           .NET 7.0 | 10000 |     834.291 ns |         - |
|    **MinMaxSimd** |   **.NET 8.0** | 10000 |     **808.150 ns** |         - |
