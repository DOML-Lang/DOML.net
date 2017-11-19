#pragma warning disable IDE0012 // Ignorning warning for shorter names
/* THIS IS AUTO-GENERATED
 * ALL CHANGES WILL BE RESET
 * UPON GENERATION
 */

using DOML.Logger;
using DOML.IR;

namespace StaticBindings.BindingsForSystem
{
    public static partial class ____ColorStaticBindings____
    {
        public static void NewColorEmpty(InterpreterRuntime runtime)
        {
            if (!runtime.Push(new Test.UnitTests.Color(), true))
                Log.Error("Color Constructor failed");
        }

        public static void GetFieldR(InterpreterRuntime runtime)
        {
            if (!runtime.Pop(out Test.UnitTests.Color result) || !runtime.Push(result.R, true))
                Log.Error("Color::R Getter failed");
        }

        public static void SetFieldR(InterpreterRuntime runtime)
        {
            if (!runtime.Pop(out Test.UnitTests.Color result) || !runtime.Pop(out result.R))
                Log.Error("Color::R Setter failed");
        }

        public static void GetFieldG(InterpreterRuntime runtime)
        {
            if (!runtime.Pop(out Test.UnitTests.Color result) || !runtime.Push(result.G, true))
                Log.Error("Color::G Getter failed");
        }

        public static void SetFieldG(InterpreterRuntime runtime)
        {
            if (!runtime.Pop(out Test.UnitTests.Color result) || !runtime.Pop(out result.G))
                Log.Error("Color::G Setter failed");
        }

        public static void GetFieldB(InterpreterRuntime runtime)
        {
            if (!runtime.Pop(out Test.UnitTests.Color result) || !runtime.Push(result.B, true))
                Log.Error("Color::B Getter failed");
        }

        public static void SetFieldB(InterpreterRuntime runtime)
        {
            if (!runtime.Pop(out Test.UnitTests.Color result) || !runtime.Pop(out result.B))
                Log.Error("Color::B Setter failed");
        }

        public static void GetFieldName(InterpreterRuntime runtime)
        {
            if (!runtime.Pop(out Test.UnitTests.Color result) || !runtime.Push(result.Name, true))
                Log.Error("Color::Name Getter failed");
        }

        public static void SetFieldName(InterpreterRuntime runtime)
        {
            if (!runtime.Pop(out Test.UnitTests.Color result) || !runtime.Pop(out result.Name))
                Log.Error("Color::Name Setter failed");
        }

        public static void SetRGB(InterpreterRuntime runtime)
        {
            if (!runtime.Pop(out Test.UnitTests.Color result) || !runtime.Pop(out System.Single a3) || !runtime.Pop(out System.Single a2) || !runtime.Pop(out System.Single a1))
                Log.Error("Color::RGB Function failed");
            else
                result.RGB(a1, a2, a3);
        }

        public static void SetRGBNormalised(InterpreterRuntime runtime)
        {
            if (!runtime.Pop(out Test.UnitTests.Color result) || !runtime.Pop(out System.Single a3) || !runtime.Pop(out System.Single a2) || !runtime.Pop(out System.Single a1))
                Log.Error("Color::RGBNormalised Function failed");
            else
                result.RGBNormalised(a1, a2, a3);
        }

        public static void SetRGBHex(InterpreterRuntime runtime)
        {
            if (!runtime.Pop(out Test.UnitTests.Color result) || !runtime.Pop(out System.Int32 a1))
                Log.Error("Color::RGBHex Function failed");
            else
                result.RGBHex(a1);
        }

        public static void RegisterCalls()
        {
            InstructionRegister.RegisterConstructor("System.Color", NewColorEmpty);
            InstructionRegister.RegisterGetter("R", "System.Color", 1, GetFieldR);
            InstructionRegister.RegisterSetter("R", "System.Color", 1, SetFieldR);
            InstructionRegister.RegisterGetter("G", "System.Color", 1, GetFieldG);
            InstructionRegister.RegisterSetter("G", "System.Color", 1, SetFieldG);
            InstructionRegister.RegisterGetter("B", "System.Color", 1, GetFieldB);
            InstructionRegister.RegisterSetter("B", "System.Color", 1, SetFieldB);
            InstructionRegister.RegisterGetter("Name", "System.Color", 1, GetFieldName);
            InstructionRegister.RegisterSetter("Name", "System.Color", 1, SetFieldName);
            InstructionRegister.RegisterSetter("RGB", "System.Color", 3, SetRGB);
            InstructionRegister.RegisterSetter("RGB.Normalised", "System.Color", 3, SetRGBNormalised);
            InstructionRegister.RegisterSetter("RGB.Hex", "System.Color", 1, SetRGBHex);
        }

        public static void UnRegisterCalls()
        {
            InstructionRegister.UnRegisterConstructor("System.Color");
            InstructionRegister.UnRegisterGetter("R", "System.Color");
            InstructionRegister.UnRegisterSetter("R", "System.Color");
            InstructionRegister.UnRegisterGetter("G", "System.Color");
            InstructionRegister.UnRegisterSetter("G", "System.Color");
            InstructionRegister.UnRegisterGetter("B", "System.Color");
            InstructionRegister.UnRegisterSetter("B", "System.Color");
            InstructionRegister.UnRegisterGetter("Name", "System.Color");
            InstructionRegister.UnRegisterSetter("Name", "System.Color");
            InstructionRegister.UnRegisterSetter("RGB", "System.Color");
            InstructionRegister.UnRegisterSetter("RGB.Normalised", "System.Color");
            InstructionRegister.UnRegisterSetter("RGB.Hex", "System.Color");
        }
    }
}
#pragma warning restore IDE0012 // Restore warning for shorter names
