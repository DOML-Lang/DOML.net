using System;
using System.Collections.Generic;
using System.Text;
using DOML.Logger;
using System.Reflection;

namespace DOML.ByteCode
{
    public enum BaseInstruction : byte
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
        /// Panics if top value matches parameter.
        /// </summary>
        /// <remarks> Parameter; Any Type. </remarks>
        PANIC,

        /// <summary>
        /// Reserve space in stack equal to the parameter given.
        /// Note: The parameter should represent the new stack length not the difference.
        /// </summary>
        /// <remarks> Parameter; An integer. </remarks>
        MAKE_SPACE,

        /// <summary>
        /// Reserve space in object registers.
        /// </summary>
        MAKE_REG,

        /* ------ Set Instructions ------ */

        /// <summary>
        /// Runs the set function of the parameter given.
        /// </summary>
        /// <remarks> Parameter; The set function to run. </remarks>
        SET,

        /// <summary>
        /// Copies top value, (aka peek and repush).
        /// </summary>
        COPY,

        /// <summary>
        /// Clears range .
        /// @TODO: Doesn't take any operators, not a very good practice.  Should figure out either 'how' it can or whatever
        /// </summary>
        CLEAR,

        /// <summary>
        /// Clears all registers.
        /// @TODO: Doesn't take any operators, not a very good practice.  Should figure out either 'how' it can or whatever
        /// </summary>
        CLEAR_REG,

        /// <summary>
        /// Register an object to the index supplied.
        /// </summary>
        REG_OBJ,

        /// <summary>
        /// Unregisters an object
        /// </summary>
        UNREG_OBJ,

        /* ------ Push Instructions (17 - 34) ------ */
        /// <summary>
        /// Pushes an object from the register ID given by the parameter onto the stack.
        /// </summary>
        /// <remarks> Parameter; A signed 32 integer. </remarks>
        PUSH_OBJ,

        /// <summary>
        /// Pushes the parameter onto the stack.
        /// </summary>
        /// <remarks> Parameter; A signed 8 integer. </remarks>
        PUSH_8I,

        /// <summary>
        /// Pushes the parameter onto the stack.
        /// </summary>
        /// <remarks> Parameter; A signed 16 integer. </remarks>
        PUSH_16I,

        /// <summary>
        /// Pushes the parameter onto the stack.
        /// </summary>
        /// <remarks> Parameter; A signed 32 integer. </remarks>
        PUSH_32I,

        /// <summary>
        /// Pushes the parameter onto the stack.
        /// </summary>
        /// <remarks> Parameter; A signed 64 integer. </remarks>
        PUSH_64I,

        /// <summary>
        /// Pushes the parameter onto the stack.
        /// </summary>
        /// <remarks> Parameter; A unsigned 8 integer. </remarks>
        PUSH_8U,

        /// <summary>
        /// Pushes the parameter onto the stack.
        /// </summary>
        /// <remarks> Parameter; An unsigned 16 integer. </remarks>
        PUSH_16U,

        /// <summary>
        /// Pushes the parameter onto the stack.
        /// </summary>
        /// <remarks> Parameter; An unsigned 32 integer. </remarks>
        PUSH_32U,

        /// <summary>
        /// Pushes the parameter onto the stack.
        /// </summary>
        /// <remarks> Parameter; An unsigned 64 integer. </remarks>
        PUSH_64U,

        /// <summary>
        /// Pushes the parameter onto the stack.
        /// </summary>
        /// <remarks> Parameter; A 16 floating point number. </remarks>
        PUSH_16F,

        /// <summary>
        /// Pushes the parameter onto the stack.
        /// </summary>
        /// <remarks> Parameter; A 32 floating point number. </remarks>
        PUSH_32F,

        /// <summary>
        /// Pushes the parameter onto the stack.
        /// </summary>
        /// <remarks> Parameter; A 64 floating point number. </remarks>
        PUSH_64F,

        /// <summary>
        /// Pushes the parameter onto the stack.
        /// </summary>
        /// <remarks> Parameter; A 80 floating point number. </remarks>
        PUSH_80F,

        /// <summary>
        /// Pushes the parameter onto the stack.
        /// </summary>
        /// <remarks> Parameter; A 128 floating point number. </remarks>
        PUSH_128F,

        /// <summary>
        /// Pushes the parameter onto the stack.
        /// </summary>
        /// <remarks> Parameter; A 64 decimal floating point number. </remarks>
        PUSH_64D,

        /// <summary>
        /// Pushes the parameter onto the stack.
        /// </summary>
        /// <remarks> Parameter; A 128 decimal floating point number. </remarks>
        PUSH_128D,

        /// <summary>
        /// Pushes the parameter onto the stack.
        /// </summary>
        /// <remarks> Parameter; A string. </remarks>
        PUSH_STR,

        /// <summary>
        /// Pushes the parameter onto the stack.
        /// </summary>
        /// <remarks> Parameter; A character. </remarks>
        PUSH_CHAR,

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
        /// Performs a function call based on the parameter.
        /// Indexes <see cref="InstructionRegister.Registers"/> with "get" + parameter.
        /// </summary>
        /// <remarks> Parameter; An identifier to call. </remarks>
        CALL,

        /// <summary>
        /// Creates a new object based on the parameter.
        /// Indexes <see cref="InstructionRegister.Registers"/> with "new" + parameter.
        /// </summary>
        /// <remarks> Parameter; An identifier to call. </remarks>
        NEW,

        /// <summary>
        /// Pops items off the stack equal to the parameter given.
        /// I.e. if 2 is given then pop off the top two values.
        /// </summary>
        /// <remarks> Parameter; A signed 32 integer. </remarks>
        POP,

        /* ------ Check Instructions  (35 - 55) ------ */
        /// <summary>
        /// Pushes true if the max stack size is less than the top value, else false.
        /// </summary>
        COMP_SIZE,

        /// <summary>
        /// Pushes true if the current stack size is less than the top value, else false.
        /// </summary>
        COMP_PTR,

        /// <summary>
        /// Pushes true if the current register size is less than the top value, else false.
        /// </summary>
        COMP_REG,

        /// <summary>
        /// The amount of instructions.
        /// Keep ALWAYS as last.
        /// May not match the range of the last set of instructions.
        /// But that's okay since in reality that's an 'optimisation'.
        /// </summary>
        COUNT_OF_INSTRUCTIONS,
    }

    public class Interpreter
    {
        /// <summary>
        /// Basically all the 'user' instructions.
        /// Will also contain our instructions.
        /// </summary>
        public readonly List<Instruction> Instructions;
        private InterpreterRuntime Runtime;

        public Interpreter(List<Instruction> instructions)
        {
            Instructions = instructions;
            Runtime = new InterpreterRuntime();
        }

        public void Execute(bool safe = true)
        {
            Runtime.ClearSpace();

            for (int i = 0; i < Instructions.Count; i++)
            {
                if (Instructions[i].OpCode >= (byte)BaseInstruction.COUNT_OF_INSTRUCTIONS)
                {
                    Log.Error("Opcode not in valid range");
                }
                else if (safe)
                {
                    HandleSafeInstruction(Instructions[i]);
                }
                else
                {
                    HandleUnsafeInstruction(Instructions[i]);
                }
            }
        }

        public void Emit(ByteCodeWriter writer, bool withLineComments, bool withAnyComments)
        {
            writer.WriteHeader();

            for (int i = 0; i < Instructions.Count; i++)
            {
                switch ((BaseInstruction)Instructions[i].OpCode)
                {
                default:
                    if (Instructions[i].OpCode < (byte)BaseInstruction.COUNT_OF_INSTRUCTIONS)
                    {
                        writer.WriteInstructionText(Instructions[i], withLineComments);
                    }
                    else
                    {
                        Log.Error("Invalid instruction: " + Instructions[i].OpCode);
                    }
                    break;
                }
            }

            writer.Finish();
        }

        public void Emit(string filePath, bool append, bool withLineComments, bool withAnyComments)
        {
            using (ByteCodeWriter writer = new ByteCodeWriter(filePath, append))
            {
                Emit(writer, withLineComments, withAnyComments);
            }
        }

        public void HandleSafeInstruction(Instruction instruction)
        {
            switch ((BaseInstruction)instruction.OpCode)
            {
            #region System Instructions
            case BaseInstruction.NOP:
            case BaseInstruction.COMMENT:
                // Explicity does nothing
                // @NOTE: Should we even call them??
                return;
            case BaseInstruction.PANIC:
                {
                    if (!Runtime.Pop(out object result))
                    {
                        Log.Error("PANIC: Nothing to compare against.");
                    }
                    else if (result != instruction.Parameter)
                    {
                        // Maybe we convert to string then check??
                        Log.Error("PANIC: Top value doesn't equal parameter.");
                    }
                    return;
                }
            case BaseInstruction.MAKE_SPACE:
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
            case BaseInstruction.MAKE_REG:
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
            case BaseInstruction.SET:
                // Run user code
                {
                    if (!(instruction.Parameter is string key) || !InstructionRegister.Registers.ContainsKey(key))
                    {
                        Log.Error("Set failed");
                    }
                    else
                    {
                        InstructionRegister.Registers[key](Runtime);
                    }
                    return;
                }
            case BaseInstruction.COPY:
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
            case BaseInstruction.CLEAR:
                Runtime.ClearSpace();
                return;
            case BaseInstruction.CLEAR_REG:
                Runtime.ClearRegisters();
                return;
            case BaseInstruction.REG_OBJ:
                {
                    if (!(instruction.Parameter is int res) || !Runtime.Pop(out object result) || !Runtime.SetObject(result, res))
                    {
                        Log.Error("Register Object Failed");
                    }
                    return;
                }
            case BaseInstruction.UNREG_OBJ:
                {
                    if (!(instruction.Parameter is int res) || !Runtime.UnsetObject(res))
                    {
                        Log.Error("Unregister Object Failed");
                    }
                    return;
                }
            #endregion
            #region Push Instructions
            case BaseInstruction.PUSH_OBJ:
                {
                    if (!(instruction.Parameter is int res) || !Runtime.GetObject(res, out object result) || !Runtime.Push(result, true))
                    {
                        Log.Error("Push failed or wrong type.");
                    }
                    return;
                }
            /* Note: In the following cases I'm not casting since it would just jitter that away anyway. */
            case BaseInstruction.PUSH_16I:
                if (!(instruction.Parameter is short) || !Runtime.Push(instruction.Parameter, true))
                {
                    Log.Error("Push failed or wrong type.");
                }
                return;
            case BaseInstruction.PUSH_32I:
                if (!(instruction.Parameter is int) || !Runtime.Push(instruction.Parameter, true))
                {
                    Log.Error("Push failed or wrong type.");
                }
                return;
            case BaseInstruction.PUSH_64I:
                if (!(instruction.Parameter is long) || !Runtime.Push(instruction.Parameter, true))
                {
                    Log.Error("Push failed or wrong type.");
                }
                return;
            case BaseInstruction.PUSH_16U:
                if (!(instruction.Parameter is ushort) || !Runtime.Push(instruction.Parameter, true))
                {
                    Log.Error("Push failed or wrong type.");
                }
                return;
            case BaseInstruction.PUSH_32U:
                if (!(instruction.Parameter is uint) || !Runtime.Push(instruction.Parameter, true))
                {
                    Log.Error("Push failed or wrong type.");
                }
                return;
            case BaseInstruction.PUSH_64U:
                if (!(instruction.Parameter is ulong) || !Runtime.Push(instruction.Parameter, true))
                {
                    Log.Error("Push failed or wrong type.");
                }
                return;
            case BaseInstruction.PUSH_32F:
                if (!(instruction.Parameter is float) || !Runtime.Push(instruction.Parameter, true))
                {
                    Log.Error("Push failed or wrong type.");
                }
                return;
            case BaseInstruction.PUSH_64F:
                if (!(instruction.Parameter is double) || !Runtime.Push(instruction.Parameter, true))
                {
                    Log.Error("Push failed or wrong type.");
                }
                return;
            case BaseInstruction.PUSH_80F:
                throw new NotImplementedException();
            case BaseInstruction.PUSH_128F:
                throw new NotImplementedException();
            case BaseInstruction.PUSH_64D:
                throw new NotImplementedException();
            case BaseInstruction.PUSH_128D:
                if (!(instruction.Parameter is decimal) || !Runtime.Push(instruction.Parameter, true))
                {
                    Log.Error("Push failed or wrong type.");
                }
                return;
            case BaseInstruction.PUSH_STR:
                if (!(instruction.Parameter is string) || !Runtime.Push(instruction.Parameter, true))
                {
                    Log.Error("Push failed or wrong type.");
                }
                return;
            case BaseInstruction.PUSH_CHAR:
                if (!(instruction.Parameter is char) || !Runtime.Push(instruction.Parameter, true))
                {
                    Log.Error("Push failed or wrong type.");
                }
                return;
            case BaseInstruction.PUSH_BOOL:
                if (!(instruction.Parameter is bool) || !Runtime.Push(instruction.Parameter, true))
                {
                    Log.Error("Push failed or wrong type.");
                }
                return;
            case BaseInstruction.PUSH:
                if (!Runtime.Push(instruction.Parameter, true))
                {
                    Log.Error("Push failed");
                }
                return;
            case BaseInstruction.CALL:
            case BaseInstruction.NEW:
                {
                    // At this level they do the same thing since we store the parameter with the 'get' or 'new'
                    if (instruction.Parameter is string str && InstructionRegister.Registers.ContainsKey(str))
                    {
                        InstructionRegister.Registers[str](Runtime);
                    }
                    else
                    {
                        Log.Error("Invalid Opcode or parameter isn't the right type.");
                    }
                    return;
                }
            case BaseInstruction.POP:
                {
                    if (instruction.Parameter is int i && i < Runtime.CurrentStackSize)
                    {
                        for (; i < Runtime.CurrentStackSize; i++)
                        {
                            if (!Runtime.Pop(out object obj))
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
            #region Check Instructions
            case BaseInstruction.COMP_SIZE:
                {
                    if (!(instruction.Parameter is int res) || !Runtime.Push(Runtime.MaxStackSize < res, true))
                    {
                        Log.Error("Check failed");
                    }
                    return;
                }
            case BaseInstruction.COMP_PTR:
                {
                    if (!(instruction.Parameter is int res) || !Runtime.Push(Runtime.CurrentStackSize < res, true))
                    {
                        Log.Error("Check failed");
                    }
                    return;
                }
            case BaseInstruction.COMP_REG:
                {
                    if (!(instruction.Parameter is int res) || !Runtime.Push(Runtime.RegisterSize < res, true))
                    {
                        Log.Error("Check failed");
                    }
                    return;
                }
            #endregion
            default:
                throw new NotImplementedException("Option not implemented");
            }
        }

        public void HandleUnsafeInstruction(Instruction instruction)
        {
            switch ((BaseInstruction)instruction.OpCode)
            {
            #region System Instructions
            case BaseInstruction.NOP:
            case BaseInstruction.COMMENT:
                // Explicity does nothing
                // @NOTE: Should we even call them??
                return;
            case BaseInstruction.PANIC:
                {
                    if (Runtime.Unsafe_Pop<object>() != instruction.Parameter)
                    {
                        // Maybe we convert to string then check??
                        Log.Error("PANIC: Top value doesn't equal parameter.");
                    }
                    return;
                }
            case BaseInstruction.MAKE_SPACE:
                Runtime.ReserveSpace((int)instruction.Parameter);
                return;
            case BaseInstruction.MAKE_REG:
                Runtime.ReserveRegister((int)instruction.Parameter);
                return;
            #endregion
            #region Set Instructions
            case BaseInstruction.SET:
                // Run user code
                InstructionRegister.Registers[(string)instruction.Parameter](Runtime);
                return;
            case BaseInstruction.COPY:
                {
                    object obj = Runtime.Unsafe_Peek<object>();
                    for (int i = (int)instruction.Parameter; i >= 0; i--)
                    {
                        Runtime.Unsafe_Push(obj);
                    }
                    return;
                }
            case BaseInstruction.CLEAR:
                Runtime.Unsafe_ClearSpace();
                return;
            case BaseInstruction.CLEAR_REG:
                Runtime.Unsafe_ClearRegisters();
                return;
            case BaseInstruction.REG_OBJ:
                Runtime.Unsafe_SetObject(Runtime.Unsafe_Pop<object>(), (int)instruction.Parameter);
                return;
            case BaseInstruction.UNREG_OBJ:
                Runtime.Unsafe_UnsetObject((int)instruction.Parameter);
                return;
            #endregion
            #region Push Instructions
            case BaseInstruction.PUSH_OBJ:
                Runtime.Unsafe_Push(Runtime.Unsafe_GetObject((int)instruction.Parameter));
                return;
            // Arguably we should do a cast before push??
            // But it'll jit that way most likely and its just 'useless'
            case BaseInstruction.PUSH_8I:
            case BaseInstruction.PUSH_16I:
            case BaseInstruction.PUSH_32I:
            case BaseInstruction.PUSH_64I:
            case BaseInstruction.PUSH_8U:
            case BaseInstruction.PUSH_16U:
            case BaseInstruction.PUSH_32U:
            case BaseInstruction.PUSH_64U:
            case BaseInstruction.PUSH_16F:
            case BaseInstruction.PUSH_32F:
            case BaseInstruction.PUSH_64F:
            case BaseInstruction.PUSH_80F:
            case BaseInstruction.PUSH_128F:
            case BaseInstruction.PUSH_64D:
            case BaseInstruction.PUSH_128D:
            case BaseInstruction.PUSH_STR:
            case BaseInstruction.PUSH_CHAR:
            case BaseInstruction.PUSH_BOOL:
            case BaseInstruction.PUSH:
                Runtime.Unsafe_Push(instruction.Parameter);
                return;
            case BaseInstruction.NEW:
            case BaseInstruction.CALL:
                // At this level they do the same thing since we store the parameter with the 'get' or 'new'
                InstructionRegister.Registers[(string)instruction.Parameter](Runtime);
                return;
            case BaseInstruction.POP:
                {
                    for (int i = (int)instruction.Parameter; i < Runtime.CurrentStackSize; i++)
                    {
                        Runtime.Unsafe_Pop<object>();
                    }
                    return;
                }
            #endregion
            #region Check Instructions
            case BaseInstruction.COMP_SIZE:
                Runtime.Unsafe_Push(Runtime.MaxStackSize < (int)instruction.Parameter);
                return;
            case BaseInstruction.COMP_PTR:
                Runtime.Unsafe_Push(Runtime.CurrentStackSize < (int)instruction.Parameter);
                return;
            case BaseInstruction.COMP_REG:
                Runtime.Unsafe_Push(Runtime.RegisterSize < (int)instruction.Parameter);
                return;
            #endregion
            default:
                throw new NotImplementedException("Option not implemented");
            }
        }
    }
}
