#pragma warning disable IDE0012 // Simplify Names
/* THIS IS AUTO-GENERATED
 * ALL CHANGES WILL BE RESET
 * UPON GENERATION
 */
using DOML.Logger;
using DOML.IR;
namespace UserStaticBindings
{
    public class ____ColourStaticBindings____
    {
        public void ____GetRStaticBindings____(InterpreterRuntime runtime)
        {
            if (!runtime.Pop(out Test.UnitTests.Colour result) || !runtime.Push(result.R, true))
                Log.Error("Getter failed");
        }
        public void ____SetRStaticBindings____(InterpreterRuntime runtime)
        {
            if (runtime.Pop(out Test.UnitTests.Colour result) || !runtime.Pop(out result.R))
                Log.Error("Setter failed");
        }
        public void ____GetGStaticBindings____(InterpreterRuntime runtime)
        {
            if (!runtime.Pop(out Test.UnitTests.Colour result) || !runtime.Push(result.G, true))
                Log.Error("Getter failed");
        }
        public void ____SetGStaticBindings____(InterpreterRuntime runtime)
        {
            if (runtime.Pop(out Test.UnitTests.Colour result) || !runtime.Pop(out result.G))
                Log.Error("Setter failed");
        }
        public void ____GetBStaticBindings____(InterpreterRuntime runtime)
        {
            if (!runtime.Pop(out Test.UnitTests.Colour result) || !runtime.Push(result.B, true))
                Log.Error("Getter failed");
        }
        public void ____SetBStaticBindings____(InterpreterRuntime runtime)
        {
            if (runtime.Pop(out Test.UnitTests.Colour result) || !runtime.Pop(out result.B))
                Log.Error("Setter failed");
        }
        public void ____setSetRGBStaticBindings____(InterpreterRuntime runtime)
        {

            if (!runtime.Pop(out Test.UnitTests.Colour result) || !runtime.Pop(out System.Single a0) || !runtime.Pop(out System.Single a1) || !runtime.Pop(out System.Single a2))
                Log.Error("Function failed");
            else
                result.SetRGB(a0, a1, a2);
        }
        public void RegisterCalls()
        {
            InstructionRegister.RegisterGetter("R", "System.Colour", 1, ____GetRStaticBindings____);
            InstructionRegister.RegisterSetter("R", "System.Colour", 1, ____SetRStaticBindings____);
            InstructionRegister.RegisterGetter("G", "System.Colour", 1, ____GetGStaticBindings____);
            InstructionRegister.RegisterSetter("G", "System.Colour", 1, ____SetGStaticBindings____);
            InstructionRegister.RegisterGetter("B", "System.Colour", 1, ____GetBStaticBindings____);
            InstructionRegister.RegisterSetter("B", "System.Colour", 1, ____SetBStaticBindings____);
            InstructionRegister.RegisterSetter("SetRGB", "System.Colour", 3, ____setSetRGBStaticBindings____);
        }
        public void UnRegisterCalls()
        {
            InstructionRegister.UnRegisterGetter("R", "System.Colour");
            InstructionRegister.UnRegisterSetter("R", "System.Colour");
            InstructionRegister.UnRegisterGetter("G", "System.Colour");
            InstructionRegister.UnRegisterSetter("G", "System.Colour");
            InstructionRegister.UnRegisterGetter("B", "System.Colour");
            InstructionRegister.UnRegisterSetter("B", "System.Colour");
            InstructionRegister.UnRegisterSetter("SetRGB", "System.Colour");
        }
    }
}
#pragma warning restore IDE0012 // Simplify Names
