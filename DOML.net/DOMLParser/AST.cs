using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using DOML.IR;
using System.Linq;

namespace DOML.AST {
    public enum FunctionType {
        GETTER,
        SETTER,
        CONSTRUCTOR,
    }

    public static class Symbol {
        public const char BRANCH_LAST = '╚';
        public const char BRANCH = '╠';
        public const char BAR = '═';
        public const char V_BAR = '║';
        public const string INDENT = " ";

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
            next?.Print(writer, indent + (last ? INDENT + INDENT : V_BAR + INDENT));
        }
    }

    public static class InstructionWriter {
        public const string INDENT = "  ";

        public static int GetTypeID(object obj) {
            if (obj is long) return 0;
            if (obj is double) return 1;
            if (obj is decimal) return 2;
            if (obj is string) return 3;
            if (obj is bool) return 4;
            return 5;
        }

        public static string GetTypeID(object obj, bool simple) {
            if (simple) {
                if (obj is long) return "int";
                if (obj is double) return "flt";
                if (obj is decimal) return "dec";
                if (obj is string) return "str";
                if (obj is bool) return "bool";
                return "obj";
            } else {
                if (obj is long) return "0";
                if (obj is double) return "1";
                if (obj is decimal) return "2";
                if (obj is string) return "3";
                if (obj is bool) return "4";
                return "5";
            }
        }

        public static void WriteHeader(TextWriter writer, bool simple) {
            if (simple) {
                writer.WriteLine("#IR simple {");
            } else {
                writer.WriteLine("#IR {");
            }
        }

        public static void WriteInstructionOp(TextWriter writer, Opcodes opcode, bool simple) {
            if (simple) {
                writer.Write($"{INDENT}{Enum.GetName(typeof(Opcodes), opcode)} ");
            } else {
                writer.Write($"{INDENT}{opcode} ");
            }
        }
    }

    // More like an 'interface' tbh maybe make into one?
    public abstract class BaseNode {
        public abstract void Print(TextWriter writer, string indent);
        public abstract void BasicCodegen(TextWriter writer, bool simple);
        public abstract IEnumerable<Instruction> GetInstructions();
        public abstract bool Verify(TextWriter err);
        public virtual void Init(ref int registerIndex) { }
    }

    public sealed class TopLevelNode {
        public BaseNode[] children;
        public bool errorOccurred;
        public int maxObjects;
        public int maxValues;

        public void BasicCodegen(TextWriter writer, bool simple) {
            InstructionWriter.WriteHeader(writer, simple);
            InstructionWriter.WriteInstructionOp(writer, Opcodes.INIT, simple);
            writer.WriteLine($"{maxValues} {maxObjects}");
            foreach (BaseNode child in children) {
                child.BasicCodegen(writer, simple);
            }
            writer.WriteLine("}");
        }

        public TopLevelNode(BaseNode[] children, bool error, int maxObj, int maxVal) {
            this.children = children;
            this.errorOccurred = error;
            this.maxObjects = maxObj;
            this.maxValues = maxVal;
            Init();
        }

        public void Init() {
            int index = 0;
            foreach (BaseNode child in children) {
                child.Init(ref index);
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
            writer.WriteLine("Top Level");
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

    public sealed class ArrayNode : BaseNode {
        public List<BaseNode> values = new List<BaseNode>();

        public override void BasicCodegen(TextWriter writer, bool simple) {
            InstructionWriter.WriteInstructionOp(writer, Opcodes.PUSH_ARRAY, simple);
            // Check for onedimensional
            if (values[0] is ValueNode) {
                string typeID = InstructionWriter.GetTypeID(((ValueNode)values[0]).obj, simple);
                writer.WriteLine($"{typeID} {values.Count}");
                InstructionWriter.WriteInstructionOp(writer, Opcodes.ARRAY_CPY, simple);
                writer.Write($"{typeID} {values.Count}");
                foreach (BaseNode node in values) {
                    writer.Write($" {(node as ValueNode).obj.ToString()}");
                }
                writer.WriteLine();
            } else {
                // @TODO: Implement non one dimensional arrays
                throw new NotImplementedException();
            }
        }

        public override IEnumerable<Instruction> GetInstructions() {
            int typeID = InstructionWriter.GetTypeID(values[0]);
            yield return new Instruction(Opcodes.PUSH_ARRAY, new object[] { typeID, values.Count });
            if (values[0] is ValueNode) {
                // Simpler for 1D
                List<object> parameters = new List<object>(values.Count + 2) {
                    typeID, values.Count
                };

                for (int i = 0; i < values.Count; i++) {
                    parameters.Add(((ValueNode)values[i]).obj);
                }
                yield return new Instruction(Opcodes.ARRAY_CPY, parameters.ToArray());
            } else {
                throw new NotImplementedException();
            }
        }

        public override void Print(TextWriter writer, string indent) {
            writer.WriteLine($"{InstructionWriter.GetTypeID(values[0], true)}[{values.Count}]");
            for (int i = 0; i < values.Count; i++) {
                Symbol.Print(writer, i == values.Count - 1, indent, values[i]);
            }
        }

        public override bool Verify(TextWriter err) {
            throw new NotImplementedException();
        }
    }

    public sealed class MapNode : BaseNode {
        public Dictionary<BaseNode, BaseNode> map = new Dictionary<BaseNode, BaseNode>();

        public override void BasicCodegen(TextWriter writer, bool simple) {
            throw new NotImplementedException();
        }

        public override IEnumerable<Instruction> GetInstructions() {
            throw new NotImplementedException();
        }

        public override void Print(TextWriter writer, string indent) {
            writer.WriteLine(map.ToString());
        }

        public override bool Verify(TextWriter err) {
            throw new NotImplementedException();
        }
    }

    public class MacroNode : BaseNode {
        public override void BasicCodegen(TextWriter writer, bool simple) {
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

    public sealed class ReserveNode : BaseNode {
        public object data;

        public override void BasicCodegen(TextWriter writer, bool simple) {
            throw new InvalidOperationException();
        }

        public override IEnumerable<Instruction> GetInstructions() {
            throw new InvalidOperationException();
        }

        public override void Print(TextWriter writer, string indent) {
            throw new InvalidOperationException();
        }

        public override bool Verify(TextWriter err) {
            throw new InvalidOperationException();
        }
    }

    public sealed class ValueNode : BaseNode {
        public object obj;

        public override void BasicCodegen(TextWriter writer, bool simple) {
            InstructionWriter.WriteInstructionOp(writer, Opcodes.PUSH, simple);
            writer.WriteLine($"{InstructionWriter.GetTypeID(obj, simple)} 1 {obj.ToString()}");
        }

        public override IEnumerable<Instruction> GetInstructions() {
            yield return new Instruction(Opcodes.PUSH, new object[] { InstructionWriter.GetTypeID(obj, false), 1, obj });
        }

        public override void Print(TextWriter writer, string indent) {
            writer.Write(obj);
            writer.WriteLine($" ({InstructionWriter.GetTypeID(obj, true)})");
        }

        public override bool Verify(TextWriter err) {
            throw new NotImplementedException();
        }
    }

    public sealed class ArgumentNode : BaseNode {
        public string name;
        public BaseNode value; // To allow for functions

        public override void BasicCodegen(TextWriter writer, bool simple) {
            value.BasicCodegen(writer, simple);
        }

        public override IEnumerable<Instruction> GetInstructions() {
            return value.GetInstructions();
        }

        public override void Print(TextWriter writer, string indent) {
            if (name == null) {
                value.Print(writer, indent);
            } else {
                writer.WriteLine(name);
                Symbol.Print(writer, true, indent, value);
            }
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

        public override void BasicCodegen(TextWriter writer, bool simple) {
            // @Optimisation, check if each arg has same type to perform quick call
            foreach (ArgumentNode arg in args) {
                arg.BasicCodegen(writer, simple);
            }
            InstructionWriter.WriteInstructionOp(writer, Opcodes.CALL_N, simple);
            writer.WriteLine($"{(simple ? obj.name : obj.registerIndex + "")} {obj.type} {name} {args.Length}");
        }

        public override IEnumerable<Instruction> GetInstructions() {
            return args.SelectMany(x => x.GetInstructions());
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
        public int registerIndex;

        public override void Init(ref int registerIndex) {
            this.registerIndex = registerIndex++;
        }

        public override void BasicCodegen(TextWriter writer, bool simple) {
            if (constructor.args.Length > 0) {
                foreach (ArgumentNode node in constructor.args) {
                    node.BasicCodegen(writer, simple);
                }
            }
            InstructionWriter.WriteInstructionOp(writer, Opcodes.NEW_OBJ, simple);
            writer.WriteLine($"{type} {constructor.name} {(simple ? "#" + name : "" + registerIndex)} {constructor.args.Length}");
        }

        public override IEnumerable<Instruction> GetInstructions() {
            foreach (Instruction instruction in constructor.GetInstructions()) {
                yield return instruction;
            }
            yield return new Instruction(Opcodes.NEW_OBJ, new object[] { type, constructor.name, registerIndex, constructor.args.Length });
        }

        public override void Print(TextWriter writer, string indent) {
            writer.WriteLine(name + " : " + type);
            Symbol.Print(writer, true, indent, constructor);
        }

        public override bool Verify(TextWriter err) {
            return name != null && type != null && name != type &&
                constructor.Verify(err) && constructor.type == FunctionType.CONSTRUCTOR;
        }
    }
}
