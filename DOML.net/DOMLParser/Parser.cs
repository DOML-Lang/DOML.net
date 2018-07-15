#region License
// ====================================================
// Team DOML Copyright(C) 2017 Team DOML
// This program comes with ABSOLUTELY NO WARRANTY; This is free software, 
// and you are welcome to redistribute it under certain conditions; See 
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion

// @BUG: Currently minimum for objects is '6' for no real reason?

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DOML.AST;
using DOML.Logger;

namespace DOML {
    /// <summary>
    /// The parser for DOML and IR.
    /// </summary>
    public class Parser {
        [Flags]
        public enum Settings {
            JUST_IR = 1 << 0,
            KEEP_COMMENTS = 1 << 1,
            BINARY_NATIVE = 1 << 2,
            BINARY_LENGTH = 1 << 3,
        }

        private static Dictionary<string, Func<Parser, MacroNode>> macros = new Dictionary<string, Func<Parser, MacroNode>>() {
            ["ir"] = new IRMacroNode().ParseNode,
            ["deinit"] = new DeinitMacroNode().ParseNode,
            ["version"] = HandleVersion,
            ["strict"] = HandleStrict,
            ["nokeywords"] = HandleNoKeywords,
        };

        public const string CompilerVersion = "0.3";

        private Settings settings;

        public Tokenizer tok;

        /// <summary>
        /// Current variable for the `...` statements.
        /// </summary>
        private ObjectNode currentVariable;

        /// <summary>
        /// Registers to use.
        /// </summary>
        private int registers = 0;

        /// <summary>
        /// The maximum amount of spaces this script uses.
        /// </summary>
        private int maxSpaces = 0;

        /// <summary>
        /// Inside a block.
        /// </summary>
        private bool inBlock;

        /// <summary>
        /// #strict true/false.
        /// </summary>
        private bool strict = true;

        /// <summary>
        /// #version x.y.z.
        /// </summary>
        private string version = CompilerVersion;

        /// <summary>
        /// #nokeywords true/false
        /// </summary>
        private bool noKeywords;

        /// <summary>
        /// All the register informtation.
        /// </summary>
        private Dictionary<string, ObjectNode> Registers { get; } = new Dictionary<string, ObjectNode>();

        private Dictionary<string, BaseNode> Definitions { get; } = new Dictionary<string, BaseNode>();

        /// <summary>
        /// Log an error using current line information.
        /// </summary>
        /// <param name="error"> What to log. </param>
        private void LogError(string error) => Log.Error(error, new Log.Information(tok.line, tok.line, tok.col, tok.col));

        /// <summary>
        /// Log an warning using current line information.
        /// </summary>
        /// <param name="warning"> What to log. </param>
        private void LogWarning(string warning) => Log.Warning(warning, new Log.Information(tok.line, tok.line, tok.col, tok.col));

        /// <summary>
        /// Log an info using current line information.
        /// </summary>
        /// <param name="info"> What to log. </param>
        private void LogInfo(string info) => Log.Info(info, new Log.Information(tok.line, tok.line, tok.col, tok.col));

        public Parser(TextReader reader) {
            tok = new Tokenizer(reader);
        }

        /// <summary>
        /// Get an interpreter from a file path.
        /// </summary>
        /// <param name="filePath"> The path to the file to open. </param>
        /// <param name="readMode"> What read mode to use. </param>
        /// <returns> An interpreter instance. </returns>
        //public static Interpreter GetInterpreterFromPath(string filePath, ReadMode readMode = ReadMode.DOML)
        //{
        //    if (File.Exists(filePath))
        //        switch (readMode)
        //        {
        //        case ReadMode.DOML:
        //            using (StreamReader reader = new StreamReader(new FileStream(filePath, FileMode.Open)))
        //                return GetInterpreter();
        //        case ReadMode.IR:
        //            using (StreamReader reader = new StreamReader(new FileStream(filePath, FileMode.Open)))
        //                return GetInterpreterFromIR();
        //        case ReadMode.BINARY_Length:
        //            using (BinaryReader reader = new BinaryReader(new FileStream(filePath, FileMode.Open)))
        //                return GetInterpreterFromBinary(, true);
        //        case ReadMode.BINARY_Native:
        //            using (BinaryReader reader = new BinaryReader(new FileStream(filePath, FileMode.Open)))
        //                return GetInterpreterFromBinary(, false);
        //        default:
        //            throw new NotImplementedException("Case Not Implemented; This is a bug.");
        //        }
        //    else
        //        throw new FileNotFoundException("File Path Invalid");
        //}

        /// <summary>
        /// Get an interpreter from text.
        /// </summary>
        /// <param name="text"> The text to interpret. </param>
        /// <param name="readMode"> What read mode to use. </param>
        /// <returns> An interpreter instance. </returns>
        /// <remarks> This is significantly slower for binary work. </remarks>
        //public static Interpreter GetInterpreterFromText(string text, ReadMode readMode = ReadMode.DOML)
        //{
        //    if (text != null)
        //        switch (readMode)
        //        {
        //        case ReadMode.DOML:
        //            using (StringReader reader = new StringReader(text))
        //                return GetInterpreter();
        //        case ReadMode.IR:
        //            using (StringReader reader = new StringReader(text)) return GetInterpreterFromIR();
        //        case ReadMode.BINARY_Length:
        //            using (BinaryWriter writer = BinaryWriter.Null)
        //            {
        //                foreach (char chr in text)
        //                    writer.Write(Convert.ToByte(chr));

        //                using (BinaryReader reader = new BinaryReader(writer.BaseStream))
        //                    return GetInterpreterFromBinary(, true);
        //            }
        //        case ReadMode.BINARY_Native:
        //            using (BinaryWriter writer = BinaryWriter.Null)
        //            {
        //                foreach (char chr in text)
        //                    writer.Write(Convert.ToByte(chr));

        //                using (BinaryReader reader = new BinaryReader(writer.BaseStream))
        //                    return GetInterpreterFromBinary(, false);
        //            }
        //        default:
        //            throw new NotImplementedException("Case Not Implemented; This is a bug.");
        //        }
        //    else
        //        throw new ArgumentNullException("Text is null");
        //}

        public IRBlockNode ParseIRBlock() {

        }

        // TopLevelNode has an error occurred flag.
        public TopLevelNode ParseAST() {
            List<BaseNode> children = new List<BaseNode>();
            bool success = true;
            while (true) {
                // Remove whitespace/comments before first line
                if (!ParseComments()) return null;
                if (tok.isEOF) break;
                if (tok.currentChar == '#') {
                    MacroNode node = ParseMacro();
                    if (node == null) {
                        Log.Error("Invalid macro");
                        success = false;
                        break;
                    }

                    // Avoids having a lot of NOPs
                    if (!(node is DummyNode)) children.Add(node);
                } else if (tok.currentChar == '}') {
                    if (inBlock) {
                        inBlock = false;
                        tok.Advance();
                        continue; // loop till next
                    } else {
                        LogError("Invalid Character '}'");
                        success = false;
                        break;
                    }
                }

                // Check if we are in block for objects
                if (inBlock) {
                    // Automatic assignment
                    FunctionNode node = ParseAssignment(currentVariable);
                    if (node == null) {
                        success = false;
                        break;
                    }
                    children.Add(node);
                } else {
                    BaseNode node = ParseObjectOrAssignment();
                    if (node == null && !inBlock) {
                        // @Debt: handling `.{` syntax currently requires inBlock to be false
                        return null;
                    } else if (node != null && !(node is DummyNode)) {
                        children.Add(node);
                    }
                }
            }

            // Confirm version
            if (version != CompilerVersion) {
                string[] splitVersion = version.Split('.');
                int x = int.Parse(splitVersion[0]);
                int y = int.Parse(splitVersion[1]);
                int z = int.Parse(splitVersion[2]);
                if (x > 0 || y > 3 || z > 0) {
                    Log.Error($"Invalid Version {version}, this compiler doesn't support above v0.3.0");
                    return null;
                } else if (y < 3) {
                    Log.Error($"Invalid Version {version}, this compiler doesn't support below v0.3");
                    return null;
                }
            }

            Console.WriteLine(version);

            return new TopLevelNode(children.ToArray(), !success, registers, maxSpaces);
        }

        private static DummyNode HandleVersion(Parser parser) {
            StringBuilder version = new StringBuilder();
            parser.tok.IgnoreWhitespace();
            int digitCount = 0;
            while (char.IsDigit(parser.tok.currentChar) || parser.tok.currentChar == '.') {
                if (parser.tok.currentChar == '.') digitCount++;
                version.Append(parser.tok.currentChar);
                parser.tok.Advance();
            }

            if (!char.IsWhiteSpace(parser.tok.currentChar) || digitCount > 2) {
                Log.Error("Invalid Version");
                return null;
            }

            parser.version = version.ToString();
            parser.tok.IgnoreWhitespace();
            return new DummyNode();
        }

        private bool? ParseBool() {
            // @TODO: #true/#false
            tok.IgnoreWhitespace();
            string val = ParseIdentifier("", false).ToString();
            tok.IgnoreWhitespace();
            if (val == "true") return true;
            if (val == "false") return false;
            return null;
        }

        private static MacroNode HandleNoKeywords(Parser parser) {
            if (parser.ParseBool() is bool val) {
                parser.noKeywords = val;
                return new DummyNode();
            } else {
                return null;
            }
        }

        private static MacroNode HandleStrict(Parser parser) {
            if (parser.ParseBool() is bool val) {
                parser.strict = val;
                return new DummyNode();
            } else {
                return null;
            }
        }

        private MacroNode ParseMacro() {
            if (tok.currentChar != '#') {
                Log.Error("Internal error");
                return null;
            }

            // @ERROR. Invalid character '#'
            if (!tok.AdvanceAndIgnoreWS()) return null;

            // Parse identifier
            string identifier = ParseIdentifier("", false).ToString().ToLower();
            if (macros.ContainsKey(identifier)) {
                return macros[identifier](this);
            }

            return null;
            // Should we suggest similar names??
        }

        private FunctionNode ParseAssignment(ObjectNode obj, string name = null) {
            if (name == null) {
                // Passes obj.x = y, from the state of 'x' (post '.')
                name = ParseIdentifier("", true).ToString();
                // Skip ws
                tok.IgnoreWhitespace();
            }

            // Confirm equals
            if (tok.currentChar != '=') {
                LogError("Missing '='");
                return null;
            }

            // Now just parse the 'arg list'
            // @ERROR: early eof
            if (!tok.AdvanceAndIgnoreWS()) return null;
            List<BaseNode> node = ParseArgList();
            if (node == null) return null;
            return new FunctionNode() { args = node.Select(x => new ArgumentNode() { name = null, value = x }).ToArray(), name = name, obj = obj, type = FunctionType.SETTER };
        }

        private bool IsEndingChar(char c) {
            return (c == ';' || c == ']' || c == ',' || c == '}' || char.IsWhiteSpace(c) || c == ')');
        }

        private BaseNode ParseArray(ref int count) {
            ArrayNode values = new ArrayNode();
            while (tok.currentChar != ']' && tok.AdvanceAndIgnoreWS()) {
                BaseNode node = ParseValue(ref count); // note: not propagating possibleFuncArg
                if (node == null || node is ReserveNode) return null; // Error occurred
                values.values.Add(node);
                tok.IgnoreWhitespace();
                if (tok.currentChar != ',' && tok.currentChar != ']') {
                    LogError("Missing ',' or ']'");
                    return null;
                }
            }

            if (values.values.Count == 0) {
                // empty array
                LogError("Can't have empty array, use null instead");
                return null;
            }

            if (tok.currentChar != ']') {
                LogError("Missing ']' character.");
                return null;
            }

            tok.Advance();
            return values;
        }

        // Parses floats and integers
        private BaseNode ParseNumber(ref int count) {
            StringBuilder builder = new StringBuilder();
            bool flt = false;
            while (!IsEndingChar(tok.currentChar)) {
                builder.Append(tok.currentChar);
                if (tok.currentChar == '.' || tok.currentChar == 'e' || tok.currentChar == 'E') flt = true;
                tok.Advance();
            }

            if (flt) {
                // @TODO: confirm it supports hex, oct and bin
                if (builder.Length == 0 || !double.TryParse(builder.ToString(), out double result)) {
                    LogError("Invalid double: " + builder.ToString());
                    return null;
                }
                return new ValueNode() { obj = result };
            } else {
                // @TODO: confirm it supports hex, oct and bin
                if (builder.Length == 0 || !long.TryParse(builder.ToString(), out long result)) {
                    LogError("Invalid integer: " + builder.ToString());
                    return null;
                }
                return new ValueNode() { obj = result };
            }
        }

        private BaseNode ParseString(ref int count) {
            StringBuilder builder = new StringBuilder();
            bool escaped = false;
            while (tok.Advance() && (tok.currentChar != '"' || escaped)) {
                if (tok.currentChar == '\\') {
                    escaped = true;
                } else {
                    // @TODO: other escape codes like whitespace
                    escaped = false;
                    builder.Append(tok.currentChar);
                }
            }

            if (tok.currentChar != '"' && !escaped) {
                LogError("Missing terminating '\"'");
                return null;
            }
            tok.Advance();
            return new ValueNode() { obj = builder.ToString() };
        }

        private BaseNode ParseDecimal(ref int count) {
            tok.Advance(); // skip '$'
            StringBuilder builder = new StringBuilder();
            while (IsEndingChar(tok.currentChar)) {
                builder.Append(tok.currentChar);
            }

            if (builder.Length == 0 || !decimal.TryParse(builder.ToString(), out decimal result)) {
                LogError("Invalid decimal: " + builder.ToString());
                return null;
            }

            return new ValueNode() { obj = result };
        }

        private BaseNode ParseMap(ref int count) {
            MapNode map = new MapNode();
            while (tok.currentChar != '}' && tok.AdvanceAndIgnoreWS()) {
                BaseNode key = ParseValue(ref count);
                if (key == null) return null;

                tok.IgnoreWhitespace();
                if (tok.currentChar != ':') {
                    LogError("Expecting ':'");
                    return null;
                }
                tok.AdvanceAndIgnoreWS();

                BaseNode value = ParseValue(ref count);
                if (value == null) return null;
                tok.IgnoreWhitespace();
                if (tok.currentChar != ',' && tok.currentChar != '}') {
                    LogError("Expecting either ',' or '}'");
                    return null;
                }

                map.map.Add(key, value);
            }
            if (tok.currentChar != '}') {
                LogError("No terminating '}'");
            }

            tok.AdvanceAndIgnoreWS();
            return map;
        }

        // @TODO: refactor this more its too long
        private BaseNode ParseObjectReference(ref int count, bool possibleFuncArg = false) {
            // @TODO: support #NoKeywords
            // Could be boolean, object or null
            // Parse till ',' ']' ')' or an invalid
            StringBuilder identifier = ParseIdentifier("", false);

            if (tok.currentChar != '.') {
                tok.IgnoreWhitespace();
                if (tok.currentChar == ':') {
                    if (possibleFuncArg) {
                        // @Query? Does count have to be reset
                        if (tok.currentChar != ':') {
                            LogError("Internal error expected ':'");
                            return null;
                        }

                        tok.AdvanceAndIgnoreWS();
                        // Parse actual value
                        BaseNode val = ParseValue(ref count);
                        if (val == null) return null;
                        return new ArgumentNode() { name = identifier.ToString(), value = val };
                    } else {
                        LogError("Syntax Error");
                        return null;
                    }
                } else if (tok.currentChar == '=') {
                    LogError("Syntax error");
                    return null;
                }
            }

            string value = identifier.ToString();
            if (value == "true") return new ValueNode() { obj = true };
            if (value == "false") return new ValueNode() { obj = false };
            if (value == "null") return new ValueNode() { obj = null };

            BaseNode objValue = null;
            bool definition = false;
            if (Registers.ContainsKey(value)) objValue = Registers[value];
            else if (Definitions.ContainsKey(value)) {
                objValue = Definitions[value];
                definition = true;
            }

            // Object
            if (objValue == null) {
                // @TODO compare strings to guess name
                LogError($"Invalid value \"{value}\", maybe a typo.");
                return null;
            }

            if (tok.currentChar != '.') {
                return objValue;
            } else if (definition) {
                LogError($"'.' operator not valid on a definition");
                return null;
            }

            // Get other end
            identifier.Clear();
            tok.Advance(); // skip '.'
            while (tok.Advance()) {
                if (char.IsLetter(tok.currentChar)) {
                    identifier.Append(tok.currentChar);
                } else {
                    break;
                }
            }
            tok.IgnoreWhitespace();

            if (tok.currentChar == '=') return null;

            string funcName = identifier.ToString();

            List<ArgumentNode> list = new List<ArgumentNode>();
            if (tok.currentChar == '(') {
                // Parse function list
                list = ParseFuncArgList(ref count);
                if (list == null) {
                    return null;
                }
            }

            return new FunctionNode() { args = list.ToArray(), name = funcName, obj = Registers[value] };
        }

        private BaseNode ParseValue(ref int count, bool possibleFuncArg = false) {
            count++;
            if (tok.currentChar == '[') {
                return ParseArray(ref count);
            } else if (tok.currentChar == '{') {
                return ParseMap(ref count);
            } else if (char.IsDigit(tok.currentChar)) {
                return ParseNumber(ref count);
            } else if (tok.currentChar == '$') {
                return ParseDecimal(ref count);
            } else if (tok.currentChar == '"') {
                return ParseString(ref count);
            } else {
                return ParseObjectReference(ref count, possibleFuncArg);
            }
        }

        private List<ArgumentNode> ParseFuncArgList(ref int count) {
            if (tok.currentChar != '(') {
                LogError("Invalid Syntax");
                return null;
            }

            tok.Advance();
            List<ArgumentNode> arguments = new List<ArgumentNode>();

            if (tok.currentChar == ')') {
                // Empty/void
                return arguments;
            }

            do {
                tok.IgnoreWhitespace();
                BaseNode value = ParseValue(ref count, true);
                if (value == null) return null;
                if (value is ArgumentNode arg) arguments.Add(arg);
                else arguments.Add(new ArgumentNode() { name = null, value = value });
            } while (tok.currentChar != ')' && tok.currentChar == ',' && tok.Advance());

            if (tok.currentChar != ')') {
                LogError("Syntax Error");
                return null;
            }
            tok.Advance();
            return arguments;
        }

        private BaseNode ParseLiteralDefinition(string name) {
            // Definition
            tok.AdvanceAndIgnoreWS();
            int count = 0;
            BaseNode value = ParseValue(ref count);
            // Do we actually care about count??
            if (value == null) {
                return null;
            }

            string definition = name;

            if (Definitions.ContainsKey(definition) && strict) {
                Log.Error("Definition already defined, can't redefine definitions when in strict mode, disable it with #strict false");
                return null;
            } else if (Registers.ContainsKey(definition)) {
                Log.Error("Registers contains object with the same name, can't define a definition with the same name as an object");
                return null;
            }

            Definitions[definition] = value;
            return new DummyNode();
        }

        private ObjectNode ParseConstructor(string objName, string typeName) {
            tok.Advance();
            if (tok.currentChar != ':') {
                LogError("Syntax error");
                return null;
            }

            tok.Advance();
            string functionName;
            if (tok.currentChar == '(') {
                // Empty
                tok.Advance();
                functionName = objName;
            } else {
                functionName = ParseIdentifier("", false).ToString();
            }

            int count = 0;
            List<ArgumentNode> args = ParseFuncArgList(ref count);
            if (count > maxSpaces) maxSpaces = count;
            if (args == null) return null;
            return new ObjectNode() { name = objName, type = typeName, constructor = new FunctionNode() { name = functionName, type = FunctionType.CONSTRUCTOR, args = args.ToArray() } };
        }

        // Checks `x.{` syntax
        private bool CheckBlockSyntax(string name) {
            if (tok.currentChar != '.') {
                throw new InvalidOperationException("Internal Error: was expecting '.'");
            }

            tok.Advance();
            if (tok.currentChar == '{') {
                tok.Advance();
                if (!Registers.ContainsKey(name)) {
                    LogError($"Object doesn't exist {name}");
                    // @Debt: this is for the future when recursive blocks exist
                    // Currently a 'null' is allowed when you have `.{` block
                    // probably should be changed to another format of validation.
                    inBlock = false;
                    return true;
                } else {
                    currentVariable = Registers[name];
                    inBlock = true;
                    return true;
                }
            }
            return false;
        }

        private BaseNode ParseObjectDefinition(string name) {
            tok.IgnoreWhitespace();
            if (tok.currentChar != ':') {
                LogError("Syntax error");
                return null;
            }

            tok.Advance();
            if (tok.currentChar == '=') {
                return ParseLiteralDefinition(name);
            }

            tok.IgnoreWhitespace();
            StringBuilder type = ParseIdentifier("", false);
            ObjectNode objectNode;
            string typeName = type.ToString();
            if (tok.currentChar == ':') {
                objectNode = ParseConstructor(name, typeName);
            } else {
                objectNode = new ObjectNode() { name = name, type = typeName, constructor = new FunctionNode() { name = typeName, type = FunctionType.CONSTRUCTOR, args = new ArgumentNode[0] } };
            }
            registers++;
            objectNode.constructor.obj = objectNode;
            if (Definitions.ContainsKey(name)) {
                Log.Error("Definitions contains a definition with the same name, can't define an object with the same name as a definition");
                return null;
            } else if (Registers.ContainsKey(name) && strict) {
                Log.Error("Object already defined and in strict mode so can't redefine objects");
                return null;
            }

            Registers.Add(name, objectNode);
            tok.IgnoreWhitespace();
            if (tok.currentChar == '{') {
                inBlock = true;
                currentVariable = objectNode;
                tok.Advance();
            }
            return objectNode;
        }

        private BaseNode ParseObjectOrAssignment() {
            // Grab name, then check ':'
            string name = ParseIdentifier("", false).ToString();

            if (tok.currentChar == '.') {
                if (CheckBlockSyntax(name)) return null;
                if (!Registers.ContainsKey(name)) {
                    LogError($"Object doesn't exist {name}");
                    return null;
                }
                return ParseAssignment(Registers[name]);
            } else {
                return ParseObjectDefinition(name);
            }
        }

        private List<BaseNode> ParseArgList() {
            // Parse each arg, sometimes we may think there could be another node and may be wrong
            // in that case we'll handle it gracefully often emulating a parse assignment as well, thus the return type.
            // The first one will be a function node though.
            int count = 0;
            FunctionNode node = new FunctionNode();
            List<BaseNode> values = new List<BaseNode>(3);
            BaseNode next = ParseValue(ref count);
            if (next == null || next is ReserveNode) {
                LogError("Can't have any empty arg list");
                return null;
            }
            values.Add(next);

            tok.IgnoreWhitespace();
            if (tok.currentChar == ',') {
                tok.AdvanceAndIgnoreWS();
            } else {
                return values;
            }

            while (next != null) {
                next = ParseValue(ref count);
                if (next == null || next is ReserveNode) return null;
                values.Add(next);
                tok.IgnoreWhitespace();
                if (tok.currentChar != ',') break;
                tok.AdvanceAndIgnoreWS(); // skip ','
            }

            if (count > maxSpaces) maxSpaces = count;

            return values;
        }

        private StringBuilder ParseIdentifier(string prefix, bool allowDot) {
            if (!char.IsLetter(tok.currentChar) && tok.currentChar != '_') {
                LogError("Not a valid identifier");
                return null;
            }

            StringBuilder str = new StringBuilder(prefix);
            str.Append(tok.currentChar);
            while (tok.Advance()) {
                // @Query?  What symbols do we actually need to check for
                if (tok.currentChar == '=' || tok.currentChar == '(' || tok.currentChar == ':' || char.IsWhiteSpace(tok.currentChar) || (!allowDot && tok.currentChar == '.') || tok.currentChar == ',') {
                    break;
                } else if (!char.IsLetter(tok.currentChar) && tok.currentChar != '_' && (allowDot && tok.currentChar != '.')) {
                    // Invalid character
                    LogError("Invalid character for identifier");
                    return null;
                }
                str.Append(tok.currentChar);
            }
            return str;
        }

        private bool ParseComments() {
            if (char.IsWhiteSpace(tok.currentChar) == false && tok.currentChar != '/')
                return true;

            int blockCommentNesting = 0;
            do {
                if (tok.currentChar == '*' && (char)tok.reader.Peek() == '/') {
                    if (blockCommentNesting == 0) {
                        LogError("Didn't start comment block");
                        return false;
                    }

                    blockCommentNesting--;
                    tok.Advance(); // the while loop will consume the '/'
                } else if (tok.currentChar == '/') {
                    char next = (char)tok.reader.Peek();
                    if (next == '*') {
                        blockCommentNesting++;
                        tok.Advance(); // consume '*'
                    } else if (next == '/' && blockCommentNesting == 0) {
                        tok.AdvanceLine();
                    }
                } else if (char.IsWhiteSpace(tok.currentChar) == false && blockCommentNesting == 0) {
                    // We don't need to check the last condition since we know that blockCommentNesting <= 0
                    // If it is < 0 then we check that on that spot rather than towards end.
                    return true;
                }
            } while (tok.Advance());

            if (blockCommentNesting != 0) {
                LogError($"Didn't finish block comment");
                return false;
            }

            return true;
        }
    }
}


///// <summary>
///// Get an interpreter from binary data.
///// </summary>
///// <param name="reader"> The reader. </param>
///// <param name="lengthMethod"></param>
///// <returns></returns>
//public static Interpreter GetInterpreterFromBinary(BinaryReader reader, bool lengthMethod)
//{
//    if (Instructions.Count > 0) Instructions.Clear();
//    tok.currentChar = char.MinValue;
//    startingColumn = startingLine = currentLine = currentColumn = 0;
//    byte opCode;
//    object obj;

//    while (.PeekChar() != -1)
//    {
//        // Read Opcode
//        opCode = reader.ReadByte();
//        // Opcode validation??

//        if (lengthMethod)
//        {
//            if (ParseBinaryValueUsingLength(opCode, out obj) == false) return null;
//        }
//        else if (ParseBinaryValueUsingNative(, opCode, out obj) == false) return null;

//        Instructions.Add(new Instruction(opCode, obj));
//    }

//    return new Interpreter(Instructions);
//}

//public static Interpreter GetInterpreterFromIR(TextReader reader)
//{
//    if (Instructions.Count > 0) Instructions.Clear();
//    startingColumn = startingLine = this.currentLine = currentColumn = 0;
//    string currentLine = reader.ReadLine();
//    int index = 0;

//    while (currentLine != null)
//    {
//        while (char.IsWhiteSpace(currentLine[index]))
//            ++index;

//        if (currentLine[index] == ';')
//        {
//            currentLine = reader.ReadLine();
//            continue;
//        }

//        // OPCODE
//        char firstDigit = currentLine[index++];
//        int value;
//        tok.currentChar = currentLine[index];

//        if (char.IsDigit(firstDigit) == false)
//        {
//            LogError("Invalid Opcode");
//            return null;
//        }

//        if (char.IsDigit(tok.currentChar))
//        {
//            // Two Digits
//            value = (firstDigit == '1' ? 10 : 0) + tok.currentChar - '0'; // Since the maximum value is 18 so far, we can just do this, and save a multiplication
//            ++index;
//        }
//        else
//        {
//            // One Digit
//            value = firstDigit - '0';
//        }

//        if (value >= (int)Opcodes.COUNT_OF_INSTRUCTIONS)
//        {
//            LogError("Invalid Opcode");
//            return null;
//        }

//        Opcodes opcode = (Opcodes)value;

//        while (char.IsWhiteSpace(currentLine[index]))
//        {
//            if (++index >= currentLine.Length)
//            {
//                LogError("Missing parameter");
//                return null;
//            }
//        }

//        int initialIndex = index;
//        bool quoted = false;

//        do
//        {
//            tok.currentChar = currentLine[index];
//            if (((char.IsWhiteSpace(tok.currentChar) || tok.currentChar == ',') && quoted == false) || index >= currentLine.Length) break;
//            if (tok.currentChar == '"') quoted = !quoted;
//            index++;
//        }
//        while (true);

//        if (index >= currentLine.Length || !ParseValueForOpCode(opcode, currentLine.Substring(initialIndex, index - initialIndex), out object parameter))
//        {
//            LogError("Invalid Parameter");
//            return null;
//        }

//        while (currentLine[index] != '\n' && char.IsWhiteSpace(currentLine[index]))
//        {
//            if (++index >= currentLine.Length)
//            {
//                break;
//            }
//        }

//        Instructions.Add(new Instruction(opcode, parameter));

//        if (index >= currentLine.Length)
//        {
//            break;
//        }
//        else if (currentLine[index] == ',')
//        {
//            ++index;
//        }
//        else if (currentLine[index] == ';' || currentLine[index] == '\n')
//        {
//            currentLine = reader.ReadLine();
//        }
//        else
//        {
//            LogError("Invalid Character: " + currentLine[index]);
//            return null;
//        }
//    }

//    return new Interpreter(Instructions);
//}

/// <summary>
/// Create a new interpreter.
/// </summary>
/// <param name="reader"> The reader to read from. </param>
/// <returns> An interpreter instance. </returns>
/// <remarks> Remember to dispose the reader if calling this directly. </remarks>
//public static Interpreter GetInterpreter(TextReader reader)
//{
//    if (Instructions.Count > 0) Instructions.Clear();
//    tok.currentChar = char.MinValue;
//    startingColumn = startingLine = currentLine = currentColumn = 0;

//    if (Registers.Count > 0) Registers.Clear();
//    currentVariable = null;
//    nextRegister = 0;
//    maxSpaces = 0;

//    Instructions.Add(new Instruction()); // To be set at the end - ReserveSpace
//    Instructions.Add(new Instruction()); // To be set at the end - ReserveRegisters

//    AdvanceOnce(); // Kickstart

//    while (true)
//    {
//        int oldLine = currentLine;

//        // Remove whitespace/comments before first line
//        if (!ParseComments()) return null;

//        // If we at end of line then just return the interpreter instance
//        if (.Peek() < 0)
//            break;

//        if (tok.currentChar == '@')
//        {
//            if (ParseCreationStatement() == false)
//                return null;
//        }
//        else if (tok.currentChar == ';' || oldLine != currentLine || goToStatement)
//        {
//            goToStatement = false;
//            if (ParseSetStatement() == false)
//                return null;
//        }
//        else
//        {
//            // Something went wrong in remove white space or similar so just return null
//            // It could also be just a syntax error that they have initiated
//            LogError("Invalid character " + tok.currentChar);
//            return null;
//        }
//    }

//    Instructions[0] = new Instruction(Opcodes.MAKE_SPACE, maxSpaces);
//    Instructions[1] = new Instruction(Opcodes.MAKE_REG, nextRegister);

//    return new Interpreter(Instructions);
//}

/// <summary>
/// This method is more compatible with other methods,
/// however is considerably slower than the more native solution.
/// </summary>
/// <remarks>
/// Considerably slower if length is odd and it is a number
/// as it has to get the bytes and pad them.
/// </remarks>
/// <param name="reader"></param>
/// <param name="opCode"></param>
/// <param name="obj"></param>
/// <returns></returns>
//private static bool ParseBinaryValueUsingLength(BinaryReader reader, byte opCode, out object obj)
//{
//    // Represents the length in bytes
//    // Unless the opcode wants a string in which case it represents how many characters there are
//    byte length = reader.ReadByte();
//    if (length == 0)
//        Log.Error("Length == 0");

//    // Read Parameter Data
//    switch ((Opcodes)opCode)
//    {
//    case Opcodes.NOP:
//    case Opcodes.COMMENT:
//        obj = reader.ReadChars(length).ToString();
//        break;
//    case Opcodes.CALL:
//        obj = "get " + reader.ReadChars(length).ToString();
//        break;
//    case Opcodes.NEW:
//        obj = "new " + reader.ReadChars(length).ToString();
//        break;
//    case Opcodes.SET:
//        obj = "set " + reader.ReadChars(length).ToString();
//        break;
//    case Opcodes.PUSH:
//        return ParseValueForOpCode((Opcodes)opCode, reader.ReadChars(length).ToString(), out obj);
//    case Opcodes.MAKE_SPACE:
//    case Opcodes.MAKE_REG:
//    case Opcodes.COPY:
//    case Opcodes.REG_OBJ:
//    case Opcodes.UNREG_OBJ:
//    case Opcodes.PUSH_OBJ:
//    case Opcodes.POP:
//        if (length > 4)
//        {
//            Log.Error("Integer is to long");
//            obj = null;
//            return false;
//        }

//        if (length == 1)
//            obj = (int)reader.ReadByte();
//        else if (length == 2)
//            obj = (int)reader.ReadInt16();
//        else if (length == 4)
//            obj = reader.ReadInt32();
//        else
//        {
//            byte[] bytes = new byte[4] { 0, 0, 0, 0 };
//            reader.Read(bytes, 0, 4 - length);

//            if (!BitConverter.IsLittleEndian)
//                Array.Reverse(bytes);

//            obj = BitConverter.ToInt32(bytes, 0);
//        }
//        break;
//    case Opcodes.PUSH_INT:
//        if (length > 8)
//        {
//            Log.Error("Integer is to long");
//            obj = null;
//            return false;
//        }

//        if (length == 1)
//            obj = (long)reader.ReadByte();
//        else if (length == 2)
//            obj = (long)reader.ReadInt16();
//        else if (length == 4)
//            obj = (long)reader.ReadInt32();
//        else if (length == 8)
//            obj = reader.ReadInt64();
//        else
//        {
//            byte[] bytes = new byte[8] { 0, 0, 0, 0, 0, 0, 0, 0 };
//            reader.Read(bytes, 0, 8 - length);

//            if (!BitConverter.IsLittleEndian)
//                Array.Reverse(bytes);

//            obj = BitConverter.ToInt64(bytes, 0);
//        }
//        break;
//    case Opcodes.PUSH_NUM:
//        if (length > 8)
//        {
//            Log.Error("Floating Point is to long");
//            obj = null;
//            return false;
//        }

//        if (length == 4)
//            obj = (double)reader.ReadSingle();
//        if (length == 8)
//            obj = reader.ReadDouble();
//        else
//        {
//            byte[] bytes = new byte[8] { 0, 0, 0, 0, 0, 0, 0, 0 };
//            reader.Read(bytes, 0, 8 - length);

//            if (!BitConverter.IsLittleEndian)
//                Array.Reverse(bytes);

//            obj = BitConverter.ToDouble(bytes, 0);
//        }
//        break;
//    case Opcodes.PUSH_DEC:
//        if (length > 16)
//        {
//            Log.Error("Decimal is to long");
//            obj = null;
//            return false;
//        }

//        if (length == 16)
//            obj = reader.ReadDecimal();
//        else
//        {
//            byte[] bytes = new byte[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
//            reader.Read(bytes, 0, 16 - length);

//            if (!BitConverter.IsLittleEndian)
//                Array.Reverse(bytes);

//            obj = new decimal(new int[4] { BitConverter.ToInt32(bytes, 0), BitConverter.ToInt32(bytes, 4), BitConverter.ToInt32(bytes, 8), BitConverter.ToInt32(bytes, 12) });
//        }
//        break;
//    case Opcodes.PUSH_STR:
//        obj = reader.ReadChars(length).ToString();
//        break;
//    case Opcodes.PUSH_BOOL:
//        if (length > 1)
//        {
//            Log.Error("Boolean is to long");
//            obj = null;
//            return false;
//        }

//        obj = reader.ReadBoolean();
//        break;
//    default:
//        Log.Error("Forgot to include a case.  This is an error on DOML's side.  Please report.");
//        obj = null;
//        return false;
//    }

//    return true;
//}

//private static bool ParseBinaryValueUsingNative(BinaryReader reader, byte opCode, out object obj)
//{
//    // Read Parameter Data
//    switch ((Opcodes)opCode)
//    {
//    case Opcodes.NOP:
//    case Opcodes.COMMENT:
//        obj = reader.ReadString();
//        break;
//    case Opcodes.CALL:
//        obj = "get " + reader.ReadString();
//        break;
//    case Opcodes.NEW:
//        obj = "new " + reader.ReadString();
//        break;
//    case Opcodes.SET:
//        obj = "set " + reader.ReadString();
//        break;
//    case Opcodes.PUSH:
//        return ParseValueForOpCode((Opcodes)opCode, reader.ReadString(), out obj);
//    case Opcodes.MAKE_SPACE:
//    case Opcodes.MAKE_REG:
//    case Opcodes.COPY:
//    case Opcodes.REG_OBJ:
//    case Opcodes.UNREG_OBJ:
//    case Opcodes.PUSH_OBJ:
//    case Opcodes.POP:
//        obj = reader.ReadInt32();
//        break;
//    case Opcodes.PUSH_INT:
//        obj = reader.ReadInt64();
//        break;
//    case Opcodes.PUSH_NUM:
//        obj = reader.ReadDouble();
//        break;
//    case Opcodes.PUSH_DEC:
//        obj = reader.ReadDecimal();
//        break;
//    case Opcodes.PUSH_STR:
//        obj = reader.ReadString();
//        break;
//    case Opcodes.PUSH_BOOL:
//        obj = reader.ReadBoolean();
//        break;
//    default:
//        Log.Error("Forgot to include a case.  This is an error on DOML's side.  Please report.");
//        obj = null;
//        return false;
//    }

//    return true;
//}