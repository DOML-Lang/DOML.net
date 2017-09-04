using System;
using System.IO;
using DOML;
using DOML.ByteCode;
using DOML.Logger;

namespace ConsoleApp1
{
    namespace UnitTests
    {
        public struct Colour
        {
            public float R, G, B;
        }

        class Program
        {
            static void Main(string[] args)
            {
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

                InstructionRegister.RegisterInstruction("System.Color", InstructionRegister.RegisterType.NEW, (InterpreterRuntime runtime) =>
                {
                    if (!runtime.Push(new Colour(), true))
                    {
                        Log.Error("Creation failed");
                    }
                });

                InstructionRegister.RegisterInstruction("RGB", InstructionRegister.RegisterType.SET, Set_RGB);
                InstructionRegister.RegisterInstruction("RGB", InstructionRegister.RegisterType.GET, Get_RGB);
                InstructionRegister.RegisterInstruction("RGB.Normalised", InstructionRegister.RegisterType.GET, (InterpreterRuntime runtime) =>
                {
                    if (runtime.Pop(out Colour result))
                        if (!runtime.Pop(out result.B) || !runtime.Pop(out result.G) || !runtime.Pop(out result.R))
                            Log.Error("Pops failed");
                });

                Parser.GetInterpreterFromText(@"
            @ Test = System.Color ... // Comment
            ;      .RGB(Normalised) = 0.5, 0.25, 1f, ""This is a test string that is quite long I think overall it is very long lolol This is a test string that is quite long I think overall it is very long lolol This is a test string that is quite long I think overall it is very long lolol This is a test string that is quite long I think overall it is very long lolol""
            ").Emit(new ByteCodeWriter(Directory.GetCurrentDirectory() + "\\Test.odoml", false), true, true);

                Console.Read();
            }

            private static void Log_LogHandler(string message, DOML.Logger.Type type, bool useLineNumbers)
            {
                if (useLineNumbers)
                {
                    Console.WriteLine($"{type} at Line/Col: {Parser.CurrentLine}/{Parser.CurrentColumn}; {message}");
                }
                else
                {
                    Console.WriteLine(message + ": " + type);
                }
            }
        }
    }
}
