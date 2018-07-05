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
using System.Linq;
using System.Reflection;
using DOML.IR;
using DOML.Logger;

namespace ReflectionBindings {
    /// <summary>
    /// Create reflected bindings.
    /// Not as fast as static but can be done at runtime.
    /// </summary>
    public static class CreateBindings {
        /// <summary>
        /// Create from a given class type.
        /// </summary>
        /// <typeparam name="T"> The class to generate from. </typeparam>
        public static void Create<T>() => Create(typeof(T));

        private static void RegisterConstructor(ConstructorInfo info, Type forClass) {
            ParameterInfo[] parameterInfo = info.GetParameters();

            InstructionRegister.RegisterConstructor($"{forClass.Name}", (InterpreterRuntime runtime, int registerIndex) => {
                if (runtime.GetObject(registerIndex, out object result)) {
                    if (parameterInfo.Length > 0) {
                        object[] objects = new object[parameterInfo.Length];
                        for (int i = 0; i < parameterInfo.Length; i++)
                            if (!runtime.Pop(out objects[parameterInfo.Length - 1 - i]))
                                break;

                        // Since it will break but not assign objects[i] if the pop goes wrong we can just check if it has set it
                        if (objects[objects.Length - 1] != null) {
                            if (runtime.Push(info.Invoke(result, objects), true)) {
                                return;
                            }
                        }
                    } else if (runtime.Push(info.Invoke(result, null), true)) {
                        return;
                    }
                }
                Log.Error($"{forClass.Name} Constructor failed");
            }, parameterInfo.Select(x => InstructionRegister.GetParamType(x.ParameterType)).ToArray());
        }

        private static void RegisterField(FieldInfo info, Type forClass) {
            InstructionRegister.RegisterGetter(info.Name + "::" + forClass.Name, (InterpreterRuntime runtime, int registerIndex) => {
                if (!runtime.GetObject(registerIndex, out object result) || !runtime.Push(info.GetValue(result), true))
                    Log.Error($"{forClass.Name}::{info.Name} Getter Failed");
            }, new ParamType[0]);

            InstructionRegister.RegisterSetter(info.Name + "::" + forClass.Name, (InterpreterRuntime runtime, int registerIndex) => {
                if (!runtime.GetObject(registerIndex, out object result) || !runtime.Pop(out object valueToSet))
                    Log.Error($"{forClass.Name}::{info.Name} Setter Failed");
                else
                    info.SetValue(result, valueToSet);
            }, new ParamType[0]);
        }

        private static void RegisterProperty(PropertyInfo info, Type forClass) {
            if (info.CanRead && (info.GetMethod?.IsPublic ?? false)) {
                InstructionRegister.RegisterGetter(info.Name + "::" + forClass.Name, (InterpreterRuntime runtime, int registerIndex) => {
                    if (!runtime.GetObject(registerIndex, out object result) || !runtime.Push(info.GetValue(result), true))
                        Log.Error($"{forClass.Name}::{info.Name} Getter Failed");
                }, new ParamType[0]);
            }

            if (info.CanWrite && (info.SetMethod?.IsPublic ?? false)) {
                InstructionRegister.RegisterSetter(info.Name + "::" + forClass.Name, (InterpreterRuntime runtime, int registerIndex) => {
                    if (!runtime.GetObject(registerIndex, out object result) || !runtime.Pop(out object valueToSet))
                        Log.Error($"{forClass.Name}::{info.Name} Setter Failed");
                    else
                        info.SetValue(result, valueToSet);
                }, new ParamType[0]);
            }
        }

        public static void RegisterGetter(MethodInfo info, ParameterInfo[] parameters, Type forClass) {
            InstructionRegister.RegisterGetter(info.Name + "::" + forClass.Name, (InterpreterRuntime runtime, int registerIndex) => {
                if (runtime.GetObject(registerIndex, out object result)) {
                    if (parameters.Length > 0) {
                        object[] objects = new object[parameters.Length];
                        for (int i = 0; i < parameters.Length; i++)
                            if (!runtime.Pop(out objects[parameters.Length - 1 - i]))
                                break;

                        // Since it will break but not assign objects[i] if the pop goes wrong we can just check if it has set it
                        if (objects[objects.Length - 1] != null) {
                            if (runtime.Push(info.Invoke(result, objects), true)) {
                                return;
                            }
                        }
                    } else if (runtime.Push(info.Invoke(result, null), true)) {
                        return;
                    }
                }
                Log.Error($"{forClass.Name}::{info.Name} Function failed");
            }, parameters.Select(x => InstructionRegister.GetParamType(x.ParameterType)).ToArray());
        }

        public static void RegisterSetter(MethodInfo info, ParameterInfo[] parameters, Type forClass) {
            InstructionRegister.RegisterSetter(info.Name + "::" + forClass.Name, (InterpreterRuntime runtime, int registerIndex) => {
                if (runtime.GetObject(registerIndex, out object result)) {
                    if (parameters.Length > 0) {
                        object[] objects = new object[parameters.Length];

                        for (int i = 0; i < parameters.Length; i++)
                            if (!runtime.Pop(out objects[parameters.Length - 1 - i]))
                                break;

                        // Since it will break but not assign objects[i] if the pop goes wrong we can just check if it has set it
                        if (objects[objects.Length - 1] != null) {
                            info.Invoke(result, objects);
                        }
                    } else {
                        info.Invoke(result, null);
                    }
                    Log.Error($"{forClass.Name}::{info.Name} Function failed");
                }
            }, parameters.Select(x => InstructionRegister.GetParamType(x.ParameterType)).ToArray());
        }

        /// <summary>
        /// Create from a given class type.
        /// </summary>
        /// <param name="forClass"> The class to generate from. </param>
        public static void Create(Type forClass) {
            IEnumerable<ConstructorInfo> constructorInfo = forClass.GetTypeInfo().DeclaredConstructors;
            IEnumerable<FieldInfo> fieldInfo = forClass.GetRuntimeFields();
            IEnumerable<PropertyInfo> propertyInfo = forClass.GetRuntimeProperties();
            IEnumerable<MethodInfo> methodInfo = forClass.GetRuntimeMethods();

            foreach (ConstructorInfo info in constructorInfo) {
                RegisterConstructor(info, forClass);
            }

            foreach (FieldInfo info in fieldInfo) {
                RegisterField(info, forClass);
            }

            foreach (PropertyInfo info in propertyInfo) {
                RegisterProperty(info, forClass);
            }

            foreach (MethodInfo info in methodInfo) {
                ParameterInfo[] parameterInfo = info.GetParameters();
                if (info.ReturnType != typeof(void)) {
                    RegisterGetter(info, parameterInfo, forClass);
                } else {
                    RegisterSetter(info, parameterInfo, forClass);
                }
            }
        }
    }
}
