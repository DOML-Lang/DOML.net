using System;
using System.Collections.Generic;
using System.Text;

namespace DOML.ByteCode
{
    public static class InstructionRegister
    {
        public enum RegisterType
        {
            GET,
            SET,
            NEW,
        }

        public static Dictionary<string, Action<InterpreterRuntime>> Registers { get; } = new Dictionary<string, Action<InterpreterRuntime>>();

        /// <summary>
        /// Register an instruction.
        /// </summary>
        /// <param name="action"> The instruction to register. </param>
        /// <param name="name"> The name of the action. </param>
        public static void RegisterInstruction(string name, RegisterType type, Action<InterpreterRuntime> action)
        {
            name = type.ToString().ToLower() + name;
            if (Registers.ContainsKey(name) == false)
            {
                Registers.Add(name, action);
            }
            else
            {
                Registers[name] += action;
            }
        }

        /// <summary>
        /// Unregister an instruction.
        /// Do <see cref="ClearInstructions"/> to clear all instructions rather than unregister them all.
        /// </summary>
        /// <param name="name"> The name of the command to remove. </param>
        /// <returns> True if it was already registered and if it could unregister succesfully, else false. </returns>
        public static bool UnRegisterInstruction(string name, RegisterType type)
        {
            return Registers.Remove(type.ToString().ToLower() + name);
        }

        /// <summary>
        /// Unregister an action handler.
        /// </summary>
        /// <param name="name"> The name of the instruction. </param>
        /// <param name="action"> The action to run. </param>
        /// <returns> True if it was registered and removal was successful else false. </returns>
        public static bool UnRegisterHandler(string name, RegisterType type, Action<InterpreterRuntime> action)
        {
            name = type.ToString().ToLower() + name;
            if (Registers.ContainsKey(name))
            {
                Registers[name] -= action;
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Clears all instructions.
        /// Doesn't include system made ones.
        /// </summary>
        public static void ClearInstructions(bool isGetter)
        {
            Registers.Clear();
        }
    }
}
