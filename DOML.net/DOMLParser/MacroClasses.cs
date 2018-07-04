using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using DOML.IR;
using System.Linq;

namespace DOML.AST {
    public sealed class IRMacroNode : MacroNode {
        public List<Instruction> nodes = new List<Instruction>();

        public override void BasicCodegen(TextWriter writer, bool simple) {
            foreach (Instruction node in nodes) {
                InstructionWriter.WriteInstructionOp(writer, (Opcodes)node.OpCode, simple);
                foreach (object obj in node.Parameters) {
                    writer.Write($" {obj}");
                }
                writer.WriteLine();
            }
        }

        public override IEnumerable<Instruction> GetInstructions() {
            foreach (Instruction node in nodes) {
                yield return node;
            }
        }

        public override MacroNode ParseNode(TextReader reader, Parser parser) {
            // We need parsing IR to exist in parser first
            throw new NotImplementedException();
        }

        public override void Print(TextWriter writer, string indent) {
            writer.WriteLine("#IR");
            // ? Do we want a better printout?? I'm just concerned it'll make it look noisy
        }

        public override bool Verify(TextWriter err) {
            // ? Error maybe?
            return true;
        }
    }

    public sealed class DeinitMacroNode : MacroNode {
        public override void BasicCodegen(TextWriter writer, bool simple) {
            InstructionWriter.WriteInstructionOp(writer, Opcodes.DE_INIT, simple);
            writer.WriteLine();
        }

        public override MacroNode ParseNode(TextReader reader, Parser parser) {
            parser.IgnoreWhitespace(reader);
            return new DeinitMacroNode();
        }

        public override IEnumerable<Instruction> GetInstructions() {
            yield return new Instruction(Opcodes.DE_INIT, new object[0]);
        }

        public override void Print(TextWriter writer, string indent) {
            writer.WriteLine("#deinit");
        }

        public override bool Verify(TextWriter err) {
            return true;
        }
    }
}
