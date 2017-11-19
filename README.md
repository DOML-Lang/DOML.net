# Data Oriented Markup Language - DOML (.Net)
This is the .net implementation for DOML (Data Oriented Markup Language), which is a 'new' markup language that takes a different approach then most.  It enacts to simulate a call-stack rather than simulate data structures, this allows it to represent a constructor like look rather than the usual bracketed `{...}` mess.

Note: Check out the proper [spec](https://github.com/DOML-DataOrientedMarkupLanguage/DOML-Spec) if you want to see what this language can do and how to use it, for the sake of simplicity I won't repeat myself here.  Furthermore this is 100% compliant with the current spec.

## Benchmarks
``` ini

BenchmarkDotNet=v0.10.9, OS=Windows 10 Redstone 2 (10.0.15063)
Processor=Intel Core i5-4590 CPU 3.30GHz (Haswell), ProcessorCount=4
Frequency=3222661 Hz, Resolution=310.3026 ns, Timer=TSC
.NET Core SDK=1.1.0
  [Host]     : .NET Core 1.1.2 (Framework 4.6.25211.01), 64bit RyuJIT
  DefaultJob : .NET Core 1.1.2 (Framework 4.6.25211.01), 64bit RyuJIT


```
**Note: The times are in nanoseconds, divide by 1000 to get microseconds.  I.e. the parsing time is ~17ms or ~17,000ns
 |      Method | WithCondition |        Mean |      Error |      StdDev |
 |------------ |-------------- |------------:|-----------:|------------:|
 |   **ParseTest** |         **False** | **16,945.8 ns** | **125.820 ns** | **117.6918 ns** |
 |    EmitTest |         False | 15,358.0 ns | 100.696 ns |  89.2647 ns |
 | ExecuteTest |         False |  2,294.9 ns |   9.437 ns |   8.8275 ns |
 |      ReadIR |         False |    250.0 ns |   1.402 ns |   1.3118 ns |
 |   **ParseTest** |          **True** | **17,029.7 ns** |  **78.868 ns** |  **73.7734 ns** |
 |    EmitTest |          True | 44,286.0 ns | 175.621 ns | 146.6518 ns |
 | ExecuteTest |          True |  3,599.1 ns |   9.569 ns |   8.9511 ns |
 |      ReadIR |          True |    261.1 ns |   1.019 ns |   0.9035 ns |

> WithConditions represents reading with/without commments, emitting with/without comments, and executing in either safe/unsafe mode

#### Takeaways
- Parsing is around 17ms (or 17,000 ms)
    - With/Without comments makes a miniscle difference
- Emission is around 15ms without comments, and 44ms with comments
	- With comments is significantly slower due to the string manipulation that occurs, this could be optimised quite signficantly
	- Emission in general could also be optimised, but I'm more focusing on the reading currently
- Execution is 2.3ms in unsafe mode, and 3.6ms in safe mode.
	- Thus Execution is is around 1.5x slower when using safe mode.
	- Doubt it can be optimised too much more, due to the nature of it being relatively simple, though perhaps a nicer branching system could benefit the code.
- Reading IR without comments is around 0.25ms and with comments is 0.26ms, thus there is basically no real difference