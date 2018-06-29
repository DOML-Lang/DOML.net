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
    /// <summary>
    /// A register of all the instructions.
    /// </summary>
    public static class InstructionRegister {
        /// <summary>
        /// These represent the actions to run.
        /// </summary>
        public readonly static Dictionary<string, Action<InterpreterRuntime, int>> Setters = new Dictionary<string, Action<InterpreterRuntime, int>>();

        public readonly static Dictionary<string, Action<InterpreterRuntime, int>> Constructors = new Dictionary<string, Action<InterpreterRuntime, int>>();

        public readonly static Dictionary<string, Action<InterpreterRuntime, int>> Getters = new Dictionary<string, Action<InterpreterRuntime, int>>();

        /// <summary>
        /// Register a getter function.
        /// </summary>
        /// <param name="name"> Function name. </param>
        /// <param name="forObject"> The object name this refers to. </param>
        /// <param name="returnCount"> The amount of return variables. </param>
        /// <param name="func"> The function. </param>
        public static void RegisterGetter(string name, string forObject, int returnCount, Action<InterpreterRuntime, int> func) => RegisterActionAndSizeOf($"get {forObject}::{name}", returnCount, func);

        /// <summary>
        /// Register a setter function.
        /// </summary>
        /// <param name="name"> Function name. </param>
        /// <param name="forObject"> The object name this refers to. </param>
        /// <param name="parameterCount"> The amount of parameter variables. </param>
        /// <param name="func"> The function. </param>
        public static void RegisterSetter(string name, string forObject, int parameterCount, Action<InterpreterRuntime, int> func) => RegisterActionAndSizeOf($"set {forObject}::{name}", parameterCount, func);

        /// <summary>
        /// Unregisters a getter function.
        /// </summary>
        /// <param name="name"> Function name. </param>
        /// <param name="forObject"> The object name this refers to. </param>
        public static void UnRegisterGetter(string name, string forObject) => UnRegisterActionAndSizeOf($"get {forObject}::{name}");

        /// <summary>
        /// Unregisters a setter function.
        /// </summary>
        /// <param name="name"> Function name. </param>
        /// <param name="forObject"> The object name this refers to. </param>
        public static void UnRegisterSetter(string name, string forObject) => UnRegisterActionAndSizeOf($"set {forObject}::{name}");

        /// <summary>
        /// Registers a constructor.
        /// </summary>
        /// <param name="name"> Function name. </param>
        /// <param name="func"> The function. </param>
        public static void RegisterConstructor(string name, Action<InterpreterRuntime, int> func) {
            if (Constructors.ContainsKey(name) == false) {
                Constructors.Add(name, func);
            } else {
                Log.Warning($"Action already exists for {name}, will set to the one provided");
                Constructors[name] = func;
            }
        }

        /// <summary>
        /// Unregisters a constructor.
        /// </summary>
        /// <param name="name"> Function name. </param>
        public static void UnRegisterConstructor(string name) {
            Constructors.Remove(name);
        }

        /// <summary>
        /// Clears all instructions.
        /// Doesn't include system made ones.
        /// </summary>
        public static void ClearInstructions() {
            Constructors.Clear();
            Getters.Clear();
            Setters.Clear();
        }

        /// <summary>
        /// Registers action and sizeof.
        /// </summary>
        /// <param name="name"> The function name. </param>
        /// <param name="amount"> The amount of sizeofs. </param>
        /// <param name="func"> The function. </param>
        private static void RegisterActionAndSizeOf(string name, int amount, Action<InterpreterRuntime, int> func) {
            if (Actions.ContainsKey(name) == false) {
                Actions.Add(name, func);
            } else {
                Log.Warning($"Action already exists for {name}, will set to the one provided");
                Actions[name] = func;
            }

            if (SizeOf.ContainsKey(name)) {
                Log.Warning($"SizeOf already exists for {name}, will choose the higher variant.");
                if (SizeOf[name] < amount)
                    SizeOf[name] = amount;
            } else {
                SizeOf.Add(name, amount);
            }
        }
    }
}
