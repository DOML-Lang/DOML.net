#region License
// ====================================================
// Team DOML Copyright(C) 2017 Team DOML
// This program comes with ABSOLUTELY NO WARRANTY; This is free software, 
// and you are welcome to redistribute it under certain conditions; See 
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion

namespace DOML.IR
{
    /// <summary>
    /// An instruction consists of just an opcode and a parameter.
    /// </summary>
    public struct Instruction
    {
        /// <summary>
        /// The opcode for the instruction.
        /// </summary>
        public readonly byte OpCode;

        /// <summary>
        /// The parameter of the command.
        /// </summary>
        public readonly object Parameter;

        /// <summary>
        /// Create a new instruction from the byte value of the opcode and the parameter.
        /// </summary>
        /// <param name="opcode"> The byte code value. </param>
        /// <param name="parameter"> The parameter. </param>
        public Instruction(byte opcode, object parameter)
        {
            OpCode = opcode;
            Parameter = parameter;
        }

        /// <summary>
        /// Create a new instruction from the opcode and the parameter.
        /// </summary>
        /// <param name="opcode"> The opcode. </param>
        /// <param name="parameter"> The parameter. </param>
        public Instruction(Opcodes opcode, object parameter)
        {
            OpCode = (byte)opcode;
            Parameter = parameter;
        }
    }
}
