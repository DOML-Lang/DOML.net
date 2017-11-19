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
    /// <summary>
    /// Create reflected bindings.
    /// Not as fast as static but can be done at runtime.
    /// </summary>
    public static class CreateBindings
    {
        /// <summary>
        /// Create from a given class type.
        /// </summary>
        /// <typeparam name="T"> The class to generate from. </typeparam>
        /// <param name="rootNamespace"> The namespace to place things in. </param>
        public static void Create<T>(string rootNamespace) => Create(typeof(T), rootNamespace);

        /// <summary>
        /// Create from a given class type.
        /// </summary>
        /// <param name="forClass"> The class to generate from. </param>
        /// <param name="rootNamespace"> The namespace to place things in. </param>
        public static void Create(Type forClass, string rootNamespace)
        {
            IEnumerable<ConstructorInfo> constructorInfo = forClass.GetTypeInfo().DeclaredConstructors;
            IEnumerable<FieldInfo> fieldInfo = forClass.GetRuntimeFields();
            IEnumerable<PropertyInfo> propertyInfo = forClass.GetRuntimeProperties();
            IEnumerable<MethodInfo> methodInfo = forClass.GetRuntimeMethods();

            foreach (ConstructorInfo info in constructorInfo)
            {
                ConstructorInfo copyInfo = info;
                ParameterInfo[] parameterInfo = copyInfo.GetParameters();

                Action<InterpreterRuntime> action = (InterpreterRuntime runtime) =>
                {
                    if (runtime.Pop(out object result))
                    {
                        if (parameterInfo.Length > 0)
                        {
                            object[] objects = new object[parameterInfo.Length];
                            for (int i = 0; i < parameterInfo.Length; i++)
                                if (!runtime.Pop(out objects[parameterInfo.Length-1-i]))
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

                    Log.Error($"{forClass.Name} Constructor failed");
                };

                InstructionRegister.RegisterConstructor($"{rootNamespace}.{forClass.Name}", action);
            }

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

                if (copyInfo.ReturnType != typeof(void))
                {
                    // Getter
                    Action<InterpreterRuntime> action = (InterpreterRuntime runtime) =>
                    {
                        if (runtime.Pop(out object result))
                        {
                            if (parameterInfo.Length > 0)
                            {
                                object[] objects = new object[parameterInfo.Length];
                                for (int i = 0; i < parameterInfo.Length; i++)
                                    if (!runtime.Pop(out objects[parameterInfo.Length-1-i]))
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
                                    if (!runtime.Pop(out objects[parameterInfo.Length - 1 - i]))
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
    }
}
