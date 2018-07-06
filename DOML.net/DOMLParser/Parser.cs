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
using DOML.IR;
using DOML.Logger;
using DOML.AST;

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

        private static Dictionary<string, Func<TextReader, Parser, MacroNode>> macros = new Dictionary<string, Func<TextReader, Parser, MacroNode>>() {
            ["ir"] = new IRMacroNode().ParseNode,
            ["deinit"] = new DeinitMacroNode().ParseNode,
            ["version"] = HandleVersion,
            ["strict"] = HandleStrict,
            ["nokeywords"] = HandleNoKeywords,
        };

        public const string CompilerVersion = "0.3";

        private Settings settings;

        /// <summary>
        /// Starting line used for logging purposes.
        /// </summary>
        private int startingLine;

        /// <summary>
        /// Starting column used for logging purposes.
        /// </summary>
        private int startingColumn;

        /// <summary>
        /// Current line used for logging purposes.
        /// </summary>
        private int currentLine;

        /// <summary>
        /// Current column used for logging purposes.
        /// </summary>
        private int currentColumn;

        /// <summary>
        /// Current character that has just been read.
        /// </summary>
        private char currentCharacter;

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
        private void LogError(string error) => Log.Error(error, new Log.Information(startingLine, currentLine, startingColumn, currentColumn));

        /// <summary>
        /// Log an warning using current line information.
        /// </summary>
        /// <param name="warning"> What to log. </param>
        private void LogWarning(string warning) => Log.Warning(warning, new Log.Information(startingLine, currentLine, startingColumn, currentColumn));

        /// <summary>
        /// Log an info using current line information.
        /// </summary>
        /// <param name="info"> What to log. </param>
        private void LogInfo(string info) => Log.Info(info, new Log.Information(startingLine, currentLine, startingColumn, currentColumn));

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
        //                return GetInterpreter(reader);
        //        case ReadMode.IR:
        //            using (StreamReader reader = new StreamReader(new FileStream(filePath, FileMode.Open)))
        //                return GetInterpreterFromIR(reader);
        //        case ReadMode.BINARY_Length:
        //            using (BinaryReader reader = new BinaryReader(new FileStream(filePath, FileMode.Open)))
        //                return GetInterpreterFromBinary(reader, true);
        //        case ReadMode.BINARY_Native:
        //            using (BinaryReader reader = new BinaryReader(new FileStream(filePath, FileMode.Open)))
        //                return GetInterpreterFromBinary(reader, false);
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
        //                return GetInterpreter(reader);
        //        case ReadMode.IR:
        //            using (StringReader reader = new StringReader(text)) return GetInterpreterFromIR(reader);
        //        case ReadMode.BINARY_Length:
        //            using (BinaryWriter writer = BinaryWriter.Null)
        //            {
        //                foreach (char chr in text)
        //                    writer.Write(Convert.ToByte(chr));

        //                using (BinaryReader reader = new BinaryReader(writer.BaseStream))
        //                    return GetInterpreterFromBinary(reader, true);
        //            }
        //        case ReadMode.BINARY_Native:
        //            using (BinaryWriter writer = BinaryWriter.Null)
        //            {
        //                foreach (char chr in text)
        //                    writer.Write(Convert.ToByte(chr));

        //                using (BinaryReader reader = new BinaryReader(writer.BaseStream))
        //                    return GetInterpreterFromBinary(reader, false);
        //            }
        //        default:
        //            throw new NotImplementedException("Case Not Implemented; This is a bug.");
        //        }
        //    else
        //        throw new ArgumentNullException("Text is null");
        //}

        private string AdvanceLine(TextReader reader) {
            currentLine++;
            currentColumn = 0;
            return reader.ReadLine();
        }

        private bool Advance(TextReader reader, int amount) {
            for (; amount > 1; amount--) reader.Read();

            int last = reader.Read();
            currentCharacter = (char)last;

            if (currentCharacter == '\n') {
                currentLine++;
                currentColumn = 0;
            } else
                currentColumn++;

            return last >= 0;
        }

        private bool Advance(TextReader reader) {
            return Advance(reader, 1);
        }

        // TopLevelNode has an error occurred flag.
        public TopLevelNode ParseAST(TextReader reader) {
            List<BaseNode> children = new List<BaseNode>();
            bool success = true;
            Advance(reader, 1);
            while (true) {
                int oldLine = currentLine;
                // Remove whitespace/comments before first line
                if (!ParseComments(reader)) return null;
                if (reader.Peek() < 0) break;
                if (currentCharacter == '#') {
                    MacroNode node = ParseMacro(reader);
                    if (node == null) {
                        Log.Error("Invalid macro");
                        success = false;
                        break;
                    }

                    // Avoids having a lot of NOPs
                    if (!(node is DummyNode)) children.Add(node);
                } else if (currentCharacter == '}') {
                    if (inBlock) {
                        inBlock = false;
                        Advance(reader);
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
                    FunctionNode node = ParseAssignment(reader, currentVariable);
                    if (node == null) {
                        success = false;
                        break;
                    }
                    children.Add(node);
                } else {
                    BaseNode node = ParseObjectOrAssignment(reader);
                    if (node == null) {
                        // Handling `obj.{ ... }`
                        if (currentCharacter != '.' || reader.Peek() != '{' || !inBlock) {
                            return null;
                        } else {
                            Advance(reader, 2);
                        }
                    } else if (!(node is DummyNode)) {
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

        private static DummyNode HandleVersion(TextReader reader, Parser parser) {
            StringBuilder version = new StringBuilder();
            parser.IgnoreWhitespace(reader);
            int digitCount = 0;
            while (char.IsDigit(parser.currentCharacter) || parser.currentCharacter == '.') {
                if (parser.currentCharacter == '.') digitCount++;
                version.Append(parser.currentCharacter);
                parser.Advance(reader);
            }

            if (!char.IsWhiteSpace(parser.currentCharacter) || digitCount > 2) {
                Log.Error("Invalid Version");
                return null;
            }

            parser.version = version.ToString();
            parser.IgnoreWhitespace(reader);
            return new DummyNode();
        }

        private bool? ParseBool(TextReader reader) {
            IgnoreWhitespace(reader);
            string val = ParseIdentifier(reader, "", false).ToString();
            IgnoreWhitespace(reader);
            if (val == "true") return true;
            if (val == "false") return false;
            return null;
        }

        private static MacroNode HandleNoKeywords(TextReader reader, Parser parser) {
            if (parser.ParseBool(reader) is bool val) {
                parser.noKeywords = val;
                return new DummyNode();
            } else {
                return null;
            }
        }

        private static MacroNode HandleStrict(TextReader reader, Parser parser) {
            if (parser.ParseBool(reader) is bool val) {
                parser.strict = val;
                return new DummyNode();
            } else {
                return null;
            }
        }

        private MacroNode ParseMacro(TextReader reader) {
            if (currentCharacter != '#') {
                Log.Error("Internal error");
                return null;
            }

            Advance(reader);
            IgnoreWhitespace(reader);
            // Parse identifier
            string identifier = ParseIdentifier(reader, "", false).ToString().ToLower();
            if (macros.ContainsKey(identifier)) {
                return macros[identifier](reader, this);
            }

            return null;
            // Should we suggest similar names??
        }

        private FunctionNode ParseAssignment(TextReader reader, ObjectNode obj, string name = null) {
            if (name == null) {
                // Passes obj.x = y, from the state of 'x' (post '.')
                name = ParseIdentifier(reader, "", true).ToString();
                // Skip ws
                IgnoreWhitespace(reader);
            }

            // Confirm equals
            if (currentCharacter != '=') {
                LogError("Missing '='");
                return null;
            }

            // Now just parse the 'arg list'
            Advance(reader);
            IgnoreWhitespace(reader);
            List<BaseNode> node = ParseArgList(reader);
            if (node == null) return null;
            return new FunctionNode() { args = node.Select(x => new ArgumentNode() { name = null, value = x }).ToArray(), name = name, obj = obj, type = FunctionType.SETTER };
        }

        private bool IsEndingChar(char c) {
            return (c == ';' || c == ']' || c == ',' || c == '}' || char.IsWhiteSpace(c) || c == ')');
        }

        private BaseNode ParseArray(TextReader reader, ref int count) {
            ArrayNode values = new ArrayNode();
            while (currentCharacter != ']' && Advance(reader)) {
                IgnoreWhitespace(reader);
                BaseNode node = ParseValue(reader, ref count); // note: not propagating possibleFuncArg
                if (node == null || node is ReserveNode) return null; // Error occurred
                values.values.Add(node);
                IgnoreWhitespace(reader);
                if (currentCharacter != ',' && currentCharacter != ']') {
                    LogError("Missing ',' or ']'");
                    return null;
                }
            }

            if (values.values.Count == 0) {
                // empty array
                LogError("Can't have empty array, use null instead");
                return null;
            }

            if (currentCharacter != ']') {
                LogError("Missing ']' character.");
                return null;
            }

            Advance(reader);
            return values;
        }

        // Parses floats and integers
        private BaseNode ParseNumber(TextReader reader, ref int count) {
            StringBuilder builder = new StringBuilder();
            bool flt = false;
            while (!IsEndingChar(currentCharacter)) {
                builder.Append(currentCharacter);
                if (currentCharacter == '.' || currentCharacter == 'e' || currentCharacter == 'E') flt = true;
                Advance(reader);
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

        private BaseNode ParseString(TextReader reader, ref int count) {
            StringBuilder builder = new StringBuilder();
            bool escaped = false;
            while (Advance(reader) && (currentCharacter != '"' || escaped)) {
                if (currentCharacter == '\\') {
                    escaped = true;
                } else {
                    // @TODO: other escape codes like whitespace
                    escaped = false;
                    builder.Append(currentCharacter);
                }
            }

            if (currentCharacter != '"' && !escaped) {
                LogError("Missing terminating '\"'");
                return null;
            }
            Advance(reader);
            return new ValueNode() { obj = builder.ToString() };
        }

        private BaseNode ParseDecimal(TextReader reader, ref int count) {
            Advance(reader); // skip '$'
            StringBuilder builder = new StringBuilder();
            while (IsEndingChar(currentCharacter)) {
                builder.Append(currentCharacter);
            }

            if (builder.Length == 0 || !decimal.TryParse(builder.ToString(), out decimal result)) {
                LogError("Invalid decimal: " + builder.ToString());
                return null;
            }

            return new ValueNode() { obj = result };
        }

        private BaseNode ParseMap(TextReader reader, ref int count) {
            MapNode map = new MapNode();
            while (currentCharacter != '}' && Advance(reader)) {
                IgnoreWhitespace(reader);
                BaseNode key = ParseValue(reader, ref count);
                if (key == null) return null;

                IgnoreWhitespace(reader);
                if (currentCharacter != ':') {
                    LogError("Expecting ':'");
                    return null;
                }
                Advance(reader);
                IgnoreWhitespace(reader);

                BaseNode value = ParseValue(reader, ref count);
                if (value == null) return null;
                IgnoreWhitespace(reader);
                if (currentCharacter != ',' && currentCharacter != '}') {
                    LogError("Expecting either ',' or '}'");
                    return null;
                }

                map.map.Add(key, value);
            }
            if (currentCharacter != '}') {
                LogError("No terminating '}'");
            }

            Advance(reader);
            IgnoreWhitespace(reader);
            return map;
        }

        // @TODO: refactor this more its too long
        private BaseNode ParseObjectReference(TextReader reader, ref int count, bool possibleFuncArg = false) {
            // @TODO: support #NoKeywords
            // Could be boolean, object or null
            // Parse till ',' ']' ')' or an invalid
            StringBuilder identifier = ParseIdentifier(reader, "", false);

            if (currentCharacter != '.') {
                IgnoreWhitespace(reader);
                if (currentCharacter == ':') {
                    if (possibleFuncArg) {
                        // @Query? Does count have to be reset
                        if (currentCharacter != ':') {
                            LogError("Internal error expected ':'");
                            return null;
                        }

                        Advance(reader);
                        IgnoreWhitespace(reader);
                        // Parse actual value
                        BaseNode val = ParseValue(reader, ref count);
                        if (val == null) return null;
                        return new ArgumentNode() { name = identifier.ToString(), value = val };
                    } else {
                        LogError("Syntax Error");
                        return null;
                    }
                } else if (currentCharacter == '=') {
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

            if (currentCharacter != '.') {
                return objValue;
            } else if (definition) {
                LogError($"'.' operator not valid on a definition");
                return null;
            }

            // Get other end
            identifier.Clear();
            Advance(reader); // skip '.'
            while (Advance(reader)) {
                if (char.IsLetter(currentCharacter)) {
                    identifier.Append(currentCharacter);
                } else {
                    break;
                }
            }
            IgnoreWhitespace(reader);

            if (currentCharacter == '=') return null;

            string funcName = identifier.ToString();

            List<ArgumentNode> list = new List<ArgumentNode>();
            if (currentCharacter == '(') {
                // Parse function list
                list = ParseFuncArgList(reader, ref count);
                if (list == null) {
                    return null;
                }
            }

            return new FunctionNode() { args = list.ToArray(), name = funcName, obj = Registers[value] };
        }

        private BaseNode ParseValue(TextReader reader, ref int count, bool possibleFuncArg = false) {
            count++;
            if (currentCharacter == '[') {
                return ParseArray(reader, ref count);
            } else if (currentCharacter == '{') {
                return ParseMap(reader, ref count);
            } else if (char.IsDigit(currentCharacter)) {
                return ParseNumber(reader, ref count);
            } else if (currentCharacter == '$') {
                return ParseDecimal(reader, ref count);
            } else if (currentCharacter == '"') {
                return ParseString(reader, ref count);
            } else {
                return ParseObjectReference(reader, ref count, possibleFuncArg);
            }
        }

        private List<ArgumentNode> ParseFuncArgList(TextReader reader, ref int count) {
            if (currentCharacter != '(') {
                LogError("Invalid Syntax");
                return null;
            }

            Advance(reader);
            List<ArgumentNode> arguments = new List<ArgumentNode>();

            if (currentCharacter == ')') {
                // Empty/void
                return arguments;
            }

            do {
                IgnoreWhitespace(reader);
                BaseNode value = ParseValue(reader, ref count, true);
                if (value == null) return null;
                if (value is ArgumentNode arg) arguments.Add(arg);
                else arguments.Add(new ArgumentNode() { name = null, value = value });
            } while (currentCharacter != ')' && currentCharacter == ',' && Advance(reader));

            if (currentCharacter != ')') {
                LogError("Syntax Error");
                return null;
            }
            Advance(reader);
            return arguments;
        }

        private BaseNode ParseLiteralDefinition(TextReader reader, string name) {
            // Definition
            Advance(reader);
            IgnoreWhitespace(reader);
            int count = 0;
            BaseNode value = ParseValue(reader, ref count);
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

        private ObjectNode ParseConstructor(TextReader reader, string objName, string typeName) {
            Advance(reader);
            if (currentCharacter != ':') {
                LogError("Syntax error");
                return null;
            }

            Advance(reader);
            string functionName;
            if (currentCharacter == '(') {
                // Empty
                Advance(reader);
                functionName = objName;
            } else {
                functionName = ParseIdentifier(reader, "", false).ToString();
            }

            int count = 0;
            List<ArgumentNode> args = ParseFuncArgList(reader, ref count);
            if (count > maxSpaces) maxSpaces = count;
            if (args == null) return null;
            return new ObjectNode() { name = objName, type = typeName, constructor = new FunctionNode() { name = functionName, type = FunctionType.CONSTRUCTOR, args = args.ToArray() } };
        }

        private BaseNode ParseAssignment(TextReader reader, string name) {
            // Pasre assignment
            if (reader.Peek() == '{') {
                if (!Registers.ContainsKey(name)) {
                    LogError($"Object doesn't exist {name}");
                } else {
                    currentVariable = Registers[name];
                    inBlock = true;
                    return null;
                }
            }

            // An assignment
            Advance(reader);
            if (!Registers.ContainsKey(name)) {
                LogError($"Object doesn't exist {name}");
                return null;
            }
            return ParseAssignment(reader, Registers[name]);
        }

        private BaseNode ParseObjectDefinition(TextReader reader, string name) {
            IgnoreWhitespace(reader);
            if (currentCharacter != ':') {
                LogError("Syntax error");
                return null;
            }

            Advance(reader);
            if (currentCharacter == '=') {
                return ParseLiteralDefinition(reader, name);
            }

            IgnoreWhitespace(reader);
            StringBuilder type = ParseIdentifier(reader, "", false);
            ObjectNode objectNode;
            string typeName = type.ToString();
            if (currentCharacter == ':') {
                objectNode = ParseConstructor(reader, name, typeName);
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
            IgnoreWhitespace(reader);
            if (currentCharacter == '{') {
                inBlock = true;
                currentVariable = objectNode;
                Advance(reader);
            }
            return objectNode;
        }

        private BaseNode ParseObjectOrAssignment(TextReader reader) {
            // Grab name, then check ':'
            StringBuilder name = ParseIdentifier(reader, "", false);

            if (currentCharacter == '.') {
                return ParseAssignment(reader, name.ToString());
            } else {
                return ParseObjectDefinition(reader, name.ToString());
            }
        }

        private List<BaseNode> ParseArgList(TextReader reader) {
            // Parse each arg, sometimes we may think there could be another node and may be wrong
            // in that case we'll handle it gracefully often emulating a parse assignment as well, thus the return type.
            // The first one will be a function node though.
            int count = 0;
            FunctionNode node = new FunctionNode();
            List<BaseNode> values = new List<BaseNode>(3);
            BaseNode next = ParseValue(reader, ref count);
            if (next == null || next is ReserveNode) {
                LogError("Can't have any empty arg list");
                return null;
            }
            values.Add(next);

            IgnoreWhitespace(reader);
            if (currentCharacter == ',') {
                Advance(reader);
            } else {
                return values;
            }
            IgnoreWhitespace(reader);

            while (next != null) {
                next = ParseValue(reader, ref count);
                if (next == null || next is ReserveNode) return null;
                values.Add(next);
                IgnoreWhitespace(reader);
                if (currentCharacter != ',') break;
                Advance(reader); // skip ','
                IgnoreWhitespace(reader);
            }

            if (count > maxSpaces) maxSpaces = count;

            return values;
        }

        private StringBuilder ParseIdentifier(TextReader reader, string prefix, bool allowDot) {
            startingLine = currentLine;
            startingColumn = currentColumn;

            if (!char.IsLetter(currentCharacter) && currentCharacter != '_') {
                LogError("Not a valid identifier");
                return null;
            }

            StringBuilder str = new StringBuilder(prefix);
            str.Append(currentCharacter);
            while (Advance(reader)) {
                // @Query?  What symbols do we actually need to check for
                if (currentCharacter == '=' || currentCharacter == '(' || currentCharacter == ':' || char.IsWhiteSpace(currentCharacter) || (!allowDot && currentCharacter == '.') || currentCharacter == ',') {
                    break;
                } else if (!char.IsLetter(currentCharacter) && currentCharacter != '_' && (allowDot && currentCharacter != '.')) {
                    // Invalid character
                    LogError("Invalid character for identifier");
                    return null;
                }
                str.Append(currentCharacter);
            }
            return str;
        }

        private bool ParseComments(TextReader reader) {
            if (char.IsWhiteSpace(currentCharacter) == false && currentCharacter != '/')
                return true;

            int blockCommentNesting = 0;
            do {
                if (currentCharacter == '*' && (char)reader.Peek() == '/') {
                    if (blockCommentNesting == 0) {
                        LogError("Didn't start comment block");
                        return false;
                    }

                    blockCommentNesting--;
                    Advance(reader); // consume '*' and begin on '/'
                } else if (currentCharacter == '/') {
                    char next = (char)reader.Peek();
                    if (next == '*') {
                        if (blockCommentNesting == 0) {
                            startingLine = currentLine;
                            startingColumn = currentColumn;
                        }

                        blockCommentNesting++;
                        Advance(reader); // consume '*'
                    } else if (next == '/' && blockCommentNesting == 0) {
                        AdvanceLine(reader);
                    }
                } else if (char.IsWhiteSpace(currentCharacter) == false && blockCommentNesting == 0) {
                    // We don't need to check the last condition since we know that blockCommentNesting <= 0
                    // If it is < 0 then we check that on that spot rather than towards end.
                    return true;
                }
            } while (Advance(reader));

            if (blockCommentNesting != 0) {
                LogError($"Didn't finish block comment");
                return false;
            }

            return true;
        }

        internal void IgnoreWhitespace(TextReader reader) {
            while (char.IsWhiteSpace(currentCharacter)) {
                Advance(reader);
            }
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
//    currentCharacter = char.MinValue;
//    startingColumn = startingLine = currentLine = currentColumn = 0;
//    byte opCode;
//    object obj;

//    while (reader.PeekChar() != -1)
//    {
//        // Read Opcode
//        opCode = reader.ReadByte();
//        // Opcode validation??

//        if (lengthMethod)
//        {
//            if (ParseBinaryValueUsingLength(reader, opCode, out obj) == false) return null;
//        }
//        else if (ParseBinaryValueUsingNative(reader, opCode, out obj) == false) return null;

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
//        currentCharacter = currentLine[index];

//        if (char.IsDigit(firstDigit) == false)
//        {
//            LogError("Invalid Opcode");
//            return null;
//        }

//        if (char.IsDigit(currentCharacter))
//        {
//            // Two Digits
//            value = (firstDigit == '1' ? 10 : 0) + currentCharacter - '0'; // Since the maximum value is 18 so far, we can just do this, and save a multiplication
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
//            currentCharacter = currentLine[index];
//            if (((char.IsWhiteSpace(currentCharacter) || currentCharacter == ',') && quoted == false) || index >= currentLine.Length) break;
//            if (currentCharacter == '"') quoted = !quoted;
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
//    currentCharacter = char.MinValue;
//    startingColumn = startingLine = currentLine = currentColumn = 0;

//    if (Registers.Count > 0) Registers.Clear();
//    currentVariable = null;
//    nextRegister = 0;
//    maxSpaces = 0;

//    Instructions.Add(new Instruction()); // To be set at the end - ReserveSpace
//    Instructions.Add(new Instruction()); // To be set at the end - ReserveRegisters

//    AdvanceOnce(reader); // Kickstart

//    while (true)
//    {
//        int oldLine = currentLine;

//        // Remove whitespace/comments before first line
//        if (!ParseComments(reader)) return null;

//        // If we at end of line then just return the interpreter instance
//        if (reader.Peek() < 0)
//            break;

//        if (currentCharacter == '@')
//        {
//            if (ParseCreationStatement(reader) == false)
//                return null;
//        }
//        else if (currentCharacter == ';' || oldLine != currentLine || goToStatement)
//        {
//            goToStatement = false;
//            if (ParseSetStatement(reader) == false)
//                return null;
//        }
//        else
//        {
//            // Something went wrong in remove white space or similar so just return null
//            // It could also be just a syntax error that they have initiated
//            LogError("Invalid character " + currentCharacter);
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