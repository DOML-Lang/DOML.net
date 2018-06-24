using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using DOML.IR;

namespace DOML.AST {
    public enum FunctionType {
        GETTER,
        SETTER,
        CONSTRUCTOR,
    }

    public static class Symbol {
        public const char BRANCH_LAST = 'L';
        public const char BRANCH = '├';
        public const char BAR = '─';
        public const char V_BAR = '│';
        public const char INDENT = ' ';

        /// <summary>
        /// Just a nice helper to print out the right indents.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="last"></param>
        /// <param name="indent"></param>
        /// <param name="next"></param>
        public static void Print(TextWriter writer, bool last, string indent, BaseNode next) {
            writer.Write(indent);
            writer.Write(last ? BRANCH_LAST : BRANCH);
            writer.Write(BAR);
            next?.Print(writer, indent + (last ? INDENT : V_BAR + INDENT));
        }
    }

    // More like an 'interface' tbh maybe make into one?
    public abstract class BaseNode {
        public abstract void Print(TextWriter writer, string indent);
        public abstract void BasicCodegen(TextWriter writer);
        public abstract IEnumerable<Instruction> GetInstructions();
        public abstract bool Verify(TextWriter err);
    }

    public sealed class TopLevelNode {
        public BaseNode[] children;
        public bool errorOccurred;

        public void BasicCodegen(TextWriter writer) {
            foreach (BaseNode child in children) {
                child.BasicCodegen(writer);
            }
        }

        public IEnumerable<Instruction> GetInstructions() {
            foreach (BaseNode child in children) {
                foreach (Instruction instruction in child.GetInstructions()) {
                    yield return instruction;
                }
            }
        }

        public void Print(TextWriter writer) {
            for (int i = 0; i < children.Length; i++) {
                Symbol.Print(writer, i == children.Length - 1, "", children[i]);
            }
        }

        public bool Verify(TextWriter err) {
            if (errorOccurred) return false;

            foreach (BaseNode child in children) {
                if (!child.Verify(err)) return false;
            }
            return true;
        }
    }

    // Another abstract???
    public sealed class MacroNode : BaseNode {
        public override void BasicCodegen(TextWriter writer) {
            throw new NotImplementedException();
        }

        public override IEnumerable<Instruction> GetInstructions() {
            throw new NotImplementedException();
        }

        public override void Print(TextWriter writer, string indent) {

        }

        public override bool Verify(TextWriter err) {
            throw new NotImplementedException();
        }
    }

    public sealed class ValueNode : BaseNode {
        public object obj;

        public override void BasicCodegen(TextWriter writer) {
            throw new NotImplementedException();
        }

        public override IEnumerable<Instruction> GetInstructions() {
            throw new NotImplementedException();
        }

        public override void Print(TextWriter writer, string indent) {
            writer.WriteLine(obj.ToString());
        }

        public override bool Verify(TextWriter err) {
            throw new NotImplementedException();
        }
    }

    public sealed class ArgumentNode : BaseNode {
        public string name;
        public BaseNode value; // To allow for functions

        public override void BasicCodegen(TextWriter writer) {
            throw new NotImplementedException();
        }

        public override IEnumerable<Instruction> GetInstructions() {
            throw new NotImplementedException();
        }

        public override void Print(TextWriter writer, string indent) {
            writer.WriteLine(name);
            Symbol.Print(writer, true, indent, value);
        }

        public override bool Verify(TextWriter err) {
            throw new NotImplementedException();
        }
    }

    public sealed class FunctionNode : BaseNode {
        public string name;
        public ObjectNode obj;
        public FunctionType type;
        public ArgumentNode[] args;

        public override void BasicCodegen(TextWriter writer) {
            throw new NotImplementedException();
        }

        public override IEnumerable<Instruction> GetInstructions() {
            throw new NotImplementedException();
        }

        public override void Print(TextWriter writer, string indent) {
            writer.WriteLine(type.ToString() + " : " + obj + "." + name);
            for (int i = 0; i < args.Length; i++) {
                bool last = (i == args.Length - 1);
                Symbol.Print(writer, last, indent, args[i]);
            }
        }

        public override bool Verify(TextWriter err) {
            throw new NotImplementedException();
        }
    }

    public sealed class ObjectNode : BaseNode {
        public string name;
        public string type;
        public FunctionNode constructor;

        public override void BasicCodegen(TextWriter writer) {
            throw new NotImplementedException();
        }

        public override IEnumerable<Instruction> GetInstructions() {
            throw new NotImplementedException();
        }

        public override void Print(TextWriter writer, string indent) {
            writer.WriteLine(name + " : " + type);
            Symbol.Print(writer, true, indent, constructor);
        }

        public override bool Verify(TextWriter err) {
            throw new NotImplementedException();
        }
    }
}
