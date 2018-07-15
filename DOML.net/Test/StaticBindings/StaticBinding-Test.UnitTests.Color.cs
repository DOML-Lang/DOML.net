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
		public static void NewColor(InterpreterRuntime runtime, int registerIndex)
		{
			if (!runtime.SetObject(new Test.UnitTests.Color(), registerIndex))
				Log.Error("Color Constructor failed");
		}

		public static void NewNormalized(InterpreterRuntime runtime, int registerIndex)
		{
			if (!runtime.Pop(out System.Single a3) || !runtime.Pop(out System.Single a2) || !runtime.Pop(out System.Single a1) || !runtime.SetObject(new Test.UnitTests.Color(a1,a2,a3), registerIndex))
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
			InstructionRegister.RegisterConstructor("Color::Color", NewColor, new ParamType[] {  });
			InstructionRegister.RegisterConstructor("Color::Normalized", NewNormalized, new ParamType[] { ParamType.FLT, ParamType.FLT, ParamType.FLT });
			InstructionRegister.RegisterGetter("Color::R", GetFieldR, new ParamType[0]);
			InstructionRegister.RegisterSetter("Color::R", SetFieldR, new ParamType[0]);
			InstructionRegister.RegisterGetter("Color::G", GetFieldG, new ParamType[0]);
			InstructionRegister.RegisterSetter("Color::G", SetFieldG, new ParamType[0]);
			InstructionRegister.RegisterGetter("Color::B", GetFieldB, new ParamType[0]);
			InstructionRegister.RegisterSetter("Color::B", SetFieldB, new ParamType[0]);
			InstructionRegister.RegisterGetter("Color::Name", GetFieldName, new ParamType[0]);
			InstructionRegister.RegisterSetter("Color::Name", SetFieldName, new ParamType[0]);
			InstructionRegister.RegisterSetter("Color::RGB", SetRGB, new ParamType[] { ParamType.INT, ParamType.INT, ParamType.INT });
			InstructionRegister.RegisterSetter("Color::Normalized", SetNormalized, new ParamType[] { ParamType.FLT, ParamType.FLT, ParamType.FLT });
			InstructionRegister.RegisterSetter("Color::Hex", SetHex, new ParamType[] { ParamType.INT });
			InstructionRegister.RegisterGetter("Color::Hex", GetHex, new ParamType[] {  });
			InstructionRegister.RegisterGetter("Color::Normalized", GetNormalized, new ParamType[] {  });
		}

		public static void UnRegisterCalls()
		{
			InstructionRegister.UnRegisterConstructor("Color::Color");
			InstructionRegister.UnRegisterConstructor("Color::Normalized");
			InstructionRegister.UnRegisterGetter("Color::R");
			InstructionRegister.UnRegisterSetter("Color::R");
			InstructionRegister.UnRegisterGetter("Color::G");
			InstructionRegister.UnRegisterSetter("Color::G");
			InstructionRegister.UnRegisterGetter("Color::B");
			InstructionRegister.UnRegisterSetter("Color::B");
			InstructionRegister.UnRegisterGetter("Color::Name");
			InstructionRegister.UnRegisterSetter("Color::Name");
			InstructionRegister.UnRegisterSetter("Color::RGB");
			InstructionRegister.UnRegisterSetter("Color::Normalized");
			InstructionRegister.UnRegisterSetter("Color::Hex");
			InstructionRegister.UnRegisterGetter("Color::Hex");
			InstructionRegister.UnRegisterGetter("Color::Normalized");
		}
	}
}
#pragma warning restore IDE0012 // Restore warning for shorter names
