#region License
// ====================================================
// Team DOML Copyright(C) 2017 Team DOML
// This program comes with ABSOLUTELY NO WARRANTY; This is free software, 
// and you are welcome to redistribute it under certain conditions; See 
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion

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
        /// The next register to use.
        /// </summary>
        private int nextRegister;

        /// <summary>
        /// The maximum amount of spaces this script uses.
        /// </summary>
        private int maxSpaces;

        /// <summary>
        /// Inside a block.
        /// </summary>
        private bool inBlock;

        /// <summary>
        /// All the register informtation.
        /// </summary>
        private static Dictionary<string, ObjectNode> Registers { get; } = new Dictionary<string, ObjectNode>();

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
            while (true) {
                int oldLine = currentLine;

                // Remove whitespace/comments before first line
                if (!ParseComments(reader)) return null;
                if (reader.Peek() < 0) break;
                if (currentCharacter == '#') {
                    MacroNode node = ParseMacro(reader);
                    if (node == null) {
                        success = false;
                        break;
                    }
                    children.Add(node);
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
                }
            }

            return new TopLevelNode() { children = children.ToArray(), errorOccurred = !success };
        }

        private MacroNode ParseMacro(TextReader reader) {
            if (currentCharacter != '#') return null;
            throw new NotImplementedException();
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
            return new FunctionNode() { args = node.Select(x => new ArgumentNode() { name = null, value = x }).ToArray(), name = name, obj = obj, type = FunctionType.SETTER };
        }

        private bool IsEndingChar(char c) {
            return (c == ';' || c == ']' || c == ',' || c == '}' || char.IsWhiteSpace(c) || c == ')');
        }

        // Returns true if value was properly passed else false if another thing was parsed
        private BaseNode ParseValue(TextReader reader) {
            if (currentCharacter == '[') {
                // Array
                Advance(reader);
                IgnoreWhitespace(reader);
                if (currentCharacter == ']') {
                    // empty array
                    LogError("Can't have empty array, use null instead");
                    return null;
                }

                // @TODO array
            } else if (char.IsDigit(currentCharacter)) {
                // number
                StringBuilder builder = new StringBuilder();
                while (IsEndingChar(currentCharacter)) {
                    builder.Append(currentCharacter);
                }

                // @TODO: confirm it supports hex, oct and bin
                if (builder.Length == 0 || !long.TryParse(builder.ToString(), out long result)) {
                    LogError("Invalid decimal");
                    return null;
                }

                return new ValueNode() { obj = result };
            } else if (currentCharacter == '$') {
                // decimal
                // Read till space or comma or newline or semicolon or parenthesis ending or bracket ending or brace ending
                Advance(reader); // skip '$'
                StringBuilder builder = new StringBuilder();
                while (IsEndingChar(currentCharacter)) {
                    builder.Append(currentCharacter);
                }

                if (builder.Length == 0 || !decimal.TryParse(builder.ToString(), out decimal result)) {
                    LogError("Invalid decimal");
                    return null;
                }

                return new ValueNode() { obj = result };
            } else if (currentCharacter == '"') {
                // string
                // go till terminating
                Advance(reader);
                StringBuilder builder = new StringBuilder();
                bool escaped = false;
                while ((currentCharacter != '"' || escaped) && Advance(reader)) {
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
                return new ValueNode() { obj = builder.ToString() };
            } else {
                // @TODO: support #NoKeywords
                // Could be boolean, object or null
                // Parse till ',' ']' ')' or an invalid
                StringBuilder identifier = new StringBuilder();
                while (Advance(reader)) {
                    if (char.IsLetter(currentCharacter)) {
                        identifier.Append(currentCharacter);
                    } else {
                        break;
                    }
                }

                if (currentCharacter != '.') {
                    IgnoreWhitespace(reader);
                    if (currentCharacter == ':' || currentCharacter == '=') return null;
                }

                string value = identifier.ToString();
                if (value == "true") return new ValueNode() { obj = true };
                if (value == "false") return new ValueNode() { obj = false };
                if (value == "null") return new ValueNode() { obj = null };

                // Object
                if (!Registers.ContainsKey(value)) {
                    LogError($"Invalid value {value}");
                    return null;
                }

                if (currentCharacter != '.') {
                    // Easy returning of object node
                    return Registers[value];
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

                if (currentCharacter == '(') {
                    // Parse function list
                    return ParseFuncArgList(reader, value, funcName);
                }

                return new FunctionNode() { args = new ArgumentNode[0], name = funcName, obj = Registers[value] };
            }
        }

        private FunctionNode ParseFuncArgList(TextReader reader, string objName = null, string funcName = null) {

        }

        private ObjectNode ParseObject(TextReader reader, string withName = null, string withType = null) {

        }

        private List<BaseNode> ParseArgList(TextReader reader) {
            // Parse each arg, sometimes we may think there could be another node and may be wrong
            // in that case we'll handle it gracefully often emulating a parse assignment as well, thus the return type.
            // The first one will be a function node though.
            FunctionNode node = new FunctionNode();
            List<BaseNode> values = new List<BaseNode>(3);
            BaseNode next = ParseValue(reader);
            if (next == null) {
                LogError("Can't have any empty arg list");
                return null;
            }

            while (next != null) {
                values.Add(next);
                next = ParseValue(reader);
                if (next == null) return null;

                IgnoreWhitespace(reader);
                if (currentCharacter != ',') break;
                Advance(reader); // skip ','
                IgnoreWhitespace(reader);
            }

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
                if (currentCharacter == '=' || currentCharacter == ':' || char.IsWhiteSpace(currentCharacter) || (!allowDot && currentCharacter == '.')) {
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
                        Advance(reader, 2);
                    }
                } else if (char.IsWhiteSpace(currentCharacter) == false) {
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

        private void IgnoreWhitespace(TextReader reader) {
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