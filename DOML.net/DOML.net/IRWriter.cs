#region License
// ====================================================
// Team DOML Copyright(C) 2017 Team DOML
// This program comes with ABSOLUTELY NO WARRANTY; This is free software, 
// and you are welcome to redistribute it under certain conditions; See 
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion

using System;
using System.Text;
using System.IO;
using DOML.Logger;

namespace DOML.IR
{
    /// <summary>
    /// An instruction consists of just an opcode
    /// and a parameter.
    /// </summary>
    public struct Instruction
    {
        public readonly byte OpCode;
        public readonly object Parameter;

        public Instruction(byte opcode, object parameter)
        {
            OpCode = opcode;
            Parameter = parameter;
        }

        public Instruction(Opcodes opcode, object parameter)
        {
            OpCode = (byte)opcode;
            Parameter = parameter;
        }
    }

    public class IRWriter : IDisposable
    {
        private TextWriter writer;

        public IRWriter(string filePath, bool append)
        {
            FileStream stream = File.Exists(filePath) ? File.Open(filePath, FileMode.Truncate) : new FileStream(filePath, append ? FileMode.Append : FileMode.Create);
            writer = new StreamWriter(stream);
        }

        public IRWriter(StringBuilder resultText)
        {
            writer = new StringWriter(resultText);
        }

        ~IRWriter()
        {
            Finish();
        }

        public void WriteHeader()
        {
            writer.WriteLine("; This is the resulting bytecode from the file given");
        }

        public void WriteInstructionText(Instruction instruction, bool withComment)
        {
            if (withComment)
            {
                writer.WriteLine("{0,-15} {1,-50} ; {2,10}", ((Opcodes)instruction.OpCode).ToString(), GetParameterText(instruction).Trim(), GetCommentEmit(instruction));
            }
            else
            {
                writer.WriteLine("{0,-15} {1,-50}", ((Opcodes)instruction.OpCode).ToString(), GetParameterText(instruction));
            }
        }

        public string GetCommentEmit(Instruction instruction)
        {
            switch ((Opcodes)instruction.OpCode)
            {
            case Opcodes.NOP:
                return $"Deliberate void instruction";
            case Opcodes.PANIC:
                return $"Panic if top value equals {GetParameterText(instruction)}";
            case Opcodes.COMMENT:
                return $"USER COMMENT";
            case Opcodes.MAKE_SPACE:
                return $"Reserves {GetParameterText(instruction)} space" + ((int)instruction.Parameter != 1 ? "s" : "") + " on the stack.";
            case Opcodes.MAKE_REG:
                return $"Reserves {GetParameterText(instruction)} space" + ((int)instruction.Parameter != 1 ? "s" : "") + " on the register.";

            case Opcodes.SET:
                return $"Runs the {GetParameterText(instruction)} function";
            case Opcodes.COPY:
                return $"Copies top value {GetParameterText(instruction)} time" + ((int)instruction.Parameter != 1 ? "s" : "") + ", aka a peek and push";
            case Opcodes.CLEAR:
                return $"Clears entire stack";
            case Opcodes.CLEAR_REG:
                return $"Clears all registers";
            case Opcodes.REG_OBJ:
                return $"Registers top object to index {GetParameterText(instruction)} after popping it off the stack";
            case Opcodes.UNREG_OBJ:
                return $"Unregisters object at index {GetParameterText(instruction)}";

            case Opcodes.PUSH_OBJ:
                return $"Pushes object in register ID: {GetParameterText(instruction)} onto the stack";
            case Opcodes.PUSH_INT:
                return $"Pushes long integer {GetParameterText(instruction)} onto the stack";
            case Opcodes.PUSH_NUM:
                return $"Pushes double {GetParameterText(instruction)} onto the stack";
            case Opcodes.PUSH_STR:
                return $"Pushes string \"{GetParameterText(instruction)}\" onto the stack";
            case Opcodes.PUSH_CHAR:
                return $"Pushes character \'{GetParameterText(instruction)}\' onto the stack";
            case Opcodes.PUSH_BOOL:
                return $"Pushes boolean {GetParameterText(instruction)} onto the stack";
            case Opcodes.PUSH:
                return $"Performs an unsafe push, pushing {GetParameterText(instruction)} onto the stack regardless of its type";
            case Opcodes.CALL:
                return $"Performs a getter call on {GetParameterText(instruction)} and pushes the values onto the stack";
            case Opcodes.NEW:
                return $"Performs a constructor call on {GetParameterText(instruction)} and pushes the new object onto the stack";
            case Opcodes.POP:
                return $"Pops top object off the stack {GetParameterText(instruction)} time" + ((int)instruction.Parameter != 1 ? "s" : "");

            case Opcodes.COMP_MAX:
                return $"Pushes true onto the stack if the max stack size is less than {GetParameterText(instruction)} else it'll push false";
            case Opcodes.COMP_SIZE:
                return $"Pushes true onto the stack if the current stack size is less than {GetParameterText(instruction)} else it'll push false";
            case Opcodes.COMP_REG:
                return $"Pushes true onto the stack if the current register size is less than {GetParameterText(instruction)} else it'll push false";

            default:
                Log.Error("Can't get comment emit on this instruction.");
                return string.Empty;
            }
        }

        public void Finish()
        {
            writer.Dispose();
        }

        public void Dispose()
        {
            writer.Dispose();
        }

        private string GetParameterText(Instruction instruction)
        {
            switch ((Opcodes)instruction.OpCode)
            {
            case Opcodes.PUSH_STR:
                return '"' + instruction.Parameter.ToString() + '"';
            case Opcodes.PUSH_CHAR:
                return "'" + instruction.Parameter.ToString() + "'";
            case Opcodes.SET:
            case Opcodes.NEW:
            case Opcodes.CALL:
                return instruction.Parameter.ToString().Split(' ')[1];
            default:
                return instruction.Parameter.ToString();
            }
        }
    }
}
