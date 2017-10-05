#pragma warning disable IDE0012 // Ignorning warning for shorter names
/* THIS IS AUTO-GENERATED
 * ALL CHANGES WILL BE RESET
 * UPON GENERATION
 */

using DOML.Logger;
using DOML.IR;

namespace StaticBindings.SystemBindings
{
    public static partial class ____ColourStaticBindings____
    {
        public static void NewColourEmpty(InterpreterRuntime runtime)
        {
            if (!runtime.Push(new Test.UnitTests.Colour(), true))
                Log.Error("Colour Constructor failed");
        }

        public static void GetFieldR(InterpreterRuntime runtime)
        {
            if (!runtime.Pop(out Test.UnitTests.Colour result) || !runtime.Push(result.R, true))
                Log.Error("Colour::R Getter failed");
        }

        public static void SetFieldR(InterpreterRuntime runtime)
        {
            if (runtime.Pop(out Test.UnitTests.Colour result) || !runtime.Pop(out result.R))
                Log.Error("Colour::R Setter failed");
        }

        public static void GetFieldG(InterpreterRuntime runtime)
        {
            if (!runtime.Pop(out Test.UnitTests.Colour result) || !runtime.Push(result.G, true))
                Log.Error("Colour::G Getter failed");
        }

        public static void SetFieldG(InterpreterRuntime runtime)
        {
            if (runtime.Pop(out Test.UnitTests.Colour result) || !runtime.Pop(out result.G))
                Log.Error("Colour::G Setter failed");
        }

        public static void GetFieldB(InterpreterRuntime runtime)
        {
            if (!runtime.Pop(out Test.UnitTests.Colour result) || !runtime.Push(result.B, true))
                Log.Error("Colour::B Getter failed");
        }

        public static void SetFieldB(InterpreterRuntime runtime)
        {
            if (runtime.Pop(out Test.UnitTests.Colour result) || !runtime.Pop(out result.B))
                Log.Error("Colour::B Setter failed");
        }

        public static void SetSetRGB(InterpreterRuntime runtime)
        {
            if (!runtime.Pop(out Test.UnitTests.Colour result) || !runtime.Pop(out System.Single a0) || !runtime.Pop(out System.Single a1) || !runtime.Pop(out System.Single a2))
                Log.Error("Colour::SetRGB Function failed");
            else
                result.SetRGB(a0, a1, a2);
        }

        public static void RegisterCalls()
        {
            InstructionRegister.RegisterConstructor("StaticBindings.SystemBindings.Colour", NewColourEmpty);
            InstructionRegister.RegisterGetter("R", "StaticBindings.SystemBindings.Colour", 1, GetFieldR);
            InstructionRegister.RegisterSetter("R", "StaticBindings.SystemBindings.Colour", 1, SetFieldR);
            InstructionRegister.RegisterGetter("G", "StaticBindings.SystemBindings.Colour", 1, GetFieldG);
            InstructionRegister.RegisterSetter("G", "StaticBindings.SystemBindings.Colour", 1, SetFieldG);
            InstructionRegister.RegisterGetter("B", "StaticBindings.SystemBindings.Colour", 1, GetFieldB);
            InstructionRegister.RegisterSetter("B", "StaticBindings.SystemBindings.Colour", 1, SetFieldB);
            InstructionRegister.RegisterSetter("SetRGB", "StaticBindings.SystemBindings.Colour", 3, SetSetRGB);
        }

        public static void UnRegisterCalls()
        {
            InstructionRegister.UnRegisterConstructor("StaticBindings.SystemBindings.Colour");
            InstructionRegister.UnRegisterGetter("R", "StaticBindings.SystemBindings.Colour");
            InstructionRegister.UnRegisterSetter("R", "StaticBindings.SystemBindings.Colour");
            InstructionRegister.UnRegisterGetter("G", "StaticBindings.SystemBindings.Colour");
            InstructionRegister.UnRegisterSetter("G", "StaticBindings.SystemBindings.Colour");
            InstructionRegister.UnRegisterGetter("B", "StaticBindings.SystemBindings.Colour");
            InstructionRegister.UnRegisterSetter("B", "StaticBindings.SystemBindings.Colour");
            InstructionRegister.UnRegisterSetter("SetRGB", "StaticBindings.SystemBindings.Colour");
        }
    }
}
#pragma warning restore IDE0012 // Restore warning for shorter names
