#region License
// ====================================================
// Team DOML Copyright(C) 2017 Team DOML
// This program comes with ABSOLUTELY NO WARRANTY; This is free software, 
// and you are welcome to redistribute it under certain conditions; See 
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion
#define BenchmarkDotNet // StaticBindings // TestBindings // BenchmarkDotNet (the last one is just till they update theirs to support standard 1.3)

using System;
using System.Collections.Generic;
using System.IO;
using DOML;
using DOML.AST;
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

namespace Test.UnitTests {
    public class Color {
        public byte R, G, B;
        public string Name;

        public Color() { }

        [DOMLCustomise("Normalized")]
        public Color(float R, float G, float B) {
            Normalized(R, G, B);
        }

        public void RGB(int R, int G, int B) {
            this.R = (byte)R;
            this.G = (byte)G;
            this.B = (byte)B;
        }

        public void Normalized(float R, float G, float B) {
            this.R = (byte)(R * 255);
            this.G = (byte)(G * 255);
            this.B = (byte)(B * 255);
        }

        public void Hex(int hex) {
            int r = (hex & 0xff0000) >> 16;
            int g = (hex & 0xff00) >> 8;
            int b = hex & 0xff;
        }

        public int Hex() => (R << (byte)16) | (G << (byte)8) | B;
        public (float, float, float) Normalized() => (R / 255, G / 255, B / 255);
    }

    [RPlotExporter]
    public class AllTests {
        public Interpreter baseInterpreter;
        public Parser parser;

        public string IRWithComments;
        public string IRWithoutComments;

        [Params(true, false)]
        public bool WithCondition;

        public AllTests() {
            //DOMLBindings.LinkBindings();
            Log.HandleLogs = false;

            //StringBuilder comments = new StringBuilder(), noComments = new StringBuilder();

            //IRWriter.EmitToString(baseInterpreter, comments, true);
            //IRWriter.EmitToString(baseInterpreter, noComments, false);

            //this.IRWithComments = IRWithComments.ToString();
            //this.IRWithoutComments = IRWithoutComments.ToString();
        }

        [Benchmark]
        public Interpreter ParseTest() {
            return null;
        }

        [Benchmark]
        public void EmitTest() {
            StringBuilder builder = new StringBuilder();
            IRWriter.EmitToString(baseInterpreter, builder, WithCondition);
        }

        [Benchmark]
        public void ExecuteTest() {
            baseInterpreter.Execute(WithCondition);
        }

        [Benchmark]
        public Interpreter ReadIR() {
            return null;
            // using (StringReader reader = new StringReader(WithCondition ? IRWithComments : IRWithoutComments))
            //   return Parser.GetInterpreterFromIR(reader);
        }
    }

    public class Program {
        static Program() {
        }

        public static void Main(string[] args) {
            if (Directory.Exists(Directory.GetCurrentDirectory() + "/StaticBindings") == false)
                Directory.CreateDirectory(Directory.GetCurrentDirectory() + "/StaticBindings");

            CreateBindings.DirectoryPath = Directory.GetCurrentDirectory() + "/StaticBindings";
            CreateBindings.Create<Color>();
            CreateBindings.CreateFinalFile();

            DOMLBindings.LinkBindings();
            Console.SetWindowSize((int)(Console.LargestWindowWidth / 1.5f), (int)(Console.LargestWindowHeight / 1.5f));
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            using (StreamReader reader = new StreamReader(File.OpenRead("test.doml"))) {
                int c = reader.Read();
                while (c > 0) {
                    Console.Write((char)c);
                    c = reader.Read();
                }
            }
            TopLevelNode topLevel = new Parser(new StreamReader(File.OpenRead("test.doml"))).ParseAST();

            topLevel.BasicCodegen(Console.Out, false);
            topLevel.BasicCodegen(Console.Out, true);
            foreach (Instruction instruction in topLevel.GetInstructions()) {
                Console.Write(instruction.OpCode);
                foreach (object obj in instruction.Parameters) {
                    Console.Write(" " + obj);
                }
                Console.WriteLine();
            }

            topLevel.Print(Console.Out);
            using (TextWriter writer = new StreamWriter(File.OpenWrite(Directory.GetCurrentDirectory() + "/Output.IR"))) {
                topLevel.BasicCodegen(writer, true);
            }
            using (TextWriter writer = new StreamWriter(File.OpenWrite(Directory.GetCurrentDirectory() + "/CompactOutput.IR"))) {
                topLevel.BasicCodegen(writer, false);
            }

            int count = 0;
            Console.WriteLine(Interpreter.CreateType(new ParamType[] { ParamType.VEC, ParamType.INT }.GetEnumerator(), ref count));
            Console.WriteLine(Interpreter.CreateType(new ParamType[] { ParamType.MAP, ParamType.VEC, ParamType.FLT, ParamType.STR }.GetEnumerator(), ref count));
            Console.WriteLine(Interpreter.CreateType(new ParamType[] { ParamType.MAP, ParamType.FLT, ParamType.STR }.GetEnumerator(), ref count));
            Console.WriteLine(Interpreter.CreateType(new ParamType[] { ParamType.MAP, ParamType.VEC, ParamType.BOOL, ParamType.MAP, ParamType.OBJ, ParamType.VEC, ParamType.FLT }.GetEnumerator(), ref count));

            Console.Read();
            return;
#if BenchmarkDotNet
            //Summary summary = BenchmarkRunner.Run<AllTests>();
            //Console.Read();
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

            IRWriter.EmitToLocation(interpreter, Directory.GetCurrentDirectory() + "/CompactOutput.IR", false, false);
            IRWriter.EmitToLocation(interpreter, Directory.GetCurrentDirectory() + "/Output.IR", false, true);

            StringBuilder builder = new StringBuilder();

            IRWriter.EmitToString(interpreter, builder, true);
            withComments = builder.ToString();
            builder.Clear();
            IRWriter.EmitToString(interpreter, builder, false);
            withoutComments = builder.ToString();

            Interpreter c;

            using (StringReader reader = new StringReader(withComments))
            {
                c = Parser.GetInterpreterFromIR(reader);
                c.HandleSafeInstruction(new Instruction());
            }

            using (StringReader reader = new StringReader(withoutComments))
            {
                c = Parser.GetInterpreterFromIR(reader);
                c.HandleSafeInstruction(new Instruction());
            }

            Console.Read();

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
            ", 5, Config.READ_EMIT);

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
