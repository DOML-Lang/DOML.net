#region License
// ====================================================
// Team DOML Copyright(C) 2017 Team DOML
// This program comes with ABSOLUTELY NO WARRANTY; This is free software, 
// and you are welcome to redistribute it under certain conditions; See 
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion
#define TestBindings // StaticBindings // TestBindings // BenchmarkDotNet (the last one is just till they update theirs to support standard 1.3)

using System;
using System.Collections.Generic;
using System.IO;
using DOML;
using DOML.IR;
using DOML.Logger;
using DOML.Test;
using StaticBindings;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Reports;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using BenchmarkDotNet.Attributes.Exporters;
using System.Reflection;
using System.Linq;

namespace Test.UnitTests
{
    public class Color
    {
        public float R, G, B;
        public string Name;

        public void RGB(float R, float G, float B)
        {
            this.R = R / 255;
            this.G = G / 255;
            this.B = B / 255;
        }

        [DOMLCustomise("RGB.Normalised")]
        public void RGBNormalised(float R, float G, float B)
        {
            this.R = R;
            this.G = G;
            this.B = B;
        }

        [DOMLCustomise("RGB.Hex")]
        public void RGBHex(int hex)
        {
            int r = ((hex & 0xff0000) >> 16) / 255;
            int g = ((hex & 0xff00) >> 8) / 255;
            int b = (hex & 0xff) / 255;
        }
    }

    [RPlotExporter]
    public class AllTests
    {
        public Interpreter baseInterpreter;

        public string IRWithComments;
        public string IRWithoutComments;

        //[Params(true, false)]
        public bool WithCondition;

        public AllTests()
        {
            DOMLBindings.LinkBindings();
            Log.HandleLogs = false;

            baseInterpreter = Parser.GetInterpreterFromText(@"
            // This is a comment
            // Construct a new System.Color
            @ Test        = System.Color ...
            ;             .RGB             = 255, 64, 128 // Implicit 'array'

            @ TheSame     = System.Color ...
            ;             .RGB->Normalised = 1, 0.25, 0.5, // You can have trailing commas

            @ AgainSame   = System.Color ...
            ;             .RGB->Hex        = 0xFF4080
            ;             .Name            = ""OtherName""

            /* Multi Line Comment Blocks are great */
            @ Copy = System.Color...
            ;             .RGB = Test.R, Test.G, Test.B
            ;             .Name = ""Copy""
            ", Parser.ReadMode.DOML);

            StringBuilder IRWithComments = new StringBuilder(), IRWithoutComments = new StringBuilder();

            IRWriter.EmitToString(baseInterpreter, IRWithComments, true);
            IRWriter.EmitToString(baseInterpreter, IRWithoutComments, false);

            this.IRWithComments = IRWithComments.ToString();
            this.IRWithoutComments = IRWithoutComments.ToString();
        }

        //[Benchmark]
        public Interpreter ParseTest()
        {
            return Parser.GetInterpreterFromText(@"
            // This is a comment
            // Construct a new System.Color
            @ Test        = System.Color ...
            ;             .RGB             = 255, 64, 128 // Implicit 'array'

            @ TheSame     = System.Color ...
            ;             .RGB->Normalised = 1, 0.25, 0.5, // You can have trailing commas

            @ AgainSame   = System.Color ...
            ;             .RGB->Hex        = 0xFF4080
            ;             .Name            = ""OtherName""

            /* Multi Line Comment Blocks are great */
            @ Copy = System.Color...
            ;             .RGB = Test.R, Test.G, Test.B
            ;             .Name = ""Copy""
            ", Parser.ReadMode.DOML);
        }

        //   [Benchmark]
        public void EmitTest()
        {
            StringBuilder builder = new StringBuilder();
            IRWriter.EmitToString(baseInterpreter, builder, WithCondition);
        }

        //   [Benchmark]
        public void ExecuteTest()
        {
            baseInterpreter.Execute(WithCondition);
        }

        [Benchmark]
        public Interpreter ReadIRComments()
        {
            using (StringReader reader = new StringReader(IRWithComments))
                return Parser.GetInterpreterFromIR(reader);
        }

        [Benchmark]
        public Interpreter ReadIR()
        {
            using (StringReader reader = new StringReader(IRWithoutComments))
                return Parser.GetInterpreterFromIR(reader);
        }
    }

    public class Program
    {
        static Program()
        {
        }

        public static void Main(string[] args)
        {
            Console.SetWindowSize((int)(Console.LargestWindowWidth / 1.5f), (int)(Console.LargestWindowHeight / 1.5f));
#if BenchmarkDotNet
            Summary summary = BenchmarkRunner.Run<AllTests>();
            Console.Read();
#elif StaticBindings
            if (Directory.Exists(Directory.GetCurrentDirectory() + "/StaticBindings") == false)
                Directory.CreateDirectory(Directory.GetCurrentDirectory() + "/StaticBindings");

            CreateBindings.DirectoryPath = Directory.GetCurrentDirectory() + "/StaticBindings";
            CreateBindings.Create<Color>("System");
            CreateBindings.CreateFinalFile();
#elif TestBindings
            DOMLBindings.LinkBindings();


            Interpreter interpreter = Parser.GetInterpreterFromText(@"
            // This is a comment
            // Construct a new System.Color
            @ Test        = System.Color ...
            ;             .RGB             = 255, 64, 128 // Implicit 'array'

            @ TheSame     = System.Color ...
            ;             .RGB->Normalised = 1, 0.25, 0.5, // You can have trailing commas

            @ AgainSame   = System.Color ...
            ;             .RGB->Hex        = 0xFF4080
            ;             .Name            = ""OtherName""

            /* Multi Line Comment Blocks are great */
            @ Copy = System.Color...
            ;             .RGB = Test.R, Test.G, Test.B
            ;             .Name = ""Copy""
            ");

            string withComments;
            string withoutComments;

            StringBuilder builder = new StringBuilder();

            IRWriter.EmitToString(interpreter, builder, true);
            withComments = builder.ToString();
            builder.Clear();
            IRWriter.EmitToString(interpreter, builder, false);
            withoutComments = builder.ToString();

            System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            long count1 = 0;
            long count2 = 0;
            Interpreter c;

            for (int i = 0; i < 10000; i++)
            {
                stopwatch.Restart();
                using (StringReader reader = new StringReader(withComments))
                {
                    c = Parser.GetInterpreterFromIR(reader);
                    stopwatch.Stop();
                    c.Execute(true);
                }
                count1 += stopwatch.ElapsedTicks;

                stopwatch.Restart();
                using (StringReader reader = new StringReader(withoutComments))
                {
                    c = Parser.GetInterpreterFromIR(reader);
                    stopwatch.Stop();
                    c.Execute(true);
                }
                count2 += stopwatch.ElapsedTicks;
            }

            Console.WriteLine(count2 / (10000 * (System.Diagnostics.Stopwatch.Frequency / 1 * 10e-6)));
            Console.WriteLine(count1 / (100000));
            Console.WriteLine(count2 / (100000));

            Console.Read();

            IRWriter.EmitToLocation(interpreter, Directory.GetCurrentDirectory() + "/CompactOutput.IR", false, false);
            IRWriter.EmitToLocation(interpreter, Directory.GetCurrentDirectory() + "/Output.IR", false, true);

            return;

            TestDOML.RunStringTest(@"
            // This is a comment
            // Construct a new System.Color
            @ Test        = System.Color ...
            ;             .RGB             = 255, 64, 128 // Implicit 'array'

            @ TheSame     = System.Color ...
            ;             .RGB->Normalised = 1, 0.25, 0.5, // You can have trailing commas

            @ AgainSame   = System.Color ...
            ;             .RGB->Hex        = 0xFF4080
            ;             .Name            = ""OtherName""

            /* Multi Line Comment Blocks are great */
            @ Copy = System.Color...
            ;             .RGB = Test.R, Test.G, Test.B
            ;             .Name = ""Copy""
            ", 5, Config.READ_EMIT | Config.READ_EMIT_COMMENT);

            Console.Write(Log.HandleLogs);

            Console.Read();
#else
            Log.LogHandler += Log_LogHandler;

            Action<InterpreterRuntime> Set_RGB = (InterpreterRuntime runtime) =>
                {
                    if (runtime.Pop(out Colour result))
                    {
                        // Handle
                        if (!runtime.Pop(out result.B) || !runtime.Pop(out result.G) || !runtime.Pop(out result.R))
                        {
                            Log.Error("Pops failed");
                            return;
                        }

                        result.R /= 255;
                        result.G /= 255;
                        result.B /= 255;
                    }
                };

            Action<InterpreterRuntime> Get_RGB = (InterpreterRuntime runtime) =>
                {
                    if (!runtime.Pop(out Colour result) || !runtime.Push(result.R, true) || !runtime.Push(result.G, true) || !runtime.Push(result.B, true))
                    {
                        Log.Error("Pushes failed");
                    }
                };

            InstructionRegister.RegisterConstructor("System.Color", (InterpreterRuntime runtime) =>
                        {
                            Colour colour = new Colour();
                            Colours.Add(colour);
                            if (!runtime.Push(colour, true))
                            {
                                Log.Error("Creation failed");
                            }
                        });

            InstructionRegister.RegisterSetter("RGB", "System.Color", 3, Set_RGB);
            InstructionRegister.RegisterGetter("RGB", "System.Color", 3, Get_RGB);
            InstructionRegister.RegisterSetter("RGB.Normalised", "System.Color", 3, (InterpreterRuntime runtime) =>
            {
                if (runtime.Pop(out Colour result))
                    if (!runtime.Pop(out result.B) || !runtime.Pop(out result.G) || !runtime.Pop(out result.R))
                        Log.Error("Pops failed");
            });

            Parser.GetInterpreterFromText(@"
                @ Test = System.Color ... // Comment
                ;      .RGB(Normalised) = 0.5, 0.25, 0.1,
                ").Execute(true);//Emit(Directory.GetCurrentDirectory() + "/Test.doml", true, true);

            /*TestDOML.RunStringTest(@"
            @ Test = System.Color ... // Comment
            ;      .RGB(Normalised) = 0.5, 0.25, 0.1,
            ", 100, Config.ALL);*/

            Colours.ForEach(y => Log.Info($"Colour RGB: {y.R} {y.G} {y.B}"));

            Console.Read();
#endif
        }
    }
}
