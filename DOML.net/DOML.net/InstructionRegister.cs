#region License
// ====================================================
// Team DOML Copyright(C) 2017 Team DOML
// This program comes with ABSOLUTELY NO WARRANTY; This is free software, 
// and you are welcome to redistribute it under certain conditions; See 
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion

using System;
using System.Collections.Generic;
using DOML.Logger;
using System.Linq;
using System.Reflection;

namespace DOML.IR {
    public enum ParamType {
        INT = 0,
        FLT = 1,
        DEC = 2,
        STR = 3,
        BOOL = 4,
        OBJ = 5,
        MAP = 6,
        VEC = 7,
    }

    public enum FnType {
        CONSTRUCTOR,
        GETTER,
        SETTER,
    }

    public struct FunctionDefinition {
        public readonly string name;
        public readonly FnType type;
        public readonly Action<InterpreterRuntime, int> action;
        public readonly ParamType[] parameterTypes;

        public FunctionDefinition(string name, FnType type, Action<InterpreterRuntime, int> action, ParamType[] parameterTypes) {
            this.name = name;
            this.type = type;
            this.action = action;
            this.parameterTypes = parameterTypes;
        }
    }

    /// <summary>
    /// A register of all the instructions.
    /// </summary>
    public static class InstructionRegister {
        /// <summary>
        /// These represent the actions to run.
        /// </summary>
        public readonly static Dictionary<string, FunctionDefinition> Actions;

        public static FunctionDefinition GetSetter(string name) => GetAction("set::" + name);
        public static void RegisterSetter(string name, Action<InterpreterRuntime, int> func, ParamType[] parameterTypes) => RegisterAction("set::" + name, func, FnType.SETTER, parameterTypes);
        public static bool UnRegisterSetter(string name) => UnRegisterAction("set::" + name);

        public static FunctionDefinition GetGetter(string name) => GetAction("get::" + name);
        public static void RegisterGetter(string name, Action<InterpreterRuntime, int> func, ParamType[] parameterTypes) => RegisterAction("get::" + name, func, FnType.GETTER, parameterTypes);
        public static bool UnRegisterGetter(string name) => UnRegisterAction("get::" + name);

        public static FunctionDefinition GetConstructor(string name) => GetAction("ctor::" + name);
        public static void RegisterConstructor(string name, Action<InterpreterRuntime, int> func, ParamType[] parameterTypes) => RegisterAction("ctor::" + name, func, FnType.CONSTRUCTOR, parameterTypes);
        public static bool UnRegisterConstructor(string name) => UnRegisterAction("ctor::" + name);

        public static ParamType GetParamType(Type t) {
            if (t.IsArray) return ParamType.VEC;
            if (t == typeof(int) || t == typeof(short) || t == typeof(byte) || t == typeof(sbyte) || t == typeof(long) ||
                t == typeof(ulong) || t == typeof(uint) || t == typeof(ushort) || t == typeof(char)) {
                return ParamType.INT;
            }
            if (t == typeof(float) || t == typeof(double)) return ParamType.FLT;
            if (t == typeof(bool)) return ParamType.BOOL;
            if (t == typeof(decimal)) return ParamType.DEC;
            if (t == typeof(string)) return ParamType.STR;
            if (t.GetTypeInfo().IsGenericType) {
                if (t.GetGenericTypeDefinition() == typeof(Dictionary<,>)) return ParamType.MAP;
                if (t.GetGenericTypeDefinition() == typeof(List<>)) return ParamType.VEC;
            }
            return ParamType.OBJ;
        }

        public static FunctionDefinition GetAction(string name) {
            if (Actions.ContainsKey(name)) {
                return Actions[name];
            } else {
                Log.Error($"No action called {name}");
                return default(FunctionDefinition);
            }
        }

        /// <summary>
        /// Registers a constructor.
        /// </summary>
        /// <param name="name"> Function name. </param>
        /// <param name="func"> The function. </param>
        public static void RegisterAction(string name, Action<InterpreterRuntime, int> func, FnType type, ParamType[] parameterTypes) {
            if (Actions.ContainsKey(name) == false) {
                Actions.Add(name, new FunctionDefinition(name, type, func, parameterTypes));
            } else {
                Log.Warning($"Action already exists for {name}, will set to the one provided");
                Actions[name] = new FunctionDefinition(name, type, func, parameterTypes);
            }
        }

        /// <summary>
        /// Unregisters a constructor.
        /// </summary>
        /// <param name="name"> Function name. </param>
        public static bool UnRegisterAction(string name) => Actions.Remove(name);
    }
}
