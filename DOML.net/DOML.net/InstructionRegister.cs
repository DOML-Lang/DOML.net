using System;
using System.Collections.Generic;
using System.Text;
using DOML.Logger;

namespace DOML.ByteCode
{
    public static class InstructionRegister
    {
        public readonly static Dictionary<string, Action<InterpreterRuntime>> Actions = new Dictionary<string, Action<InterpreterRuntime>>();

        public readonly static Dictionary<string, int> SizeOf = new Dictionary<string, int>();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="maxAmount"></param>
        /// <param name="func"></param>
        public static void RegisterGetter(string name, string forObject, int maxAmount, Action<InterpreterRuntime> func) => RegisterAllActionAndSizeOf($"get {forObject}::{name}", maxAmount, func);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="maxAmount"></param>
        /// <param name="func"></param>
        public static void RegisterSetter(string name, string forObject, int maxAmount, Action<InterpreterRuntime> func) => RegisterAllActionAndSizeOf($"set {forObject}::{name}", maxAmount, func);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="func"></param>
        public static void UnRegisterGetter(string name, string forObject) => UnRegisterActionAndSizeOf($"get {forObject}::{name}");

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="func"></param>
        public static void UnRegisterSetter(string name, string forObject) => UnRegisterActionAndSizeOf($"set {forObject}::{name}");

        public static void RegisterConstructor(string name, Action<InterpreterRuntime> func)
        {
            name = "new " + name;
            if (Actions.ContainsKey(name) == false)
            {
                Actions.Add(name, func);
            }
            else
            {
                Log.Warning($"Action already exists for {name}, will set to the one provided");
                Actions[name] = func;
            }
        }

        public static void UnRegisterConstructor(string name)
        {
            Actions.Remove("new " + name);
        }

        /// <summary>
        /// Clears all instructions.
        /// Doesn't include system made ones.
        /// </summary>
        public static void ClearInstructions(bool isGetter)
        {
            Actions.Clear();
        }

        private static void UnRegisterActionAndSizeOf(string name)
        {
            Actions.Remove(name);
            SizeOf.Remove(name);
        }

        private static void RegisterAllActionAndSizeOf(string name, int amount, Action<InterpreterRuntime> func)
        {
            if (Actions.ContainsKey(name) == false)
            {
                Actions.Add(name, func);
            }
            else
            {
                Log.Warning($"Action already exists for {name}, will set to the one provided");
                Actions[name] = func;
            }

            if (SizeOf.ContainsKey(name))
            {
                Log.Warning($"SizeOf already exists for {name}, will choose the higher variant.");
                if (SizeOf[name] < amount)
                    SizeOf[name] = amount;
            }
            else
            {
                SizeOf.Add(name, amount);
            }
        }
    }
}
