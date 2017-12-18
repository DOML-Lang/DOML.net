# Data Oriented Markup Language - DOML (.Net)
This is the .net implementation for DOML (Data Oriented Markup Language), which is a 'new' markup language that takes a different approach then most.  It enacts to simulate a call-stack rather than simulate data structures, this allows it to represent a constructor like look rather than the usual bracketed `{...}` mess.

Note: Check out the proper [spec](https://github.com/DOML-DataOrientedMarkupLanguage/DOML-Spec) if you want to see what this language can do and how to use it, for the sake of simplicity I won't repeat myself here.  Furthermore this is 100% compliant with the current spec.

## Benchmarks

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

> WithConditions represents reading with/without commments, emitting with/without comments, and executing in either safe/unsafe mode
<img src="https://github.com/DOML-DataOrientedMarkupLanguage/DOML.net/blob/master/DOML.net/Test/BenchmarkDotNet.Artifacts/results/AllTests-barplot.png" width="500" height="500">

#### Takeaways
- Parsing is around 17us (or ~17,000ns)
    - With/Without comments makes a miniscle difference, this is mainly due to line comments being ignored with a readline whereas whitespace is done bit by bit, so the calls cancel out the cost of creating a new instruction and placing the string in.
- Emission is around 16us without comments, and 45us with comments
	- With comments is significantly slower due to the string manipulation that occurs, this could be optimised quite signficantly
	- Emission in general could also be optimised, but I'm more focusing on the reading currently
- Execution is 2.3us in unsafe mode, and 3.6us in safe mode.
	- Thus Execution is is around 1.5x slower when using safe mode.
	- Doubt it can be optimised too much more, due to the nature of it being relatively simple, though perhaps a nicer branching system could benefit the code.
- Reading IR without comments is around 7.2us and with comments is 5.7us
	- The difference can be attributed to the fact that reading without reads the entire line in, and that is less optimised then reading multiple lines, due to the fact it indexes and substrings from that; it also could be attributed to the memory cost associated, and the resulting cache misses.
	- In reality there is little difference, though it does raise questions of whether or not we could improve the compact code, and I think there is a lot of room for improvement there.
