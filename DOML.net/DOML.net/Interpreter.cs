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

namespace DOML.IR
{
    /// <summary>
    /// Opcodes for DOML IR.
    /// </summary>
    public enum Opcodes : byte
    {
        /* ------ System Instructions ------ */

        /// <summary>
        /// Explicitly does nothing.
        /// Parameter doesn't matter since it explicitly ignores it.
        /// </summary>
        /// <remarks> Parameter; Any Type. </remarks>
        NOP = 0,

        /// <summary>
        /// Represents a DOML comment.
        /// Parameter is the comment to emit.
        /// Similar to <see cref="NOP"/> as it does nothing.
        /// </summary>
        /// <remarks> Parameter; A string. </remarks>
        COMMENT,

        /// <summary>
        /// Reserve space in stack equal to the parameter given.
        /// Note: The parameter should represent the new stack length not the difference.
        /// </summary>
        /// <remarks> Parameter; An integer. </remarks>
        MAKE_SPACE,

        /// <summary>
        /// Reserve space in object registers equal to the parameter given.
        /// Note: The parameter should represent the new object register length not the difference.
        /// </summary>
        /// <remarks> Parameter; An integer. </remarks>
        MAKE_REG,

        /* ------ Call Instructions ------ */

        /// <summary>
        /// Runs the set function of the parameter given.
        /// </summary>
        /// <remarks> Parameter; The set function to run. </remarks>
        SET,

        /// <summary>
        /// Performs a function call based on the parameter.
        /// Indexes <see cref="InstructionRegister.Actions"/> with "get" + parameter.
        /// </summary>
        /// <remarks> Parameter; An identifier to call. </remarks>
        CALL,

        /// <summary>
        /// Creates a new object based on the parameter.
        /// Indexes <see cref="InstructionRegister.Actions"/> with "new" + parameter.
        /// </summary>
        /// <remarks> Parameter; An identifier to call. </remarks>
        NEW,

        /* ------ Push Instructions ------ */

        /// <summary>
        /// Register an object to the index supplied.
        /// </summary>
        REG_OBJ,

        /// <summary>
        /// Unregisters an object
        /// </summary>
        UNREG_OBJ,

        /// <summary>
        /// Copies top value, (aka peek and repush).
        /// </summary>
        COPY,

        /// <summary>
        /// Pops items off the stack equal to the parameter given.
        /// I.e. if 2 is given then pop off the top two values.
        /// </summary>
        /// <remarks> Parameter; A signed 32 integer. </remarks>
        POP,

        /// <summary>
        /// Pushes an object from the register ID given by the parameter onto the stack.
        /// </summary>
        /// <remarks> Parameter; A signed 32 integer. </remarks>
        PUSH_OBJ,

        /// <summary>
        /// Pushes the parameter onto the stack.
        /// </summary>
        /// <remarks> Parameter; A signed 64 integer. </remarks>
        PUSH_INT,

        /// <summary>
        /// Pushes the parameter onto the stack.
        /// </summary>
        /// <remarks> Parameter; A 64 floating point number. </remarks>
        PUSH_NUM,

        /// <summary>
        /// Pushes the parameter onto the stack.
        /// </summary>
        /// <remarks> Parameter; A 128 decimal floating point number. </remarks>
        PUSH_DEC,

        /// <summary>
        /// Pushes the parameter onto the stack.
        /// </summary>
        /// <remarks> Parameter; A string. </remarks>
        PUSH_STR,

        /// <summary>
        /// Pushes the parameter onto the stack.
        /// </summary>
        /// <remarks> Parameter; A boolean. </remarks>
        PUSH_BOOL,

        /// <summary>
        /// Pushes the parameter onto the stack.
        /// </summary>
        /// <remarks> Parameter; Any Type. </remarks>
        PUSH,

        /// <summary>
        /// The amount of instructions.
        /// </summary>
        /// <remarks> Keep ALWAYS as last value. </remarks>
        COUNT_OF_INSTRUCTIONS,
    }

    /// <summary>
    /// The interpreter for DOML IR.
    /// This handles executing the opcodes.
    /// </summary>
    public class Interpreter
    {
        /// <summary>
        /// All the instructions to execute.
        /// </summary>
        public readonly List<Instruction> Instructions;

        /// <summary>
        /// The runtime of the interpreter instance.
        /// Manages the stack VM and registers.
        /// </summary>
        public readonly InterpreterRuntime Runtime;

        /// <summary>
        /// Create a new interpreter instance.
        /// </summary>
        /// <param name="instructions"> The instructions to assign to this interpreter instance. </param>
        public Interpreter(List<Instruction> instructions)
        {
            Instructions = instructions;
            Runtime = new InterpreterRuntime();
        }

        /// <summary>
        /// Executes all instructions.
        /// </summary>
        /// <param name="safe"> Run either safe or unsafe instructions (safe does checks, unsafe doesn't). </param>
        /// <remarks> If using the code generated by the parser instance you can use unsafe else use safe. </remarks>
        public void Execute(bool safe = true)
        {
            Runtime.ClearSpace();

            for (int i = 0; i < Instructions.Count; i++)
            {
                if (safe)
                    HandleSafeInstruction(Instructions[i]);
                else
                    HandleUnsafeInstruction(Instructions[i]);
            }
        }

        /// <summary>
        /// Handles the instruction safely.
        /// </summary>
        /// <param name="instruction"> The instruction. </param>
        public void HandleSafeInstruction(Instruction instruction)
        {
            switch ((Opcodes)instruction.OpCode)
            {
            #region System Instructions
            case Opcodes.NOP:
            case Opcodes.COMMENT:
                // Explicity does nothing
                // @NOTE: Should we even call them??
                return;
            case Opcodes.MAKE_SPACE:
                {
                    if (instruction.Parameter is int res)
                    {
                        if (Runtime.ReserveSpace(res))
                        {
                            Log.Info("Stack resized, objects won't be carried across.");
                        }
                    }
                    else
                    {
                        Log.Error("Reserve failed");
                    }
                    return;
                }
            case Opcodes.MAKE_REG:
                {
                    if (instruction.Parameter is int res)
                    {
                        if (Runtime.ReserveRegister(res))
                        {
                            Log.Info("Registers resized, objects won't be carried across.");
                        }
                    }
                    else
                    {
                        Log.Error("Reserve failed");
                    }
                    return;
                }
            #endregion
            #region Set Instructions
            case Opcodes.SET:
                // Run user code
                {
                    if (!(instruction.Parameter is string key) || !InstructionRegister.Actions.ContainsKey(key))
                    {
                        Log.Error("Set failed");
                    }
                    else
                    {
                        InstructionRegister.Actions[key](Runtime);
                    }
                    return;
                }
            case Opcodes.COPY:
                {
                    // Peek top and push
                    if (!(instruction.Parameter is int res) || !Runtime.Peek(out object result))
                    {
                        Log.Error("Copy failed");
                    }
                    else
                    {
                        for (; res >= 0; res++)
                        {
                            if (!Runtime.Push(result, true))
                            {
                                Log.Error("Copy - push failed");
                                return;
                            }
                        }
                    }
                    return;
                }
            case Opcodes.REG_OBJ:
                {
                    if (!(instruction.Parameter is int res) || !Runtime.Pop(out object result) || !Runtime.SetObject(result, res))
                    {
                        Log.Error("Register Object Failed");
                    }
                    return;
                }
            case Opcodes.UNREG_OBJ:
                {
                    if (!(instruction.Parameter is int res) || !Runtime.RemoveObject(res))
                    {
                        Log.Error("Unregister Object Failed");
                    }
                    return;
                }
            #endregion
            #region Push Instructions
            case Opcodes.PUSH_OBJ:
                {
                    if (!(instruction.Parameter is int res) || !Runtime.GetObject(res, out object result) || !Runtime.Push(result, true))
                    {
                        Log.Error("Push failed or wrong type.");
                    }
                    return;
                }
            case Opcodes.PUSH_INT:
                if (!(instruction.Parameter is long) || !Runtime.Push(instruction.Parameter, true))
                {
                    Log.Error("Push failed or wrong type.");
                }
                return;
            case Opcodes.PUSH_NUM:
                if (!(instruction.Parameter is double) || !Runtime.Push(instruction.Parameter, true))
                {
                    Log.Error("Push failed or wrong type.");
                }
                return;
            case Opcodes.PUSH_DEC:
                if (!(instruction.Parameter is decimal) || !Runtime.Push(instruction.Parameter, true))
                {
                    Log.Error("Push failed or wrong type.");
                }
                return;
            case Opcodes.PUSH_STR:
                if (!(instruction.Parameter is string) || !Runtime.Push(instruction.Parameter, true))
                {
                    Log.Error("Push failed or wrong type.");
                }
                return;
            case Opcodes.PUSH_BOOL:
                if (!(instruction.Parameter is bool) || !Runtime.Push(instruction.Parameter, true))
                {
                    Log.Error("Push failed or wrong type.");
                }
                return;
            case Opcodes.PUSH:
                if (!Runtime.Push(instruction.Parameter, true))
                {
                    Log.Error("Push failed");
                }
                return;
            case Opcodes.CALL:
            case Opcodes.NEW:
                {
                    // At this level they do the same thing since we store the parameter with the 'get' or 'new'
                    if (instruction.Parameter is string str && InstructionRegister.Actions.ContainsKey(str))
                    {
                        InstructionRegister.Actions[str](Runtime);
                    }
                    else
                    {
                        Log.Error("Invalid Opcode or parameter isn't the right type.");
                    }
                    return;
                }
            case Opcodes.POP:
                {
                    if (instruction.Parameter is int i && i < Runtime.CurrentStackSize)
                    {
                        for (; i < Runtime.CurrentStackSize; i++)
                        {
                            if (!Runtime.Pop())
                            {
                                Log.Error("Pop failed");
                                return;
                            }
                        }
                    }
                    else
                    {
                        Log.Error("Pop failed");
                    }
                    return;
                }
            #endregion
            default:
                throw new NotImplementedException("Option not implemented");
            }
        }

        /// <summary>
        /// Handles the instruction unsafely.
        /// </summary>
        /// <param name="instruction"> The instruction. </param>
        public void HandleUnsafeInstruction(Instruction instruction)
        {
            switch ((Opcodes)instruction.OpCode)
            {
            #region System Instructions
            case Opcodes.NOP:
            case Opcodes.COMMENT:
                // Explicity does nothing
                // @NOTE: Should we even call them??
                return;
            case Opcodes.MAKE_SPACE:
                Runtime.ReserveSpace((int)instruction.Parameter);
                return;
            case Opcodes.MAKE_REG:
                Runtime.ReserveRegister((int)instruction.Parameter);
                return;
            #endregion
            #region Set Instructions
            case Opcodes.SET:
                // Run user code
                InstructionRegister.Actions[(string)instruction.Parameter](Runtime);
                return;
            case Opcodes.COPY:
                {
                    object obj = Runtime.Unsafe_Peek();
                    for (int i = (int)instruction.Parameter; i >= 0; i--)
                    {
                        Runtime.Unsafe_Push(obj);
                    }
                    return;
                }
            case Opcodes.REG_OBJ:
                Runtime.Unsafe_SetObject(Runtime.Unsafe_Pop(), (int)instruction.Parameter);
                return;
            case Opcodes.UNREG_OBJ:
                Runtime.Unsafe_RemoveObject((int)instruction.Parameter);
                return;
            #endregion
            #region Push Instructions
            case Opcodes.PUSH_OBJ:
                Runtime.Unsafe_Push(Runtime.Unsafe_GetObject((int)instruction.Parameter));
                return;
            // Arguably we should do a cast before push??
            // But it'll jit that way most likely and its just 'useless'
            case Opcodes.PUSH_INT:
            case Opcodes.PUSH_NUM:
            case Opcodes.PUSH_DEC:
            case Opcodes.PUSH_STR:
            case Opcodes.PUSH_BOOL:
            case Opcodes.PUSH:
                Runtime.Unsafe_Push(instruction.Parameter);
                return;
            case Opcodes.NEW:
            case Opcodes.CALL:
                // At this level they do the same thing since we store the parameter with the 'get' or 'new'
                InstructionRegister.Actions[(string)instruction.Parameter](Runtime);
                return;
            case Opcodes.POP:
                {
                    for (int i = (int)instruction.Parameter; i < Runtime.CurrentStackSize; i++)
                    {
                        Runtime.Unsafe_Pop();
                    }
                    return;
                }
            #endregion
            default:
                throw new NotImplementedException("Option not implemented");
            }
        }
    }
}
