``` ini

BenchmarkDotNet=v0.10.9, OS=Windows 10 Redstone 2 (10.0.15063)
Processor=Intel Core i5-4590 CPU 3.30GHz (Haswell), ProcessorCount=4
Frequency=3222670 Hz, Resolution=310.3017 ns, Timer=TSC
.NET Core SDK=1.1.0
  [Host]     : .NET Core 1.1.2 (Framework 4.6.25211.01), 64bit RyuJIT
  DefaultJob : .NET Core 1.1.2 (Framework 4.6.25211.01), 64bit RyuJIT


```
 |      Method | WithCondition |      Mean |     Error |    StdDev |
 |------------ |-------------- |----------:|----------:|----------:|
 |   **ParseTest** |         **False** | **17.381 us** | **0.2962 us** | **0.2770 us** |
 |    EmitTest |         False | 16.078 us | 0.2121 us | 0.1984 us |
 | ExecuteTest |         False |  2.326 us | 0.0160 us | 0.0142 us |
 |      ReadIR |         False |  7.221 us | 0.0955 us | 0.0893 us |
 |   **ParseTest** |          **True** | **17.199 us** | **0.1040 us** | **0.0868 us** |
 |    EmitTest |          True | 44.816 us | 0.4070 us | 0.3807 us |
 | ExecuteTest |          True |  3.597 us | 0.0292 us | 0.0273 us |
 |      ReadIR |          True |  5.646 us | 0.0624 us | 0.0584 us |
