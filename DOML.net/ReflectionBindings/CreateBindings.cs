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
using System.Reflection;
using DOML.IR;
using DOML.Logger;

namespace ReflectionBindings
{
    public static class CreateBindings
    {
        public static void Create(Type forClass, string rootNamespace)
        {
            IEnumerable<FieldInfo> fieldInfo = forClass.GetRuntimeFields();
            IEnumerable<PropertyInfo> propertyInfo = forClass.GetRuntimeProperties();
            IEnumerable<MethodInfo> methodInfo = forClass.GetRuntimeMethods();

            foreach (FieldInfo info in fieldInfo)
            {
                FieldInfo copyInfo = info;
                InstructionRegister.RegisterGetter(copyInfo.Name, forClass.Name, 1, (InterpreterRuntime runtime) =>
                {
                    if (!runtime.Pop(out object result) || !runtime.Push(copyInfo.GetValue(result), true))
                        Log.Error($"{forClass.Name}::{info.Name} Getter Failed");
                });

                InstructionRegister.RegisterSetter(copyInfo.Name, forClass.Name, 1, (InterpreterRuntime runtime) =>
                {
                    if (!runtime.Pop(out object result) || !runtime.Pop(out object valueToSet))
                        Log.Error($"{forClass.Name}::{info.Name} Setter Failed");
                    else
                        info.SetValue(result, valueToSet);
                });
            }

            foreach (PropertyInfo info in propertyInfo)
            {
                PropertyInfo copyInfo = info;

                if (info.CanRead && (info.GetMethod?.IsPublic ?? false))
                {
                    InstructionRegister.RegisterGetter(copyInfo.Name, forClass.Name, 1, (InterpreterRuntime runtime) =>
                    {
                        if (!runtime.Pop(out object result) || !runtime.Push(copyInfo.GetValue(result), true))
                            Log.Error($"{forClass.Name}::{info.Name} Getter Failed");
                    });
                }

                if (info.CanWrite && (info.SetMethod?.IsPublic ?? false))
                {
                    InstructionRegister.RegisterSetter(copyInfo.Name, forClass.Name, 1, (InterpreterRuntime runtime) =>
                    {
                        if (!runtime.Pop(out object result) || !runtime.Pop(out object valueToSet))
                            Log.Error($"{forClass.Name}::{info.Name} Setter Failed");
                        else
                            info.SetValue(result, valueToSet);
                    });
                }
            }

            foreach (MethodInfo info in methodInfo)
            {
                MethodInfo copyInfo = info;
                ParameterInfo[] parameterInfo = copyInfo.GetParameters();

                if (copyInfo.IsConstructor || copyInfo.ReturnType != typeof(void))
                {
                    // Constructor or Getter
                    Action<InterpreterRuntime> action = (InterpreterRuntime runtime) =>
                    {
                        if (runtime.Pop(out object result))
                        {
                            if (parameterInfo.Length > 0)
                            {
                                object[] objects = new object[parameterInfo.Length];
                                for (int i = 0; i < parameterInfo.Length; i++)
                                    if (!runtime.Pop(out objects[i]))
                                        break;

                                // Since it will break but not assign objects[i] if the pop goes wrong we can just check if it has set it
                                if (objects[objects.Length - 1] != null)
                                {
                                    if (runtime.Push(copyInfo.Invoke(result, objects), true))
                                    {
                                        return;
                                    }
                                }
                            }
                            else if (runtime.Push(copyInfo.Invoke(result, null), true))
                            {
                                return;
                            }
                        }

                        Log.Error($"{forClass.Name}::{copyInfo.Name} Function failed");
                    };

                    if (copyInfo.IsConstructor)
                        InstructionRegister.RegisterConstructor($"{rootNamespace}.{forClass.Name}", action);
                    else
                        InstructionRegister.RegisterGetter(info.Name, forClass.Name, 1, action);
                }
                else
                {
                    // Setter
                    InstructionRegister.RegisterSetter(info.Name, forClass.Name, 1, (InterpreterRuntime runtime) =>
                    {
                        if (runtime.Pop(out object result))
                        {
                            if (parameterInfo.Length > 0)
                            {
                                object[] objects = new object[parameterInfo.Length] ;

                                for (int i = 0; i < parameterInfo.Length; i++)
                                    if (!runtime.Pop(out objects[i]))
                                        break;

                                // Since it will break but not assign objects[i] if the pop goes wrong we can just check if it has set it
                                if (objects[objects.Length - 1] != null)
                                {
                                    copyInfo.Invoke(result, objects);
                                }
                            }
                            else
                            {
                                copyInfo.Invoke(result, null);
                            }

                            Log.Error($"{forClass.Name}::{copyInfo.Name} Function failed");
                        }
                    });
                }
            }
        }

        public static void Create<T>(string rootNamespace) => Create(typeof(T), rootNamespace);
    }
}
