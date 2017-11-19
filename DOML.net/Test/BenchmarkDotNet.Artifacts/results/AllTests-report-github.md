``` ini

BenchmarkDotNet=v0.10.9, OS=Windows 10 Redstone 2 (10.0.15063)
Processor=Intel Core i5-4590 CPU 3.30GHz (Haswell), ProcessorCount=4
Frequency=3222661 Hz, Resolution=310.3026 ns, Timer=TSC
.NET Core SDK=1.1.0
  [Host]     : .NET Core 1.1.2 (Framework 4.6.25211.01), 64bit RyuJIT DEBUG  [AttachedDebugger]
  DefaultJob : .NET Core 1.1.2 (Framework 4.6.25211.01), 64bit RyuJIT


```
 |         Method |     Mean |     Error |    StdDev |
 |--------------- |---------:|----------:|----------:|
 | ReadIRComments | 28.24 us | 0.3603 us | 0.3370 us |
 |         ReadIR | 12.07 us | 0.0480 us | 0.0400 us |
