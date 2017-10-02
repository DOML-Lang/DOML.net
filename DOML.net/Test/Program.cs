#region License
// ====================================================
// Team DOML Copyright(C) 2017 Team DOML
// This program comes with ABSOLUTELY NO WARRANTY; This is free software, 
// and you are welcome to redistribute it under certain conditions; See 
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion
//#define StaticTest

using System;
using System.Collections.Generic;
using System.IO;
using DOML;
using DOML.IR;
using DOML.Logger;
using StaticBindings;

namespace Test.UnitTests
{
    public class Colour
    {
        public float R, G, B;

        public void SetRGB(float R, float G, float B)
        {
            this.R = R;
            this.G = G;
            this.B = B;
        }
    }

    public class Program
    {
        private static List<Colour> Colours = new List<Colour>();

        public static void Main(string[] args)
        {
            Console.SetWindowSize((int)(Console.LargestWindowWidth / 1.5f), (int)(Console.LargestWindowHeight / 1.5f));

#if StaticTest
            if (Directory.Exists(Directory.GetCurrentDirectory() + "/StaticBindings") == false)
                Directory.CreateDirectory(Directory.GetCurrentDirectory() + "/StaticBindings");

            CreateBindings.DirectoryPath = Directory.GetCurrentDirectory() + "/StaticBindings";
            CreateBindings.Create<Colour>("System");
            CreateBindings.CreateFinalFile();
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

        private static void Log_LogHandler(string message, Log.Type type, bool useLineNumbers)
        {
            if (useLineNumbers)
            {
                Console.WriteLine($"{type} at Line/Col: {Parser.CurrentLine}/{Parser.CurrentColumn}; {message}");
            }
            else
            {
                Console.WriteLine(type + ": " + message);
            }
        }
    }
}
