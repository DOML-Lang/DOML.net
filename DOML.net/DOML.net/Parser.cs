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
    public static class Parser
    {
        public static int StartingLine { get; private set; }
        public static int StartingColumn { get; private set; }
        public static int CurrentLine { get; private set; }
        public static int CurrentColumn { get; private set; }

        private static char currentCharacter;
        private static string currentVariable; // for ...
        private static int nextRegister;
        private static int maxSpaces;

        private static Dictionary<string, CreationObjectInfo> Registers { get; } = new Dictionary<string, CreationObjectInfo>();

        private static List<Instruction> Instructions { get; } = new List<Instruction>();

        /// <summary>
        /// Get an interpreter from a file path.
        /// </summary>
        /// <param name="filePath"> The path to the file to open. </param>
        /// <returns> An interpreter instance. </returns>
        public static Interpreter GetInterpreterFromPath(string filePath, bool IR = false)
        {
            if (File.Exists(filePath))
                using (StreamReader reader = new StreamReader(new FileStream(filePath, FileMode.Open)))
                    return GetInterpreter(reader, IR);
            else
                throw new FileNotFoundException("File Path Invalid");
        }

        /// <summary>
        /// Get an interpreter from text.
        /// </summary>
        /// <param name="text"> The text to interpret. </param>
        /// <returns> An interpreter instance. </returns>
        public static Interpreter GetInterpreterFromText(string text, bool IR = false)
        {
            if (text != null)
                using (StringReader reader = new StringReader(text))
                    return GetInterpreter(reader, IR);
            else
                throw new ArgumentNullException("Text is null");
        }

        private static bool AllowedSeperator(char value)
        {
            // Maybe bounding checks
            return value == '[' || value == ']'
                || value == '{' || value == '}'
                || value == '(' || value == ')'
                || value == '<' || value == '>'
                || value == '-';
        }

        private static string AdvanceLine(TextReader reader)
        {
            CurrentLine++;
            CurrentColumn = 0;
            return reader.ReadLine();
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

        private static StringBuilder ParseIdentifierStatement(TextReader reader, string prefix, char[] breakOn, bool allowDot, bool allowSeperators)
        {
            if (!char.IsLetter(currentCharacter) && currentCharacter != '_')
            {
                Log.Error("Not a valid identifier", true);
                return null;
            }

            StartingLine = CurrentLine;
            StartingColumn = CurrentColumn;
            StringBuilder variableName = new StringBuilder(prefix);
            variableName.Append(currentCharacter);

            // Get identifier before '='
            while (Advance(reader, 1))
            {
                if (allowSeperators && AllowedSeperator(currentCharacter))
                {
                    variableName.Append('.');
                    continue;
                }
                else if (breakOn.Contains(currentCharacter) || char.IsWhiteSpace(currentCharacter) || (currentCharacter == '.' && reader.Peek() == '.'))
                {
                    break;
                }
                else if (char.IsLetterOrDigit(currentCharacter) == false && currentCharacter != '_' && currentCharacter == '.' && allowDot == false)
                {
                    Log.Error($"Invalid character for identifier starting at Line: /{StartingLine}, Column: /{StartingColumn}", true);
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
            Advance(reader, 1); // eat '@'
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
                Log.Error($"Missing '=' starting at Line: /{CurrentLine}, Column: /{CurrentColumn}", true);
                return false;
            }

            Advance(reader, 1);
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
                    Log.Error("Can't end a line on two dots, did you mean to do three?", true);
                    return false;
                }

                currentVariable = variableName;
            }

            Registers.Add(variableName, new CreationObjectInfo(nextRegister++, creationName.ToString(4, creationName.Length - 4)));
            Instructions.Add(new Instruction(Opcodes.NEW, creationName.ToString()));
            Instructions.Add(new Instruction(Opcodes.REG_OBJ, Registers[variableName].RegisterID));
            return true;
        }

        private static bool ParseSetStatement(TextReader reader)
        {
            Advance(reader, 1); // eat ';'
            IgnoreWhitespace(reader);
            CreationObjectInfo objectInfoToPush; // Get the object to push
            string variableName;
            StringBuilder parsed;

            // Push object
            if (currentCharacter == '.')
            {
                if (currentVariable == null || !Registers.ContainsKey(currentVariable))
                {
                    Log.Error("Are you missing a '...' in the previous line.  No previous 'variable' history found.", true);
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
                    Log.Error($"Missing '.' starting at Line: /{StartingLine}, Column: /{StartingColumn}", true);
                    return false;
                }

                if (Registers.ContainsKey(variableName) == false)
                {
                    Log.Error($"No previous 'variable' history found for /{variableName}.", true);
                    return false;
                }

                objectInfoToPush = Registers[variableName];
            }

            // The object ID will be pushed last next we get what we are setting
            Advance(reader, 1); // eat up the '.'

            parsed = ParseIdentifierStatement(reader, $"set {objectInfoToPush.ObjectType}::", new char[] { '/', '=' }, true, true);
            if (parsed == null) return false;

            // Remove any trailing '.'
            // Reasonably efficient not too worried and won't run if no trailings
            if (parsed[parsed.Length - 1] == '.')
            {
                for (int i = parsed.Length - 1, depth = -1; i >= 0; i--)
                {
                    if (parsed[i] != '.')
                    {
                        parsed.Remove(depth, parsed.Length - depth);
                        break;
                    }

                    depth = i;
                }
            }

            variableName = parsed.ToString();
            IgnoreWhitespace(reader);

            // Should start with a '='
            if (currentCharacter != '=')
            {
                Log.Error($"Missing '=' starting at Line: /{StartingLine}, Column: /{StartingColumn}", true);
                return false;
            }

            // Handle values
            int values = 0;

            do
            {
                Advance(reader, 1); // eat '=' or ','
                IgnoreWhitespace(reader);
                if (currentCharacter == '@' || currentCharacter == ';' || reader.Peek() <= 0)
                    // This allows you to have a comma at the very end despite being a little hacky
                    break;

                if (ParseValue(reader, ref values) == false)
                {
                    Log.Error("Parsing values went wrong");
                    return false;
                }

                IgnoreWhitespace(reader);
            } while (currentCharacter == ',');

            // If we don't have enough values - compile time error
            if (values < InstructionRegister.SizeOf[variableName])
            {
                Log.Error("Too few variables.");
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
        /// Create a new interpreter.
        /// </summary>
        /// <param name="reader"> The reader to read from. </param>
        /// <returns> An interpreter instance. </returns>
        /// <remarks> Remember to dispose the reader if calling this directly. </remarks>
        public static Interpreter GetInterpreter(TextReader reader, bool IR)
        {
            Instructions.Clear();
            currentCharacter = char.MinValue;
            StartingColumn = StartingLine = CurrentLine = CurrentColumn = 0;

            if (IR)
            {
                // This is relatively efficient, I'm not sold on Enum.TryParse yet though
                // This is around 100x more efficient then the last one
                while (currentCharacter == ';' || Advance(reader, 1))
                {
                    if (char.IsWhiteSpace(currentCharacter)) continue;

                    if (currentCharacter == ';')
                    {
                        AdvanceLine(reader);
                        currentCharacter = char.MinValue;
                        continue;
                    }

                    // Get Opcode
                    StringBuilder builder = new StringBuilder(currentCharacter);
                    while (Advance(reader, 1) && char.IsWhiteSpace(currentCharacter) == false)
                    {
                        builder.Append(char.ToUpper(currentCharacter));
                    }

                    if (char.IsWhiteSpace(currentCharacter) == false)
                    {
                        Log.Error("Invalid Line");
                        return null;
                    }

                    // This has to be tested to see how fast it is
                    if (!Enum.TryParse(builder.ToString(), out Opcodes opcode))
                    {
                        Log.Error("Invalid Opcode");
                        return null;
                    }

                    IgnoreWhitespace(reader);

                    if (reader.Peek() < 0)
                    {
                        Log.Error("No Parameter");
                        return null;
                    }

                    builder.Clear();

                    do
                    {
                        builder.Append(currentCharacter);
                    }
                    while (Advance(reader, 1) && char.IsWhiteSpace(currentCharacter) == false);

                    if (char.IsWhiteSpace(currentCharacter) == false)
                    {
                        Log.Error("Invalid Line");
                        return null;
                    }

                    if (!ParseValueForOpCode(opcode, builder.ToString(), out object parameter))
                    {
                        Log.Error("Invalid Parameter");
                        return null;
                    }

                    Instructions.Add(new Instruction(opcode, parameter));
                }

                return new Interpreter(Instructions);
            }
            else
            {
                Registers.Clear();
                currentVariable = null;
                nextRegister = 0;
                maxSpaces = 0;

                Instructions.Add(new Instruction()); // To be set at the end - ReserveSpace
                Instructions.Add(new Instruction()); // To be set at the end - ReserveRegisters

                while (currentCharacter == '@' || currentCharacter == ';' || Advance(reader, 1))
                {
                    // Remove whitespace/comments before first line
                    ParseComments(reader);

                    // If we at end of line then just return the interpreter instance
                    if (reader.Peek() < 0)
                        return new Interpreter(Instructions);

                    switch (currentCharacter)
                    {
                    case '@':
                        if (ParseCreationStatement(reader) == false)
                            return null;
                        break;
                    case ';':
                        if (ParseSetStatement(reader) == false)
                            return null;
                        break;
                    default:
                        // Something went wrong in remove white space or similar so just return null
                        Log.Error("Invalid character", true);
                        return null;
                    }
                }

                Instructions[0] = new Instruction(Opcodes.MAKE_SPACE, maxSpaces);
                Instructions[1] = new Instruction(Opcodes.MAKE_REG, nextRegister);
            }

            return new Interpreter(Instructions);
        }

        private static bool ParseValueForOpCode(Opcodes opcode, string valueToParse, out object obj)
        {
            switch (opcode)
            {
            case Opcodes.CLEAR:
            case Opcodes.CLEAR_REG:
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
            case Opcodes.PANIC:
            case Opcodes.PUSH:
                if (valueToParse[0] == '\'' && valueToParse[valueToParse.Length - 1] == '\'')
                {
                    bool result = char.TryParse(valueToParse.Trim('\''), out char temp);
                    obj = temp;
                    return result;
                }
                else if (valueToParse[0] == '"' && valueToParse[valueToParse.Length - 1] == '"')
                {
                    obj = valueToParse.Trim('"');
                    return true;
                }
                else if (valueToParse.Any(x => char.IsDigit(x) || x == '.'))
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
                    obj = null;
                    return false;
                }
            case Opcodes.MAKE_SPACE:
            case Opcodes.MAKE_REG:
            case Opcodes.COPY:
            case Opcodes.REG_OBJ:
            case Opcodes.UNREG_OBJ:
            case Opcodes.PUSH_OBJ:
            case Opcodes.POP:
            case Opcodes.COMP_MAX:
            case Opcodes.COMP_SIZE:
            case Opcodes.COMP_REG:
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
                    obj = valueToParse.Trim('"');
                    return true;
                }
            case Opcodes.PUSH_CHAR:
                {
                    bool result = char.TryParse(valueToParse.Trim('\''), out char temp);
                    obj = temp;
                    return result;
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
                Log.Error("Invalid Instruction");
                return false;
            }
        }

        private static bool ParseValue(TextReader reader, ref int values)
        {
            if (currentCharacter == '"')
                // We know its a string so don't even try to handle it as something else
                return ParseStringOrCharacter(reader, false, ref values);
            else if (currentCharacter == '\'')
                // We know its a character so again don't even try to handle it as something else
                return ParseStringOrCharacter(reader, true, ref values);
            else if (char.IsDigit(currentCharacter) || currentCharacter == '.' || currentCharacter == '-' || currentCharacter == '+' || currentCharacter == '$')
                // We know it has to be a number, nothing else
                return ParseNumber(reader, ref values);
            else
            {
                // Read till ',' and decide
                StringBuilder builder = new StringBuilder();
                do
                {
                    builder.Append(AllowedSeperator(currentCharacter) ? '.' : currentCharacter);
                }
                while (Advance(reader, 1) && currentCharacter != ',' && char.IsWhiteSpace(currentCharacter) == false);

                string result = builder.ToString();
                if (bool.TryParse(result, out bool res))
                {
                    Instructions.Add(new Instruction(Opcodes.PUSH_BOOL, res));
                    values++;
                    return true;
                }
                else if (result.Contains('.'))
                {
                    string[] splitObject = result.Split('.');
                    string callee;
                    if (Registers.ContainsKey(splitObject[0]))
                    {
                        // We are referring to one of our objects
                        CreationObjectInfo info = Registers[splitObject[0]];
                        Instructions.Add(new Instruction(Opcodes.PUSH_OBJ, info.RegisterID));
                        callee = $"get {info.ObjectType}::{result.Substring(splitObject[0].Length + 1)}";
                    }
                    else
                    {
                        // We are referring outside of one of our objects
                        callee = $"get {result}";
                    }

                    if (InstructionRegister.Actions.ContainsKey(callee) == false)
                    {
                        Log.Error("Can't find action");
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
                    Log.Error("Doesn't exist in any registers and no call exists for it");
                    return false;
                }
            }
        }

        private static bool ParseStringOrCharacter(TextReader reader, bool character, ref int values)
        {
            StringBuilder builder = new StringBuilder();
            bool escaped = false;
            char endOn = character ? '\'' : '"';
            // We can do it like this because we want to throw the starting string character out anyway
            while (Advance(reader, 1))
            {
                if (currentCharacter == endOn && escaped == false) break;

                else if (currentCharacter == '/' && escaped == false && reader.Peek() == endOn)
                {
                    escaped = true;
                }
                else
                {
                    escaped = false;
                    builder.Append(currentCharacter);
                }
            }

            if (currentCharacter != endOn)
            {
                Log.Error($"No ending {endOn}");
                return false;
            }

            Instructions.Add(new Instruction(character ? Opcodes.PUSH_CHAR : Opcodes.PUSH_STR, builder.ToString()));
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
                Advance(reader, 1);
            }

            if (currentCharacter == '$')
            {
                // Decimal
                baseN = -1;
                char next = (char)reader.Read();
                if (next == '+' || next == '-')
                {
                    builder.Append(next);
                    Advance(reader, 1);
                }
            }
            else if (currentCharacter == '.')
            {
                builder.Append('0');
                builder.Append(currentCharacter);
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

                Advance(reader, 1); // Skipping the '0' and the character after it
            }
            else
            {
                builder.Append(currentCharacter);
            }

            while (Advance(reader, 1))
            {
                if (char.IsWhiteSpace(currentCharacter)) break;

                if (currentCharacter == '_')
                {
                    if (underscore == false)
                    {
                        Log.Error("Can't have two '_' next to each other in a number");
                        return false;
                    }
                    else
                    {
                        underscore = true;
                        continue;
                    }
                }
                else if (currentCharacter == '.')
                {
                    if (baseN == 0)
                    {
                        // Two '.' exist therefore error
                        Log.Error("Can't have two '.' in a number");
                        return false;
                    }
                    baseN = 0;
                }
                else if (baseN == 0 && currentCharacter == 'e')
                {
                    builder.Append(currentCharacter);
                    Advance(reader, 1);
                    if (currentCharacter != '+' && currentCharacter != '-' && char.IsDigit(currentCharacter) == false)
                    {
                        Log.Error("Invalid Exponent");
                        return false;
                    }
                }
                else if (baseN == 2 && (currentCharacter != '0' || currentCharacter != '1')) break;
                else if (char.IsDigit(currentCharacter) == false && baseN != 16) break;
                else if (baseN == 7 && (currentCharacter > '7')) break;
                else if (baseN == 16 && (('a' <= currentCharacter) && (currentCharacter <= 'f') || ('A' <= currentCharacter) && (currentCharacter <= 'F')) == false) break;
                else if (char.IsDigit(currentCharacter) == false)
                {
                    Log.Error("Invalid number");
                    return false;
                }

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

        private static void ParseComments(TextReader reader)
        {
            int blockCommentNesting = 0;
            StringBuilder comment = new StringBuilder();

            while (Advance(reader, 1))
            {
                if (currentCharacter == '*' && (char)reader.Peek() == '/')
                {
                    if (blockCommentNesting == 0)
                    {
                        Log.Error("Didn't start comment block");
                        return;
                    }

                    blockCommentNesting--;
                    Advance(reader, 1); // consume '*' and begin on '/'
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
                        Advance(reader, 1); // consume '*'
                    }
                    else if (currentCharacter == '/' && reader.Peek() == '/' && blockCommentNesting == 0)
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
                    return;
                }
            }

            if (blockCommentNesting != 0)
            {
                Log.Error($"Didn't finish block comment starting at Line: /{StartingLine}, Column: /{StartingColumn}", true);
                return;
            }
        }

        private static void IgnoreWhitespace(TextReader reader)
        {
            while (char.IsWhiteSpace(currentCharacter))
            {
                Advance(reader, 1);
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
