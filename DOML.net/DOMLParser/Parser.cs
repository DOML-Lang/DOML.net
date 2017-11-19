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

namespace DOML
{
    /// <summary>
    /// The parser for DOML and IR.
    /// </summary>
    public static class Parser
    {
        public enum ReadMode
        {
            DOML,
            IR,
            BINARY_Length,
            BINARY_Native,
        }

        /// <summary>
        /// Starting line used for logging purposes.
        /// </summary>
        private static int StartingLine;

        /// <summary>
        /// Starting column used for logging purposes.
        /// </summary>
        private static int StartingColumn;

        /// <summary>
        /// Current line used for logging purposes.
        /// </summary>
        private static int CurrentLine;

        /// <summary>
        /// Current column used for logging purposes.
        /// </summary>
        private static int CurrentColumn;

        /// <summary>
        /// Current character that has just been read.
        /// </summary>
        private static char currentCharacter;

        /// <summary>
        /// Current variable for the `...` statements.
        /// </summary>
        private static string currentVariable;

        /// <summary>
        /// This is just to allow an extra ',' at the end.
        /// It basically tracks the oldLine == currentLine from previously.
        /// </summary>
        private static bool goToStatement = false;

        /// <summary>
        /// The next register to use.
        /// </summary>
        private static int nextRegister;

        /// <summary>
        /// The maximum amount of spaces this script uses.
        /// </summary>
        private static int maxSpaces;

        /// <summary>
        /// All the register informtation.
        /// </summary>
        private static Dictionary<string, CreationObjectInfo> Registers { get; } = new Dictionary<string, CreationObjectInfo>();

        /// <summary>
        /// Current list of instructions.
        /// </summary>
        private static List<Instruction> Instructions { get; } = new List<Instruction>();

        /// <summary>
        /// Log an error using current line information.
        /// </summary>
        /// <param name="error"> What to log. </param>
        public static void LogError(string error) => Log.Error(error, new Log.Information(StartingLine, CurrentLine, StartingColumn, CurrentColumn));

        /// <summary>
        /// Log an warning using current line information.
        /// </summary>
        /// <param name="warning"> What to log. </param>
        public static void LogWarning(string warning) => Log.Warning(warning, new Log.Information(StartingLine, CurrentLine, StartingColumn, CurrentColumn));

        /// <summary>
        /// Log an info using current line information.
        /// </summary>
        /// <param name="info"> What to log. </param>
        public static void LogInfo(string info) => Log.Info(info, new Log.Information(StartingLine, CurrentLine, StartingColumn, CurrentColumn));

        /// <summary>
        /// Get an interpreter from a file path.
        /// </summary>
        /// <param name="filePath"> The path to the file to open. </param>
        /// <param name="readMode"> What read mode to use. </param>
        /// <returns> An interpreter instance. </returns>
        public static Interpreter GetInterpreterFromPath(string filePath, ReadMode readMode = ReadMode.DOML)
        {
            if (File.Exists(filePath))
                switch (readMode)
                {
                case ReadMode.DOML:
                    using (StreamReader reader = new StreamReader(new FileStream(filePath, FileMode.Open)))
                        return GetInterpreter(reader);
                case ReadMode.IR:
                    using (StreamReader reader = new StreamReader(new FileStream(filePath, FileMode.Open)))
                        return GetInterpreterFromIR(reader);
                case ReadMode.BINARY_Length:
                    using (BinaryReader reader = new BinaryReader(new FileStream(filePath, FileMode.Open)))
                        return GetInterpreterFromBinary(reader, true);
                case ReadMode.BINARY_Native:
                    using (BinaryReader reader = new BinaryReader(new FileStream(filePath, FileMode.Open)))
                        return GetInterpreterFromBinary(reader, false);
                default:
                    throw new NotImplementedException("Case Not Implemented; This is a bug.");
                }
            else
                throw new FileNotFoundException("File Path Invalid");
        }

        /// <summary>
        /// Get an interpreter from text.
        /// </summary>
        /// <param name="text"> The text to interpret. </param>
        /// <param name="readMode"> What read mode to use. </param>
        /// <returns> An interpreter instance. </returns>
        /// <remarks> This is significantly slower for binary work. </remarks>
        public static Interpreter GetInterpreterFromText(string text, ReadMode readMode = ReadMode.DOML)
        {
            if (text != null)
                switch (readMode)
                {
                case ReadMode.DOML:
                    using (StringReader reader = new StringReader(text))
                        return GetInterpreter(reader);
                case ReadMode.IR:
                    using (StringReader reader = new StringReader(text)) return GetInterpreterFromIR(reader);
                case ReadMode.BINARY_Length:
                    using (BinaryWriter writer = BinaryWriter.Null)
                    {
                        foreach (char chr in text)
                            writer.Write(Convert.ToByte(chr));

                        using (BinaryReader reader = new BinaryReader(writer.BaseStream))
                            return GetInterpreterFromBinary(reader, true);
                    }
                case ReadMode.BINARY_Native:
                    using (BinaryWriter writer = BinaryWriter.Null)
                    {
                        foreach (char chr in text)
                            writer.Write(Convert.ToByte(chr));

                        using (BinaryReader reader = new BinaryReader(writer.BaseStream))
                            return GetInterpreterFromBinary(reader, false);
                    }
                default:
                    throw new NotImplementedException("Case Not Implemented; This is a bug.");
                }
            else
                throw new ArgumentNullException("Text is null");
        }

        private static string AdvanceLine(TextReader reader)
        {
            CurrentLine++;
            CurrentColumn = 0;
            return reader.ReadLine();
        }

        private static bool AdvanceOnce(TextReader reader)
        {
            int last = reader.Read();
            currentCharacter = (char)last;

            if (currentCharacter == '\n')
            {
                ++CurrentLine;
                CurrentColumn = 0;
            }
            else
                ++CurrentColumn;

            return last >= 0;
        }

        private static bool Advance(TextReader reader, int amount)
        {
            for (; amount > 1; amount--)
                reader.Read();

            int last = reader.Read();
            currentCharacter = (char)last;

            if (currentCharacter == '\n')
            {
                CurrentLine++;
                CurrentColumn = 0;
            }
            else
                CurrentColumn++;

            return last >= 0;
        }

        private static StringBuilder ParseIdentifierStatement(TextReader reader, string prefix, char[] breakOn, bool allowDot, bool allowSeperator)
        {
            if (!char.IsLetter(currentCharacter) && currentCharacter != '_')
            {
                LogError("Not a valid identifier");
                return null;
            }

            StartingLine = CurrentLine;
            StartingColumn = CurrentColumn;
            StringBuilder variableName = new StringBuilder(prefix);
            variableName.Append(currentCharacter);

            // Get identifier before '='
            while (AdvanceOnce(reader))
            {
                if (allowSeperator && currentCharacter == '-' && (char)reader.Peek() == '>')
                {
                    variableName.Append('.');
                    AdvanceOnce(reader);
                    continue;
                }
                else if (breakOn.Contains(currentCharacter) || char.IsWhiteSpace(currentCharacter) || (currentCharacter == '.' && reader.Peek() == '.'))
                {
                    break;
                }
                else if (char.IsLetterOrDigit(currentCharacter) == false && currentCharacter != '_' && currentCharacter == '.' && allowDot == false)
                {
                    LogError($"Invalid character for identifier");
                    return null;
                }
                else
                {
                    variableName.Append(currentCharacter);
                }
            }

            return variableName;
        }

        private static bool ParseCreationStatement(TextReader reader)
        {
            AdvanceOnce(reader); // eat '@'
            currentVariable = null; // just so the '...' doesn't carry over awkwardly
            IgnoreWhitespace(reader);

            // Parse the creation identifier
            string variableName = ParseIdentifierStatement(reader, string.Empty, new char[]{ '/', '=' }, false, false).ToString();
            if (variableName == null)
                // We had a problem so return false, the error will be logged from the parse identifier statement
                return false;

            // Add it to register and then remove whitespace
            IgnoreWhitespace(reader);
            if (currentCharacter != '=')
            {
                LogError($"Missing '='");
                return false;
            }

            AdvanceOnce(reader);
            IgnoreWhitespace(reader);

            StringBuilder creationName = ParseIdentifierStatement(reader, "new ", new char[]{ '\n', ';' }, true, false);
            if (creationName == null)
                // We had a problem so return false, the error will be logged from the parse identifier statement
                return false;

            IgnoreWhitespace(reader);
            if (currentCharacter == '.' && reader.Peek() == '.')
            {
                // This is for when the `...` is after the identifier with a space inbetween
                Advance(reader, 2);
                if (currentCharacter != '.')
                {
                    LogError("Can't end a line on two dots, did you mean to do three?");
                    return false;
                }

                currentVariable = variableName;
                AdvanceOnce(reader);
            }

            Registers.Add(variableName, new CreationObjectInfo(nextRegister++, creationName.ToString(4, creationName.Length - 4)));
            Instructions.Add(new Instruction(Opcodes.NEW, creationName.ToString()));
            Instructions.Add(new Instruction(Opcodes.REG_OBJ, Registers[variableName].RegisterID));
            return true;
        }

        private static bool ParseSetStatement(TextReader reader)
        {
            AdvanceOnce(reader); // eat ';'
            IgnoreWhitespace(reader);
            CreationObjectInfo objectInfoToPush; // Get the object to push
            string variableName;
            StringBuilder parsed;

            // Push object
            if (currentCharacter == '.')
            {
                if (currentVariable == null || !Registers.ContainsKey(currentVariable))
                {
                    LogError("Are you missing a '...' in the previous line.  No previous 'variable' history found.");
                    return false;
                }

                objectInfoToPush = Registers[currentVariable];
            }
            else
            {
                parsed = ParseIdentifierStatement(reader, string.Empty, new char[] { '.', '/' }, false, false);
                if (parsed == null) return false;

                variableName = parsed.ToString();

                if (currentCharacter != '.')
                {
                    LogError($"Missing '.' starting at Line: /{StartingLine}, Column: /{StartingColumn}");
                    return false;
                }

                if (Registers.ContainsKey(variableName) == false)
                {
                    LogError($"No previous 'variable' history found for /{variableName}.");
                    return false;
                }

                objectInfoToPush = Registers[variableName];
            }

            // The object ID will be pushed last next we get what we are setting
            AdvanceOnce(reader); // eat up the '.'

            parsed = ParseIdentifierStatement(reader, $"set {objectInfoToPush.ObjectType}::", new char[] { '/', '=' }, true, true);
            if (parsed == null) return false;

            variableName = parsed.ToString();
            IgnoreWhitespace(reader);

            // Should start with a '='
            if (currentCharacter != '=')
            {
                LogError($"Missing '=' starting at Line: /{StartingLine}, Column: /{StartingColumn}");
                return false;
            }

            // Handle values
            int values = 0;

            do
            {
                AdvanceOnce(reader); // eat '=' or ','
                int oldLine = CurrentLine;

                IgnoreWhitespace(reader);
                if (currentCharacter == '/')
                    ParseComments(reader);

                if (char.IsDigit(currentCharacter) == false && (currentCharacter == '@' || currentCharacter == ';' || reader.Peek() <= 0 || (oldLine != CurrentLine && (currentCharacter == '.' || values == InstructionRegister.SizeOf[variableName]))))
                {
                    // - The very first set before the first && is just some ways to short circuit early, to save speed
                    // - The first three are just simple checks these can't occur anywhere else so they denote the statement, with the third being the fact that the stream has no ended
                    // - The last one is comprised of a ';' less statement, and does some easier checks before bigger ones, first check is the separate line check which is required
                    //      - Then is the quick check which is if the character is '.' then we have to be of a new statement, and the value check is just a quicker way then doing a lookahead
                    //      - If you don't have enough values, then you'll get an error about how the parsing values went wrong, and a line number so while its more that you have too few variables
                    //        Its a fine enough error message, and the line number basically makes it a non issue.
                    goToStatement = oldLine != CurrentLine;
                    break;
                }

                if (ParseValue(reader, ref values) == false)
                {
                    LogError("Parsing values went wrong");
                    return false;
                }

                IgnoreWhitespace(reader);
            } while (currentCharacter == ',');

            // If we don't have enough values - compile time error
            if (values != InstructionRegister.SizeOf[variableName])
            {
                LogError("Too few or too many variables.");
                return false;
            }

            Instructions.Add(new Instruction(Opcodes.PUSH_OBJ, objectInfoToPush.RegisterID));
            values++;

            if (values > maxSpaces) maxSpaces = values;

            // Now push object and then the set command
            Instructions.Add(new Instruction(Opcodes.SET, variableName));
            return true;
        }

        /// <summary>
        /// Get an interpreter from binary data.
        /// </summary>
        /// <param name="reader"> The reader. </param>
        /// <param name="lengthMethod"></param>
        /// <returns></returns>
        public static Interpreter GetInterpreterFromBinary(BinaryReader reader, bool lengthMethod)
        {
            if (Instructions.Count > 0) Instructions.Clear();
            currentCharacter = char.MinValue;
            StartingColumn = StartingLine = CurrentLine = CurrentColumn = 0;
            byte opCode;
            object obj;

            while (reader.PeekChar() != -1)
            {
                // Read Opcode
                opCode = reader.ReadByte();
                if (opCode >= (byte)Opcodes.COUNT_OF_INSTRUCTIONS)
                {
                    Log.Error("Opcode too large");
                    return null;
                }

                if (lengthMethod)
                {
                    if (ParseBinaryValueUsingLength(reader, opCode, out obj) == false) return null;
                }
                else if (ParseBinaryValueUsingNative(reader, opCode, out obj) == false) return null;

                Instructions.Add(new Instruction(opCode, obj));
            }

            return new Interpreter(Instructions);
        }

        public static Interpreter GetInterpreterFromIR(TextReader reader)
        {
            if (Instructions.Count > 0) Instructions.Clear();
            StartingColumn = StartingLine = CurrentLine = CurrentColumn = 0;
            string currentLine = reader.ReadLine();
            int index = 0;

            while (currentLine != null)
            {
                while (char.IsWhiteSpace(currentLine[index]))
                    ++index;

                if (currentLine[index] == ';')
                {
                    currentLine = reader.ReadLine();
                    continue;
                }

                // OPCODE
                char firstDigit = currentLine[index++];
                int value;
                currentCharacter = currentLine[index];

                if (char.IsDigit(firstDigit) == false)
                {
                    LogError("Invalid Opcode");
                    return null;
                }

                if (char.IsDigit(currentCharacter))
                {
                    // Two Digits
                    value = (firstDigit == '1' ? 10 : 0) + currentCharacter - '0'; // Since the maximum value is 18 so far, we can just do this, and save a multiplication
                    ++index;
                }
                else
                {
                    // One Digit
                    value = firstDigit - '0';
                }

                if (value >= (int)Opcodes.COUNT_OF_INSTRUCTIONS)
                {
                    LogError("Invalid Opcode");
                    return null;
                }

                Opcodes opcode = (Opcodes)value;

                while (char.IsWhiteSpace(currentLine[index]))
                {
                    if (++index >= currentLine.Length)
                    {
                        LogError("Missing parameter");
                        return null;
                    }
                }

                int initialIndex = index;
                bool quoted = false;

                do
                {
                    currentCharacter = currentLine[index];
                    if (((char.IsWhiteSpace(currentCharacter) || currentCharacter == ',') && quoted == false) || index >= currentLine.Length) break;
                    if (currentCharacter == '"') quoted = !quoted;
                    index++;
                }
                while (true);

                if (index >= currentLine.Length || !ParseValueForOpCode(opcode, currentLine.Substring(initialIndex, index - initialIndex), out object parameter))
                {
                    LogError("Invalid Parameter");
                    return null;
                }

                while (currentLine[index] != '\n' && char.IsWhiteSpace(currentLine[index]))
                {
                    if (++index >= currentLine.Length)
                    {
                        break;
                    }
                }

                Instructions.Add(new Instruction(opcode, parameter));

                if (index >= currentLine.Length)
                {
                    break;
                }
                else if (currentLine[index] == ',')
                {
                    ++index;
                }
                else if (currentLine[index] == ';' || currentLine[index] == '\n')
                {
                    currentLine = reader.ReadLine();
                }
                else
                {
                    LogError("Invalid Character: " + currentLine[index]);
                    return null;
                }
            }

            return new Interpreter(Instructions);
        }

        /// <summary>
        /// Create a new interpreter.
        /// </summary>
        /// <param name="reader"> The reader to read from. </param>
        /// <returns> An interpreter instance. </returns>
        /// <remarks> Remember to dispose the reader if calling this directly. </remarks>
        public static Interpreter GetInterpreter(TextReader reader)
        {
            if (Instructions.Count > 0) Instructions.Clear();
            currentCharacter = char.MinValue;
            StartingColumn = StartingLine = CurrentLine = CurrentColumn = 0;

            if (Registers.Count > 0) Registers.Clear();
            currentVariable = null;
            nextRegister = 0;
            maxSpaces = 0;

            Instructions.Add(new Instruction()); // To be set at the end - ReserveSpace
            Instructions.Add(new Instruction()); // To be set at the end - ReserveRegisters

            AdvanceOnce(reader); // Kickstart

            while (true)
            {
                int oldLine = CurrentLine;

                // Remove whitespace/comments before first line
                if (!ParseComments(reader)) return null;

                // If we at end of line then just return the interpreter instance
                if (reader.Peek() < 0)
                    break;

                if (currentCharacter == '@')
                {
                    if (ParseCreationStatement(reader) == false)
                        return null;
                }
                else if (currentCharacter == ';' || oldLine != CurrentLine || goToStatement)
                {
                    goToStatement = false;
                    if (ParseSetStatement(reader) == false)
                        return null;
                }
                else
                {
                    // Something went wrong in remove white space or similar so just return null
                    // It could also be just a syntax error that they have initiated
                    LogError("Invalid character " + currentCharacter);
                    return null;
                }
            }

            Instructions[0] = new Instruction(Opcodes.MAKE_SPACE, maxSpaces);
            Instructions[1] = new Instruction(Opcodes.MAKE_REG, nextRegister);

            return new Interpreter(Instructions);
        }

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
        private static bool ParseBinaryValueUsingLength(BinaryReader reader, byte opCode, out object obj)
        {
            // Represents the length in bytes
            // Unless the opcode wants a string in which case it represents how many characters there are
            byte length = reader.ReadByte();
            if (length == 0)
                Log.Error("Length == 0");

            // Read Parameter Data
            switch ((Opcodes)opCode)
            {
            case Opcodes.NOP:
            case Opcodes.COMMENT:
                obj = reader.ReadChars(length).ToString();
                break;
            case Opcodes.CALL:
                obj = "get " + reader.ReadChars(length).ToString();
                break;
            case Opcodes.NEW:
                obj = "new " + reader.ReadChars(length).ToString();
                break;
            case Opcodes.SET:
                obj = "set " + reader.ReadChars(length).ToString();
                break;
            case Opcodes.PUSH:
                return ParseValueForOpCode((Opcodes)opCode, reader.ReadChars(length).ToString(), out obj);
            case Opcodes.MAKE_SPACE:
            case Opcodes.MAKE_REG:
            case Opcodes.COPY:
            case Opcodes.REG_OBJ:
            case Opcodes.UNREG_OBJ:
            case Opcodes.PUSH_OBJ:
            case Opcodes.POP:
                if (length > 4)
                {
                    Log.Error("Integer is to long");
                    obj = null;
                    return false;
                }

                if (length == 1)
                    obj = (int)reader.ReadByte();
                else if (length == 2)
                    obj = (int)reader.ReadInt16();
                else if (length == 4)
                    obj = reader.ReadInt32();
                else
                {
                    byte[] bytes = new byte[4] { 0, 0, 0, 0 };
                    reader.Read(bytes, 0, 4 - length);

                    if (!BitConverter.IsLittleEndian)
                        Array.Reverse(bytes);

                    obj = BitConverter.ToInt32(bytes, 0);
                }
                break;
            case Opcodes.PUSH_INT:
                if (length > 8)
                {
                    Log.Error("Integer is to long");
                    obj = null;
                    return false;
                }

                if (length == 1)
                    obj = (long)reader.ReadByte();
                else if (length == 2)
                    obj = (long)reader.ReadInt16();
                else if (length == 4)
                    obj = (long)reader.ReadInt32();
                else if (length == 8)
                    obj = reader.ReadInt64();
                else
                {
                    byte[] bytes = new byte[8] { 0, 0, 0, 0, 0, 0, 0, 0 };
                    reader.Read(bytes, 0, 8 - length);

                    if (!BitConverter.IsLittleEndian)
                        Array.Reverse(bytes);

                    obj = BitConverter.ToInt64(bytes, 0);
                }
                break;
            case Opcodes.PUSH_NUM:
                if (length > 8)
                {
                    Log.Error("Floating Point is to long");
                    obj = null;
                    return false;
                }

                if (length == 4)
                    obj = (double)reader.ReadSingle();
                if (length == 8)
                    obj = reader.ReadDouble();
                else
                {
                    byte[] bytes = new byte[8] { 0, 0, 0, 0, 0, 0, 0, 0 };
                    reader.Read(bytes, 0, 8 - length);

                    if (!BitConverter.IsLittleEndian)
                        Array.Reverse(bytes);

                    obj = BitConverter.ToDouble(bytes, 0);
                }
                break;
            case Opcodes.PUSH_DEC:
                if (length > 16)
                {
                    Log.Error("Decimal is to long");
                    obj = null;
                    return false;
                }

                if (length == 16)
                    obj = reader.ReadDecimal();
                else
                {
                    byte[] bytes = new byte[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
                    reader.Read(bytes, 0, 16 - length);

                    if (!BitConverter.IsLittleEndian)
                        Array.Reverse(bytes);

                    obj = new decimal(new int[4] { BitConverter.ToInt32(bytes, 0), BitConverter.ToInt32(bytes, 4), BitConverter.ToInt32(bytes, 8), BitConverter.ToInt32(bytes, 12) });
                }
                break;
            case Opcodes.PUSH_STR:
                obj = reader.ReadChars(length).ToString();
                break;
            case Opcodes.PUSH_BOOL:
                if (length > 1)
                {
                    Log.Error("Boolean is to long");
                    obj = null;
                    return false;
                }

                obj = reader.ReadBoolean();
                break;
            default:
                Log.Error("Forgot to include a case.  This is an error on DOML's side.  Please report.");
                obj = null;
                return false;
            }

            return true;
        }

        private static bool ParseBinaryValueUsingNative(BinaryReader reader, byte opCode, out object obj)
        {
            // Read Parameter Data
            switch ((Opcodes)opCode)
            {
            case Opcodes.NOP:
            case Opcodes.COMMENT:
                obj = reader.ReadString();
                break;
            case Opcodes.CALL:
                obj = "get " + reader.ReadString();
                break;
            case Opcodes.NEW:
                obj = "new " + reader.ReadString();
                break;
            case Opcodes.SET:
                obj = "set " + reader.ReadString();
                break;
            case Opcodes.PUSH:
                return ParseValueForOpCode((Opcodes)opCode, reader.ReadString(), out obj);
            case Opcodes.MAKE_SPACE:
            case Opcodes.MAKE_REG:
            case Opcodes.COPY:
            case Opcodes.REG_OBJ:
            case Opcodes.UNREG_OBJ:
            case Opcodes.PUSH_OBJ:
            case Opcodes.POP:
                obj = reader.ReadInt32();
                break;
            case Opcodes.PUSH_INT:
                obj = reader.ReadInt64();
                break;
            case Opcodes.PUSH_NUM:
                obj = reader.ReadDouble();
                break;
            case Opcodes.PUSH_DEC:
                obj = reader.ReadDecimal();
                break;
            case Opcodes.PUSH_STR:
                obj = reader.ReadString();
                break;
            case Opcodes.PUSH_BOOL:
                obj = reader.ReadBoolean();
                break;
            default:
                Log.Error("Forgot to include a case.  This is an error on DOML's side.  Please report.");
                obj = null;
                return false;
            }

            return true;
        }

        private static bool ParseValueForOpCode(Opcodes opcode, string valueToParse, out object obj)
        {
            switch (opcode)
            {
            case Opcodes.NOP:
            case Opcodes.COMMENT:
                obj = valueToParse;
                return true;
            case Opcodes.CALL:
                obj = "get " + valueToParse;
                return true;
            case Opcodes.NEW:
                obj = "new " + valueToParse;
                return true;
            case Opcodes.SET:
                obj = "set " + valueToParse;
                return true;
            case Opcodes.PUSH:
                if (valueToParse.Any(x => char.IsDigit(x) || x == '.'))
                {
                    if (valueToParse.Contains('.'))
                    {
                        bool result = double.TryParse(valueToParse, out double temp);
                        obj = temp;
                        return result;
                    }
                    else
                    {
                        bool result = long.TryParse(valueToParse, out long temp);
                        obj = temp;
                        return result;
                    }
                }
                else if (valueToParse == "false" || valueToParse == "true")
                {
                    obj = valueToParse == "true";
                    return true;
                }
                else
                {
                    obj = valueToParse;
                    return true;
                }
            case Opcodes.MAKE_SPACE:
            case Opcodes.MAKE_REG:
            case Opcodes.COPY:
            case Opcodes.REG_OBJ:
            case Opcodes.UNREG_OBJ:
            case Opcodes.PUSH_OBJ:
            case Opcodes.POP:
                {
                    bool result = int.TryParse(valueToParse, out int temp);
                    obj = temp;
                    return result;
                }
            case Opcodes.PUSH_INT:
                {
                    bool result = long.TryParse(valueToParse, out long temp);
                    obj = temp;
                    return result;
                }
            case Opcodes.PUSH_NUM:
                {
                    bool result = double.TryParse(valueToParse, out double temp);
                    obj = temp;
                    return result;
                }
            case Opcodes.PUSH_DEC:
                {
                    bool result = decimal.TryParse(valueToParse, out decimal temp);
                    obj = temp;
                    return result;
                }
            case Opcodes.PUSH_STR:
                {
                    obj = valueToParse;
                    return true;
                }
            case Opcodes.PUSH_BOOL:
                {
                    bool result = bool.TryParse(valueToParse, out bool temp);
                    obj = temp;
                    return result;
                }
            case Opcodes.COUNT_OF_INSTRUCTIONS:
            default:
                obj = null;
                LogError("Invalid Instruction");
                return false;
            }
        }

        private static bool ParseValue(TextReader reader, ref int values)
        {
            if (currentCharacter == '"')
                return ParseString(reader, ref values);
            else if (char.IsDigit(currentCharacter) || currentCharacter == '.' || currentCharacter == '-' || currentCharacter == '+' || currentCharacter == '$')
                return ParseNumber(reader, ref values);
            else
            {
                // Read till ',' and decide
                StringBuilder builder = new StringBuilder();
                do
                {
                    builder.Append(currentCharacter);
                }
                while (AdvanceOnce(reader) && currentCharacter != ',' && char.IsWhiteSpace(currentCharacter) == false);

                string result = builder.ToString();

                if (bool.TryParse(result, out bool res))
                {
                    Instructions.Add(new Instruction(Opcodes.PUSH_BOOL, res));
                    values++;
                    return true;
                }
                else if (result.Contains('.'))
                {
                    string[] splitObjects = result.Split('.');
                    string callee;
                    if (Registers.ContainsKey(splitObjects[0]))
                    {
                        // We are referring to one of our objects
                        CreationObjectInfo info = Registers[splitObjects[0]];
                        Instructions.Add(new Instruction(Opcodes.PUSH_OBJ, info.RegisterID));
                        callee = $"get {info.ObjectType}::{result.Substring(splitObjects[0].Length + 1)}";
                    }
                    else
                    {
                        // We are referring outside of one of our objects
                        callee = $"get {result}";
                    }

                    if (InstructionRegister.Actions.ContainsKey(callee) == false)
                    {
                        LogError("Can't find action");
                        return false;
                    }

                    Instructions.Add(new Instruction(Opcodes.CALL, callee));
                    values += InstructionRegister.SizeOf[callee];
                    return true;
                }
                else if (Registers.ContainsKey(result))
                {
                    // We are passing an object
                    Instructions.Add(new Instruction(Opcodes.PUSH_OBJ, Registers[result].RegisterID));
                    values++;
                    return true;
                }
                else
                {
                    LogError("Doesn't exist in any registers and no call exists for " + result);
                    return false;
                }
            }
        }

        private static bool ParseString(TextReader reader, ref int values)
        {
            StringBuilder builder = new StringBuilder();
            bool escaped = false;
            // We can do it like this because we want to throw the starting string character out anyway
            while (AdvanceOnce(reader))
            {
                if (currentCharacter == '"' && escaped == false) break;
                else if (currentCharacter == '/' && escaped == false && reader.Peek() == '"') escaped = true;
                else
                {
                    escaped = false;
                    builder.Append(currentCharacter);
                }
            }

            if (currentCharacter != '"')
            {
                LogError($"No ending {'"'}");
                return false;
            }

            AdvanceOnce(reader);

            Instructions.Add(new Instruction(Opcodes.PUSH_STR, builder.ToString()));
            values++;
            return true;
        }

        private static bool ParseNumber(TextReader reader, ref int values)
        {
            StringBuilder builder = new StringBuilder();
            int baseN = 10;
            bool underscore = false;

            if (currentCharacter == '-' || currentCharacter == '+')
            {
                builder.Append(currentCharacter);
                AdvanceOnce(reader);
            }

            if (currentCharacter == '$')
            {
                // Decimal
                baseN = -1;
                char next = (char)reader.Read();
                if (next == '+' || next == '-')
                {
                    builder.Append(next);
                    AdvanceOnce(reader);
                }

                if (next == '.')
                {
                    builder.Append("0.");
                }
            }
            else if (currentCharacter == '.')
            {
                builder.Append("0.");
                baseN = 0;
            }
            else if (currentCharacter == '0')
            {
                char next = char.ToLower((char)reader.Peek());
                if (next == 'x') baseN = 16;
                else if (next == 'b') baseN = 2;
                else if (next == 'o') baseN = 7;
                else
                {
                    // We do want to keep the '0'
                    builder.Append(currentCharacter);
                    builder.Append(next); // Append both so we can skip both
                    if (next == '.') // @FIXME: hacky
                        baseN = 0;
                }

                AdvanceOnce(reader); // Skipping the '0' and the character after it
            }
            else
            {
                builder.Append(currentCharacter);
            }

            while (AdvanceOnce(reader) && char.IsWhiteSpace(currentCharacter) == false)
            {
                if (currentCharacter == '.')
                {
                    if (baseN == 0)
                    {
                        // Two '.' exist therefore error
                        LogError("Can't have two '.' in a number");
                        return false;
                    }
                    baseN = 0;
                }
                else if (currentCharacter == '_')
                {
                    if (underscore)
                    {
                        LogError("Can't have two '_' next to each other in a number");
                        return false;
                    }
                    else
                    {
                        underscore = true;
                        continue;
                    }
                }
                else if (baseN == 0 && currentCharacter == 'e')
                {
                    builder.Append(currentCharacter);
                    AdvanceOnce(reader);
                    if (currentCharacter != '+' && currentCharacter != '-' && char.IsDigit(currentCharacter) == false)
                    {
                        LogError("Invalid Exponent");
                        return false;
                    }
                }
                else if (baseN == 2 && (currentCharacter != '0' || currentCharacter != '1')) break;
                else if (baseN == 7 && (currentCharacter > '7')) break;
                else if (baseN == 16 && (('a' <= currentCharacter) && (currentCharacter <= 'f') || ('A' <= currentCharacter) && (currentCharacter <= 'F') || char.IsDigit(currentCharacter)) == false) break;
                else if ((baseN == 10 || baseN == -1 || baseN == 0) && char.IsDigit(currentCharacter) == false) break;

                builder.Append(currentCharacter);
                underscore = false;
            }

            if (baseN == -1)
                // Decimal
                Instructions.Add(new Instruction(Opcodes.PUSH_DEC, Convert.ToDecimal(builder.ToString())));
            else if (baseN == 0)
                // Floating Point
                Instructions.Add(new Instruction(Opcodes.PUSH_NUM, Convert.ToDouble(builder.ToString())));
            else
                // Int
                Instructions.Add(new Instruction(Opcodes.PUSH_INT, Convert.ToInt64(builder.ToString(), baseN)));

            values++;
            return true;
        }

        private static bool ParseComments(TextReader reader)
        {
            if (char.IsWhiteSpace(currentCharacter) == false && currentCharacter != '/')
                return true;

            int blockCommentNesting = 0;
            StringBuilder comment = new StringBuilder();
            do
            {
                if (currentCharacter == '*' && (char)reader.Peek() == '/')
                {
                    if (blockCommentNesting == 0)
                    {
                        LogError("Didn't start comment block");
                        return false;
                    }

                    blockCommentNesting--;
                    AdvanceOnce(reader); // consume '*' and begin on '/'
                    Instructions.Add(new Instruction(Opcodes.COMMENT, comment.ToString()));
                }
                else if (currentCharacter == '/')
                {
                    char next = (char)reader.Peek();
                    if (next == '*')
                    {
                        if (blockCommentNesting == 0)
                        {
                            StartingLine = CurrentLine;
                            StartingColumn = CurrentColumn;
                        }

                        blockCommentNesting++;
                        AdvanceOnce(reader); // consume '*'
                    }
                    else if (next == '/' && blockCommentNesting == 0)
                    {
                        Advance(reader, 2);
                        Instructions.Add(new Instruction(Opcodes.COMMENT, AdvanceLine(reader)));
                    }
                    else if (blockCommentNesting > 0)
                    {
                        comment.Append(currentCharacter);
                    }
                }
                else if (blockCommentNesting > 0)
                {
                    comment.Append(currentCharacter);
                }
                else if (char.IsWhiteSpace(currentCharacter) == false)
                {
                    // We don't need to check the last condition since we know that blockCommentNesting <= 0
                    // If it is < 0 then we check that on that spot rather than towards end.
                    return true;
                }
            } while (AdvanceOnce(reader));

            if (blockCommentNesting != 0)
            {
                LogError($"Didn't finish block comment");
                return false;
            }

            return true;
        }

        private static void IgnoreWhitespace(TextReader reader)
        {
            while (char.IsWhiteSpace(currentCharacter))
            {
                AdvanceOnce(reader);
            }
        }

        private struct CreationObjectInfo
        {
            public int RegisterID;
            public string ObjectType;

            public CreationObjectInfo(int regID, string objType)
            {
                RegisterID = regID;
                ObjectType = objType;
            }
        }
    }
}
