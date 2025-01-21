``` ini

BenchmarkDotNet=v0.13.5, OS=Windows 10 (10.0.19045.5371/22H2/2022Update)
Intel Core i5-6400 CPU 2.70GHz (Skylake), 1 CPU, 4 logical and 4 physical cores
.NET SDK=8.0.300
  [Host]     : .NET 8.0.5 (8.0.524.21615), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.5 (8.0.524.21615), X64 RyuJIT AVX2


```
|                     Method |       Mean |      Error |     StdDev |     Median |  Ratio | RatioSD |      Gen0 |      Gen1 |     Gen2 |  Allocated | Alloc Ratio |
|--------------------------- |-----------:|-----------:|-----------:|-----------:|-------:|--------:|----------:|----------:|---------:|-----------:|------------:|
|              JsonSerialize |   2.617 ms |  0.0430 ms |  0.0403 ms |   2.602 ms |   1.00 |    0.00 |   97.6563 |   97.6563 |  97.6563 |   378.5 KB |        1.00 |
|        JsonSerializeBinary |   6.272 ms |  0.1210 ms |  0.1441 ms |   6.299 ms |   2.38 |    0.07 |  312.5000 |  312.5000 | 312.5000 | 3048.08 KB |        8.05 |
|            FlexonSerialize | 262.982 ms | 14.4544 ms | 40.0531 ms | 248.421 ms | 100.94 |   13.69 |         - |         - |        - |   77.34 KB |        0.20 |
|      FlexonSerializeBinary | 403.712 ms | 30.9234 ms | 88.7250 ms | 369.444 ms | 137.60 |   18.14 |         - |         - |        - |   77.55 KB |        0.20 |
|   FlexonSerializeEncrypted | 319.309 ms | 13.2834 ms | 38.1127 ms | 307.084 ms | 124.24 |   12.92 |         - |         - |        - |   77.63 KB |        0.21 |
|            JsonDeserialize |   3.921 ms |  0.0652 ms |  0.0610 ms |   3.905 ms |   1.50 |    0.03 |  359.3750 |  304.6875 | 140.6250 | 1734.68 KB |        4.58 |
|      JsonDeserializeBinary |  10.058 ms |  0.3131 ms |  0.9230 ms |   9.666 ms |   4.35 |    0.21 | 1812.5000 | 1718.7500 | 984.3750 | 9749.45 KB |       25.76 |
|          FlexonDeserialize | 172.161 ms | 11.7901 ms | 32.8662 ms | 156.330 ms |  62.51 |    7.60 |         - |         - |        - |   77.24 KB |        0.20 |
|    FlexonDeserializeBinary | 140.518 ms |  3.5609 ms |  9.9264 ms | 137.629 ms |  52.53 |    4.04 |         - |         - |        - |   77.22 KB |        0.20 |
| FlexonDeserializeEncrypted | 183.508 ms |  5.8863 ms | 16.6023 ms | 181.705 ms |  72.16 |    6.09 |         - |         - |        - |   77.46 KB |        0.20 |
