using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using DOML.Logger;

namespace DOML.ByteCode
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

        public Instruction(BaseInstruction opcode, object parameter)
        {
            OpCode = (byte)opcode;
            Parameter = parameter;
        }
    }

    public class ByteCodeWriter : IDisposable
    {
        private StreamWriter writer;

        public ByteCodeWriter(string filePath, bool append)
        {
            FileStream stream = File.Exists(filePath) ? File.Open(filePath, FileMode.Truncate) : new FileStream(filePath, append ? FileMode.Append : FileMode.Create);
            writer = new StreamWriter(stream);
        }

        ~ByteCodeWriter()
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
                writer.WriteLine("{0,-10} {1,-30} ; {2,10}", GetInstructionName((BaseInstruction)instruction.OpCode), GetParameterText(instruction).Trim(), GetCommentEmit(instruction));
            }
            else
            {
                writer.WriteLine("{0,-10} {1,-30}", GetInstructionName((BaseInstruction)instruction.OpCode), GetParameterText(instruction));
            }
        }

        public string GetCommentEmit(Instruction instruction)
        {
            switch ((BaseInstruction)instruction.OpCode)
            {
            case BaseInstruction.NOP:
                return $"Deliberate void instruction";
            case BaseInstruction.PANIC:
                return $"Panic if top value equals {GetParameterText(instruction)}";
            case BaseInstruction.COMMENT:
                return $"USER COMMENT";
            case BaseInstruction.MAKE_SPACE:
                return $"Reserves {GetParameterText(instruction)} space" + ((int)instruction.Parameter != 1 ? "s" : "") + " on the stack.";
            case BaseInstruction.MAKE_REG:
                return $"Reserves {GetParameterText(instruction)} space" + ((int)instruction.Parameter != 1 ? "s" : "") + " on the register.";

            case BaseInstruction.SET:
                return $"Runs the {GetParameterText(instruction)} function";
            case BaseInstruction.COPY:
                return $"Copies top value {GetParameterText(instruction)} time" + ((int)instruction.Parameter != 1 ? "s" : "") + ", aka a peek and push";
            case BaseInstruction.CLEAR:
                return $"Clears entire stack";
            case BaseInstruction.CLEAR_REG:
                return $"Clears all registers";
            case BaseInstruction.REG_OBJ:
                return $"Registers top object to index {GetParameterText(instruction)} after popping it off the stack";
            case BaseInstruction.UNREG_OBJ:
                return $"Unregisters object at index {GetParameterText(instruction)}";

            case BaseInstruction.PUSH_OBJ:
                return $"Pushes object in register ID: {GetParameterText(instruction)} onto the stack";
            case BaseInstruction.PUSH_8I:
                return $"Pushes signed byte {GetParameterText(instruction)} onto the stack";
            case BaseInstruction.PUSH_16I:
                return $"Pushes short integer {GetParameterText(instruction)} onto the stack";
            case BaseInstruction.PUSH_32I:
                return $"Pushes integer {GetParameterText(instruction)} onto the stack";
            case BaseInstruction.PUSH_64I:
                return $"Pushes long integer {GetParameterText(instruction)} onto the stack";
            case BaseInstruction.PUSH_8U:
                return $"Pushes unsigned byte {GetParameterText(instruction)} onto the stack";
            case BaseInstruction.PUSH_16U:
                return $"Pushes unsigned short integer {GetParameterText(instruction)} onto the stack";
            case BaseInstruction.PUSH_32U:
                return $"Pushes unsigned integer {GetParameterText(instruction)} onto the stack";
            case BaseInstruction.PUSH_64U:
                return $"Pushes unsigned long integer {GetParameterText(instruction)} onto the stack";
            case BaseInstruction.PUSH_16F:
                return $"Pushes half {GetParameterText(instruction)} onto the stack";
            case BaseInstruction.PUSH_32F:
                return $"Pushes float {GetParameterText(instruction)} onto the stack";
            case BaseInstruction.PUSH_64F:
                return $"Pushes double {GetParameterText(instruction)} onto the stack";
            case BaseInstruction.PUSH_80F:
                return $"Pushes extended precision {GetParameterText(instruction)} onto the stack";
            case BaseInstruction.PUSH_128F:
                return $"Pushes quad {GetParameterText(instruction)} onto the stack";
            case BaseInstruction.PUSH_STR:
                return $"Pushes string \"{GetParameterText(instruction)}\" onto the stack";
            case BaseInstruction.PUSH_CHAR:
                return $"Pushes character \'{GetParameterText(instruction)}\' onto the stack";
            case BaseInstruction.PUSH_BOOL:
                return $"Pushes boolean {GetParameterText(instruction)} onto the stack";
            case BaseInstruction.PUSH:
                return $"Performs an unsafe push, pushing {GetParameterText(instruction)} onto the stack regardless of its type";
            case BaseInstruction.CALL:
                return $"Performs a getter call on {GetParameterText(instruction)} and pushes the values onto the stack";
            case BaseInstruction.NEW:
                return $"Performs a constructor call on {GetParameterText(instruction)} and pushes the new object onto the stack";
            case BaseInstruction.POP:
                return $"Pops top object off the stack {GetParameterText(instruction)} time" + ((int)instruction.Parameter != 1 ? "s" : "");

            case BaseInstruction.COMP_SIZE:
                return $"Pushes true onto the stack if the max stack size is less than {GetParameterText(instruction)} else it'll push false";
            case BaseInstruction.COMP_PTR:
                return $"Pushes true onto the stack if the current stack size is less than {GetParameterText(instruction)} else it'll push false";
            case BaseInstruction.COMP_REG:
                return $"Pushes true onto the stack if the current register size is less than {GetParameterText(instruction)} else it'll push false";

            default:
                Log.Error("Can't get comment emit on this instruction.");
                return string.Empty;
            }
        }

        public string GetInstructionName(BaseInstruction instruction)
        {
            if (instruction == BaseInstruction.COMMENT) return ";"; // This is just so it well 1) looks nicer and 2) is more inline with the spec
            string lower = instruction.ToString().ToLower();
            return lower.Contains("_") ? lower.Replace("_", "") : lower;
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
            switch ((BaseInstruction)instruction.OpCode)
            {
            case BaseInstruction.PUSH_STR:
                return '"' + instruction.Parameter.ToString() + '"';
            case BaseInstruction.PUSH_CHAR:
                return "'" + instruction.Parameter.ToString() + "'";
            case BaseInstruction.SET:
            case BaseInstruction.NEW:
            case BaseInstruction.CALL:
                return instruction.Parameter.ToString().Split(' ')[1];
            default:
                return instruction.Parameter.ToString();
            }
        }
    }
}
