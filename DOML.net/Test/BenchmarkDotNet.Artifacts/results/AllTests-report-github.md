``` ini

BenchmarkDotNet=v0.10.9, OS=Windows 10 Redstone 2 (10.0.15063)
Processor=Intel Core i5-4590 CPU 3.30GHz (Haswell), ProcessorCount=4
Frequency=3222670 Hz, Resolution=310.3017 ns, Timer=TSC
.NET Core SDK=1.1.0
  [Host]     : .NET Core 1.1.2 (Framework 4.6.25211.01), 64bit RyuJIT  [AttachedDebugger]
  DefaultJob : .NET Core 1.1.2 (Framework 4.6.25211.01), 64bit RyuJIT


```
 |      Method | WithCondition |      Mean |     Error |    StdDev |
 |------------ |-------------- |----------:|----------:|----------:|
 |   **ParseTest** |         **False** | **17.603 us** | **0.0734 us** | **0.0686 us** |
 |    EmitTest |         False | 18.825 us | 0.1647 us | 0.1540 us |
 | ExecuteTest |         False |  2.346 us | 0.0098 us | 0.0091 us |
 |      ReadIR |         False |  7.329 us | 0.0214 us | 0.0200 us |
 |   **ParseTest** |          **True** | **17.598 us** | **0.1138 us** | **0.1064 us** |
 |    EmitTest |          True | 54.359 us | 0.5333 us | 0.4989 us |
 | ExecuteTest |          True |  3.600 us | 0.0200 us | 0.0177 us |
 |      ReadIR |          True |  5.798 us | 0.1063 us | 0.0994 us |
