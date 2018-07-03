#pragma warning disable IDE0012 // Ignorning warning for shorter names
/* THIS IS AUTO-GENERATED
 * ALL CHANGES WILL BE RESET
 * UPON GENERATION
 */

using DOML.Logger;
using DOML.IR;

namespace StaticBindings
{
	public static partial class ____ColorStaticBindings____
	{
		public static void NewColorEmpty(InterpreterRuntime runtime, int registerIndex)
		{
			if (!runtime.SetObject(new Test.UnitTests.Color(), registerIndex))
				Log.Error("Color Constructor failed");
		}

		public static void GetFieldR(InterpreterRuntime runtime, int registerIndex)
		{
			if (!runtime.GetObject(registerIndex, out Test.UnitTests.Color result) || !runtime.Push(result.R, true))
				Log.Error("Color::R Getter failed");
		}

		public static void SetFieldR(InterpreterRuntime runtime, int registerIndex)
		{
			if (!runtime.GetObject(registerIndex, out Test.UnitTests.Color result) || !runtime.Pop(out result.R))
				Log.Error("Color::R Setter failed");
		}

		public static void GetFieldG(InterpreterRuntime runtime, int registerIndex)
		{
			if (!runtime.GetObject(registerIndex, out Test.UnitTests.Color result) || !runtime.Push(result.G, true))
				Log.Error("Color::G Getter failed");
		}

		public static void SetFieldG(InterpreterRuntime runtime, int registerIndex)
		{
			if (!runtime.GetObject(registerIndex, out Test.UnitTests.Color result) || !runtime.Pop(out result.G))
				Log.Error("Color::G Setter failed");
		}

		public static void GetFieldB(InterpreterRuntime runtime, int registerIndex)
		{
			if (!runtime.GetObject(registerIndex, out Test.UnitTests.Color result) || !runtime.Push(result.B, true))
				Log.Error("Color::B Getter failed");
		}

		public static void SetFieldB(InterpreterRuntime runtime, int registerIndex)
		{
			if (!runtime.GetObject(registerIndex, out Test.UnitTests.Color result) || !runtime.Pop(out result.B))
				Log.Error("Color::B Setter failed");
		}

		public static void GetFieldName(InterpreterRuntime runtime, int registerIndex)
		{
			if (!runtime.GetObject(registerIndex, out Test.UnitTests.Color result) || !runtime.Push(result.Name, true))
				Log.Error("Color::Name Getter failed");
		}

		public static void SetFieldName(InterpreterRuntime runtime, int registerIndex)
		{
			if (!runtime.GetObject(registerIndex, out Test.UnitTests.Color result) || !runtime.Pop(out result.Name))
				Log.Error("Color::Name Setter failed");
		}

		public static void SetRGB(InterpreterRuntime runtime, int registerIndex)
		{
			if (!runtime.GetObject(registerIndex, out Test.UnitTests.Color result) || !runtime.Pop(out System.Int32 a3) || !runtime.Pop(out System.Int32 a2) || !runtime.Pop(out System.Int32 a1))
				Log.Error("Color::RGB Function failed");
			else
				result.RGB(a1, a2, a3);
		}

		public static void SetNormalized(InterpreterRuntime runtime, int registerIndex)
		{
			if (!runtime.GetObject(registerIndex, out Test.UnitTests.Color result) || !runtime.Pop(out System.Single a3) || !runtime.Pop(out System.Single a2) || !runtime.Pop(out System.Single a1))
				Log.Error("Color::Normalized Function failed");
			else
				result.Normalized(a1, a2, a3);
		}

		public static void SetHex(InterpreterRuntime runtime, int registerIndex)
		{
			if (!runtime.GetObject(registerIndex, out Test.UnitTests.Color result) || !runtime.Pop(out System.Int32 a1))
				Log.Error("Color::Hex Function failed");
			else
				result.Hex(a1);
		}

		public static void GetHex(InterpreterRuntime runtime, int registerIndex)
		{
			if (!runtime.GetObject(registerIndex, out Test.UnitTests.Color result) || !runtime.Push(result.Hex(), true))
				Log.Error("Color::Hex Function failed");
		}

		public static void GetNormalized(InterpreterRuntime runtime, int registerIndex)
		{
			if (!runtime.GetObject(registerIndex, out Test.UnitTests.Color result) || !runtime.Push(result.Normalized(), true))
				Log.Error("Color::Normalized Function failed");
		}

		public static void RegisterCalls()
		{
			InstructionRegister.RegisterConstructor("Color", NewColorEmpty, new ParamType[] {  });
			InstructionRegister.RegisterGetter("R::Color", GetFieldR, new ParamType[0]);
			InstructionRegister.RegisterSetter("R::Color", SetFieldR, new ParamType[0]);
			InstructionRegister.RegisterGetter("G::Color", GetFieldG, new ParamType[0]);
			InstructionRegister.RegisterSetter("G::Color", SetFieldG, new ParamType[0]);
			InstructionRegister.RegisterGetter("B::Color", GetFieldB, new ParamType[0]);
			InstructionRegister.RegisterSetter("B::Color", SetFieldB, new ParamType[0]);
			InstructionRegister.RegisterGetter("Name::Color", GetFieldName, new ParamType[0]);
			InstructionRegister.RegisterSetter("Name::Color", SetFieldName, new ParamType[0]);
			InstructionRegister.RegisterSetter("RGB::Color", SetRGB, new ParamType[] { ParamType.INT, ParamType.INT, ParamType.INT });
			InstructionRegister.RegisterSetter("Normalized::Color", SetNormalized, new ParamType[] { ParamType.FLT, ParamType.FLT, ParamType.FLT });
			InstructionRegister.RegisterSetter("Hex::Color", SetHex, new ParamType[] { ParamType.INT });
			InstructionRegister.RegisterGetter("Hex::Color", GetHex, new ParamType[] {  });
			InstructionRegister.RegisterGetter("Normalized::Color", GetNormalized, new ParamType[] {  });
		}

		public static void UnRegisterCalls()
		{
			InstructionRegister.UnRegisterConstructor("Color");
			InstructionRegister.UnRegisterGetter("R::Color");
			InstructionRegister.UnRegisterSetter("R::Color");
			InstructionRegister.UnRegisterGetter("G::Color");
			InstructionRegister.UnRegisterSetter("G::Color");
			InstructionRegister.UnRegisterGetter("B::Color");
			InstructionRegister.UnRegisterSetter("B::Color");
			InstructionRegister.UnRegisterGetter("Name::Color");
			InstructionRegister.UnRegisterSetter("Name::Color");
			InstructionRegister.UnRegisterSetter("RGB::Color");
			InstructionRegister.UnRegisterSetter("Normalized::Color");
			InstructionRegister.UnRegisterSetter("Hex::Color");
			InstructionRegister.UnRegisterGetter("Hex::Color");
			InstructionRegister.UnRegisterGetter("Normalized::Color");
		}
	}
}
#pragma warning restore IDE0012 // Restore warning for shorter names
