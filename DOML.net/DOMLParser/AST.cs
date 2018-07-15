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
        public const string BRANCH_INIT = "╦";

        public static string Print(TextWriter writer, bool last, bool children, string indent) {
            writer.Write(indent);
            writer.Write(last ? BRANCH_LAST : BRANCH);
            writer.Write(BAR);
            if (children) writer.Write(BRANCH_INIT);
            return indent + (last ? INDENT + INDENT : V_BAR + INDENT);
        }

        /// <summary>
        /// Just a nice helper to print out the right indents.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="last"></param>
        /// <param name="indent"></param>
        /// <param name="next"></param>
        public static void Print(TextWriter writer, bool last, bool children, string indent, BaseNode next) {
            writer.Write(indent);
            writer.Write(last ? BRANCH_LAST : BRANCH);
            writer.Write(BAR);
            if (children) writer.Write(BRANCH_INIT);
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
                writer.Write($"{INDENT}{Enum.GetName(typeof(Opcodes), opcode).Replace("_", "").ToLower()} ");
            } else {
                writer.Write($"{INDENT}{(byte)opcode} ");
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
                Symbol.Print(writer, i == children.Length - 1, true, "", children[i]);
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
            InstructionWriter.WriteInstructionOp(writer, Opcodes.PUSH, simple);
            // Check for onedimensional
            if (values[0] is ValueNode) {
                string typeID = InstructionWriter.GetTypeID(((ValueNode)values[0]).obj, simple);
                writer.WriteLine($"{(simple ? "vec" : "6")} {typeID} {values.Count}");
                InstructionWriter.WriteInstructionOp(writer, Opcodes.QUICK_CPY, simple);
                writer.Write($"{(simple ? "vec" : "6")} {typeID}");
                foreach (BaseNode node in values) {
                    writer.Write($" {(node as ValueNode).obj}");
                }
                writer.WriteLine();
            } else {
                // @TODO: Implement non one dimensional arrays
                throw new NotImplementedException();
            }
        }

        public override IEnumerable<Instruction> GetInstructions() {
            int typeID = InstructionWriter.GetTypeID(values[0]);
            yield return new Instruction(Opcodes.PUSH, new object[] { 6, typeID, values.Count });
            if (values[0] is ValueNode) {
                // Simpler for 1D
                object[] parameters = new object[values.Count + 2];
                parameters[0] = 6;
                parameters[1] = typeID;

                for (int i = 0; i < values.Count; i++) {
                    parameters[2 + i] = ((ValueNode)values[i]).obj;
                }
                yield return new Instruction(Opcodes.QUICK_CPY, parameters);
            } else {
                throw new NotImplementedException();
            }
        }

        public override void Print(TextWriter writer, string indent) {
            writer.WriteLine($"{InstructionWriter.GetTypeID(values[0], true)}[{values.Count}]");
            for (int i = 0; i < values.Count; i++) {
                Symbol.Print(writer, i == values.Count - 1, false, indent, values[i]);
            }
        }

        public override bool Verify(TextWriter err) {
            throw new NotImplementedException();
        }
    }

    public sealed class MapNode : BaseNode {
        public Dictionary<BaseNode, BaseNode> map = new Dictionary<BaseNode, BaseNode>();

        public override void BasicCodegen(TextWriter writer, bool simple) {
            InstructionWriter.WriteInstructionOp(writer, Opcodes.PUSH, simple);
            KeyValuePair<BaseNode, BaseNode> firstkvp = map.First();
            // Optimisation if they all are value nodes
            if (firstkvp.Key is ValueNode firstKey && firstkvp.Value is ValueNode firstValue) {
                string keyTypeID = InstructionWriter.GetTypeID(firstKey.obj, simple);
                string valueTypeID = InstructionWriter.GetTypeID(firstValue.obj, simple);

                writer.WriteLine($"{(simple ? "map" : "7")} {keyTypeID} {valueTypeID}");
                InstructionWriter.WriteInstructionOp(writer, Opcodes.QUICK_CPY, simple);
                writer.Write($"{(simple ? "map" : "7")} {keyTypeID} {valueTypeID}");
                foreach (KeyValuePair<BaseNode, BaseNode> kvp in map) {
                    ValueNode key = (ValueNode)kvp.Key;
                    ValueNode value = (ValueNode)kvp.Value;
                    writer.Write($" {key.obj} {value.obj}");
                }
                writer.WriteLine();
            } else {
                throw new NotImplementedException();
            }
        }

        public override IEnumerable<Instruction> GetInstructions() {
            KeyValuePair<BaseNode, BaseNode> firstkvp = map.First();
            // Optimisation if they all are value nodes
            if (firstkvp.Key is ValueNode firstKey && firstkvp.Value is ValueNode firstValue) {
                string keyTypeID = InstructionWriter.GetTypeID(firstKey.obj, false);
                string valueTypeID = InstructionWriter.GetTypeID(firstValue.obj, false);
                yield return new Instruction(Opcodes.PUSH, new object[] { 7, keyTypeID, valueTypeID });

                object[] parameters = new object[map.Count*2 + 3];
                parameters[0] = 7;
                parameters[1] = keyTypeID;
                parameters[2] = valueTypeID;

                int i = 3;
                foreach (KeyValuePair<BaseNode, BaseNode> kvp in map) {
                    ValueNode key = (ValueNode)kvp.Key;
                    ValueNode value = (ValueNode)kvp.Value;
                    parameters[i++] = key.obj;
                    parameters[i++] = value.obj;
                }
                yield return new Instruction(Opcodes.QUICK_CPY, parameters);
            } else {
                throw new NotImplementedException();
            }
        }

        public override void Print(TextWriter writer, string indent) {
            KeyValuePair<BaseNode, BaseNode> firstkvp = map.First();
            // Optimisation if they all are value nodes
            if (firstkvp.Key is ValueNode firstKey && firstkvp.Value is ValueNode firstValue) {

                writer.WriteLine($"[{InstructionWriter.GetTypeID(firstKey.obj, true)} : {InstructionWriter.GetTypeID(firstValue.obj, true)}]({map.Count})");
                int i = 1;
                foreach (KeyValuePair<BaseNode, BaseNode> kvp in map) {
                    string newIndent = Symbol.Print(writer, i == map.Count, true, indent);
                    writer.WriteLine("Key Value Pair");
                    Symbol.Print(writer, false, false, newIndent, kvp.Key);
                    Symbol.Print(writer, true, false, newIndent, kvp.Value);
                    i++;
                }
            } else {
                throw new NotImplementedException();
            }
        }

        public override bool Verify(TextWriter err) {
            throw new NotImplementedException();
        }
    }

    public sealed class DummyNode : MacroNode {
        public override void BasicCodegen(TextWriter writer, bool simple) { }

        public override IEnumerable<Instruction> GetInstructions() { yield return new Instruction(Opcodes.NOP, new object[0]); }

        public override MacroNode ParseNode(Parser parser) { return new DummyNode(); }

        public override void Print(TextWriter writer, string indent) { }

        public override bool Verify(TextWriter err) { return true; }
    }

    public abstract class MacroNode : BaseNode {
        public abstract MacroNode ParseNode(Parser parser);
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
            writer.WriteLine($"{InstructionWriter.GetTypeID(obj, simple)} {obj.ToString()}");
        }

        public override IEnumerable<Instruction> GetInstructions() {
            yield return new Instruction(Opcodes.PUSH, new object[] { InstructionWriter.GetTypeID(obj, false), obj });
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
                Symbol.Print(writer, true, false, indent, value);
            }
        }

        public override bool Verify(TextWriter err) {
            throw new NotImplementedException();
        }
    }

    public sealed class IRBlockNode : BaseNode {
        public List<Instruction> instructions;

        public override void BasicCodegen(TextWriter writer, bool simple) {

        }

        public override IEnumerable<Instruction> GetInstructions() {
            foreach (Instruction instruction in instructions) {
                yield return instruction;
            }
        }

        public override void Print(TextWriter writer, string indent) {
            writer.WriteLine("IR-Block");
        }

        public override bool Verify(TextWriter err) {
            // @TODO
            return true;
        }
    }

    public sealed class FunctionNode : BaseNode {
        public string name;
        public ObjectNode obj;
        public FunctionType type;
        public ArgumentNode[] args;

        public FunctionDefinition GetDefinition() {
            switch (type) {
            case FunctionType.CONSTRUCTOR: return InstructionRegister.GetConstructor(obj.type + "::" + name);
            case FunctionType.GETTER: return InstructionRegister.GetGetter(obj.type + "::" + name);
            case FunctionType.SETTER: return InstructionRegister.GetSetter(obj.type + "::" + name);
            default: throw new NotImplementedException();
            }
        }

        public override void BasicCodegen(TextWriter writer, bool simple) {
            // @Optimisation, check if each arg has same type to perform quick call
            foreach (ArgumentNode arg in args) {
                arg.BasicCodegen(writer, simple);
            }
            InstructionWriter.WriteInstructionOp(writer, Opcodes.CALL, simple);
            writer.WriteLine($"{(simple ? "#" + obj.name : obj.registerIndex.ToString())} {obj.type}::{name}");
        }

        public override IEnumerable<Instruction> GetInstructions() {
            foreach (Instruction instruction in args.SelectMany(x => x.GetInstructions())) {
                yield return instruction;
            }

            yield return new Instruction(Opcodes.CALL, new object[] { GetDefinition(), obj.registerIndex });
        }

        public override void Print(TextWriter writer, string indent) {
            writer.WriteLine(type.ToString() + " : " + obj + "." + name);
            for (int i = 0; i < args.Length; i++) {
                bool last = (i == args.Length - 1);
                Symbol.Print(writer, last, args[i].name != null || !(args[i].value is ValueNode), indent, args[i]);
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
            writer.WriteLine($"{(simple ? "#" + name : registerIndex.ToString())} {type}::{constructor.name}");
        }

        public override IEnumerable<Instruction> GetInstructions() {
            foreach (Instruction instruction in constructor.GetInstructions()) {
                yield return instruction;
            }
            yield return new Instruction(Opcodes.NEW_OBJ, new object[] { registerIndex, constructor.GetDefinition() });
        }

        public override void Print(TextWriter writer, string indent) {
            writer.WriteLine(name + " : " + type);
            Symbol.Print(writer, true, constructor.args.Length > 0, indent, constructor);
        }

        public override bool Verify(TextWriter err) {
            return name != null && type != null && name != type &&
                constructor.Verify(err) && constructor.type == FunctionType.CONSTRUCTOR;
        }
    }
}
