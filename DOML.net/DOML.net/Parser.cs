using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DOML.ByteCode;
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

        private static char[] AllowedSeperators = new char[]{ '[', ']', '.', '{', '}', '(', ')', '<', '>', '-' };

        private static Dictionary<string, CreationObjectInfo> Registers = new Dictionary<string, CreationObjectInfo>();

        private static List<Instruction> Instructions = new List<Instruction>();

        private static string AdvanceLine(TextReader reader)
        {
            CurrentLine++;
            CurrentColumn = 0;
            return reader.ReadLine();
        }

        private static bool Advance(TextReader reader, int amount)
        {
            if (amount <= 0)
            {
                Log.Error("Invalid advancement amount", true);
                return false;
            }

            int last = -1;

            if (amount > 1)
            {
                for (; amount > 0; amount--)
                {
                    last = reader.Read();
                }
            }
            else
            {
                last = reader.Read();
            }

            currentCharacter = (char)last;

            return last >= 0;
        }

        private static StringBuilder ParseIdentifierStatement(TextReader reader, string prefix, char[] breakOn, char[] additionalAllowedCharacters)
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
                if (breakOn.Contains(currentCharacter) || char.IsWhiteSpace(currentCharacter))
                {
                    break;
                }
                else if (char.IsLetterOrDigit(currentCharacter) == false && currentCharacter != '_' && additionalAllowedCharacters.Contains(currentCharacter) == false)
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
            RemoveWhitespacesAndComments(reader);

            // Parse the creation identifier
            string variableName = ParseIdentifierStatement(reader, string.Empty, new char[]{ '/', '=' }, new char[0]).ToString();
            if (variableName == null)
            {
                // We had a problem so return false
                return false;
            }

            // Add it to register and then remove whitespace
            RemoveWhitespacesAndComments(reader);

            // Since its a creation we require a '=' to exist next
            if (currentCharacter != '=')
            {
                Log.Error($"Missing '=' starting at Line: /{StartingLine}, Column: /{StartingColumn}", true);
                return false;
            }

            Advance(reader, 1);
            RemoveWhitespacesAndComments(reader);

            StringBuilder creationName = ParseIdentifierStatement(reader, "new ", new char[]{ '\n', ';' }, new char[]{ '.' });
            if (creationName == null)
            {
                // We had a problem so return false
                return false;
            }

            // @TODO: Refactor later effectively same code twice
            if (creationName[creationName.Length - 1] == '.')
            {
                if (creationName[creationName.Length - 2] == '.' && creationName[creationName.Length - 3] == '.')
                {
                    // We have three dots at the end, so trim end and then set current
                    creationName.Remove(creationName.Length - 3, 3);
                    currentVariable = variableName;
                }
                else
                {
                    // Cant end on one or two dots
                    Log.Error("Not a valid identifier, can't end on one/two dots, only can end on three", true);
                    return false;
                }
            }
            else
            {
                // This is for when the `...` is after the identifier with a space inbetween
                RemoveWhitespacesAndComments(reader);
                if (currentCharacter == '.' && reader.Peek() == '.')
                {
                    Advance(reader, 2);
                    if (currentCharacter == '.')
                    {
                        currentVariable = variableName;
                    }
                    else
                    {
                        Log.Error("Can't end a line on two dots, did you mean to do three?", true);
                    }
                }
            }

            Registers.Add(variableName, new CreationObjectInfo(nextRegister++, creationName.ToString(4, creationName.Length - 4)));

            Instructions.Add(new Instruction(BaseInstruction.NEW, creationName.ToString()));
            Instructions.Add(new Instruction(BaseInstruction.REG_OBJ, Registers[variableName].RegisterID));
            return true;
        }

        private static bool ParseSetStatement(TextReader reader)
        {
            Advance(reader, 1); // eat ';'
            RemoveWhitespacesAndComments(reader);
            CreationObjectInfo objectInfoToPush; // Get the object to push
            string variableName;
            StringBuilder parsed;

            // Push object
            if (currentCharacter == '.')
            {
                if (currentVariable != null && Registers.ContainsKey(currentVariable))
                {
                    // Use current variable
                    objectInfoToPush = Registers[currentVariable];
                }
                else
                {
                    Log.Error("Are you missing a '...' in the previous line.  No previous 'variable' history found.", true);
                    return false;
                }
            }
            else
            {
                parsed = ParseIdentifierStatement(reader, string.Empty, new char[] { '.', '/' }, new char[0]);
                if (parsed == null)
                {
                    return false;
                }

                variableName = parsed.ToString();

                if (currentCharacter != '.')
                {
                    Log.Error($"Missing '.' starting at Line: /{StartingLine}, Column: /{StartingColumn}", true);
                    return false;
                }

                if (Registers.ContainsKey(variableName))
                {
                    // Use current variable
                    objectInfoToPush = Registers[variableName];
                }
                else
                {
                    Log.Error($"No previous 'variable' history found for /{variableName}.", true);
                    return false;
                }
            }

            // The object ID will be pushed last next we get what we are setting
            Advance(reader, 1); // eat up the '.'

            parsed = ParseIdentifierStatement(reader, $"set {objectInfoToPush.ObjectType}::", new char[] { '/', '=' }, AllowedSeperators);
            if (parsed == null)
            {
                return false;
            }

            // Replace the seperators with '.'
            for (int i = 0; i < AllowedSeperators.Length; i++)
            {
                parsed.Replace(AllowedSeperators[i], '.');
            }

            // Remove any trailing '.'
            int depth = 0;
            for (int i = parsed.Length - 1; i >= 0; i--)
            {
                if (parsed[i] == '.')
                    depth = i;
                else
                    break;
            }
            parsed.Remove(depth, parsed.Length - depth);
            variableName = parsed.ToString();

            RemoveWhitespacesAndComments(reader);

            // Should start with a '='
            if (currentCharacter != '=')
            {
                Log.Error($"Missing '=' starting at Line: /{StartingLine}, Column: /{StartingColumn}", true);
                return false;
            }

            // Handle values
            Instruction? value;
            int values = 0;
            do
            {
                Advance(reader, 1); // eat '=' or ','
                RemoveWhitespacesAndComments(reader);
                value = HandleValue(reader);
                if (value != null && value.Value.OpCode < (byte)BaseInstruction.COUNT_OF_INSTRUCTIONS)
                {
                    Instructions.Add(value.Value);
                    values++;
                }
                else
                {
                    Log.Error("Parsing values went wrong");
                    return false;
                }

                RemoveWhitespacesAndComments(reader);
            } while (currentCharacter == ',');

            Instructions.Add(new Instruction(BaseInstruction.PUSH_OBJ, objectInfoToPush.RegisterID));
            values++;

            if (values > maxSpaces) maxSpaces = values;

            // Now push object and then the set command
            Instructions.Add(new Instruction(BaseInstruction.SET, variableName));
            RemoveWhitespacesAndComments(reader);
            return true;
        }

        /// <summary>
        /// Create a new interpreter.
        /// </summary>
        /// <param name="reader"> The reader to read from. </param>
        /// <returns> An interpreter instance. </returns>
        /// <remarks> No need to dispose the reader yourself. </remarks>
        private static Interpreter GetInterpreter(TextReader reader)
        {
            Instructions.Clear();
            Registers.Clear();
            currentVariable = null;
            nextRegister = 0;
            maxSpaces = 0;

            Instructions.Add(new Instruction(BaseInstruction.MAKE_SPACE, null)); // To be set at the end - ReserveSpace
            Instructions.Add(new Instruction(BaseInstruction.MAKE_REG, null)); // To be set at the end - ReserveRegisters

            // Parse
            while (Advance(reader, 1))
            {
                // Remove whitespace/comments before first line
                RemoveWhitespacesAndComments(reader);

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

            Instructions[0] = new Instruction(BaseInstruction.MAKE_SPACE, maxSpaces);
            Instructions[1] = new Instruction(BaseInstruction.MAKE_REG, nextRegister);

            reader.Dispose();
            return new Interpreter(Instructions);
        }

        // Handles till ','
        // Check whitespace
        private static Instruction? HandleValue(TextReader reader)
        {
            object parameter = null;
            BaseInstruction code = BaseInstruction.NOP; // Similar to 'error' instruction
            string rep = "";
            int furtherType = 0; // 0 is nothing, 1 is octal, 2 is hexadecimal, 3 is string, 4 is 'string' (no more bits allowed till ','!), 5 is binary

            do
            {
                if (currentCharacter == ',' && furtherType != 3)
                {
                    break;
                }

                if (currentCharacter == '.')
                {
                    if (code == BaseInstruction.NOP && furtherType == 0)
                    {
                        code = BaseInstruction.PUSH_32F;
                        rep += currentCharacter;
                    }
                    else
                    {
                        Log.Error("Can't have '.' in octals/hexadecimals also can't have two '.' in floats/doubles.");
                        return null;
                    }
                }
                else if (furtherType == 1)
                {
                    if (currentCharacter >= '0' && currentCharacter <= '7')
                    {
                        rep += currentCharacter;
                    }
                    else
                    {
                        Log.Error("Only can use 0-7 for octals", true);
                        return null;
                    }
                }
                else if (furtherType == 2)
                {
                    if (char.IsDigit(currentCharacter) || (currentCharacter >= 'a' && currentCharacter <= 'f') || (currentCharacter >= 'A' && currentCharacter <= 'F'))
                    {
                        rep += currentCharacter;
                    }
                    else
                    {
                        Log.Error("Only can use 0-9 or a-f or A-F for hexadecimals", true);
                        return null;
                    }
                }
                else if (furtherType == 3)
                {
                    if (currentCharacter == '\\')
                    {
                        char next = (char)reader.Peek();
                        if (next == '\\' || next == '"')
                        {
                            rep += next;
                            Advance(reader, 1);
                            continue;
                        }
                    }
                    else if (currentCharacter == '"')
                    {
                        furtherType = 4;
                        parameter = rep;
                    }
                    else
                    {
                        rep += currentCharacter;
                    }
                }
                else if (furtherType == 4)
                {
                    if (char.IsWhiteSpace(currentCharacter) == false)
                    {
                        Log.Error("Ended string literal", true);
                        return null;
                    }
                }
                else if (furtherType == 5)
                {
                    if (currentCharacter == '0' || currentCharacter == '1')
                    {
                        rep += currentCharacter;
                    }
                    else
                    {
                        Log.Error("Only can use 0 or 1 for binary numbers", true);
                        return null;
                    }
                }
                else if (char.IsDigit(currentCharacter))
                {
                    char next = char.ToLower((char)reader.Peek());

                    if (next == 'x')
                    {
                        // Its a hexadecimal
                        // Keep reading till end
                        furtherType = 2;
                        Advance(reader, 1);
                    }
                    else if (next == 'o')
                    {
                        // Its an octal
                        furtherType = 1;
                        Advance(reader, 1);
                    }
                    else if (next == 'b')
                    {
                        furtherType = 5;
                        Advance(reader, 1);
                    }
                    else
                    {
                        rep += currentCharacter;
                    }
                }
                else
                {
                    if (currentCharacter == '\"')
                    {
                        if (code == BaseInstruction.NOP)
                        {
                            furtherType = 3; // don't add the '"'
                        }
                        else
                        {
                            Log.Error("Invalid string value");
                            return null;
                        }
                    }
                    else if (currentCharacter == '\'')
                    {
                        if (code != BaseInstruction.NOP)
                        {
                            Log.Error("Invalid character value");
                            return null;
                        }

                        Advance(reader, 1);
                        if (currentCharacter == '\\')
                        {
                            Advance(reader, 1);
                        }

                        char next = (char)reader.Peek();
                        if (next != '\'')
                        {
                            Log.Error("Character literal too long", true);
                            return null;
                        }
                        else
                        {
                            code = BaseInstruction.PUSH_CHAR;
                            parameter = currentCharacter;
                        }
                    }
                    else
                    {
                        char lower = char.ToLower(currentCharacter);
                        if (lower == 'd' && furtherType == 0 && (code == BaseInstruction.NOP))
                        {
                            code = BaseInstruction.PUSH_64F;
                        }
                        else if (lower == 'f' && furtherType == 0 && (code == BaseInstruction.NOP))
                        {
                            code = BaseInstruction.PUSH_32F;
                        }
                        else if (lower == 'u')
                        {
                            if (code == BaseInstruction.PUSH_32I) code = BaseInstruction.PUSH_32U;
                            if (code == BaseInstruction.PUSH_16I) code = BaseInstruction.PUSH_16U;
                            if (code == BaseInstruction.PUSH_64I) code = BaseInstruction.PUSH_64U;
                            else
                            {
                                Log.Error("u isn't valid on this value");
                                return null;
                            }
                        }
                        else if (lower == 's')
                        {
                            if (code == BaseInstruction.PUSH_32I) code = BaseInstruction.PUSH_16I;
                            if (code == BaseInstruction.PUSH_32U) code = BaseInstruction.PUSH_16U;
                            else
                            {
                                Log.Error("s isn't valid on this value");
                                return null;
                            }
                        }
                        else if (lower == 'l')
                        {
                            if (code == BaseInstruction.PUSH_32I) code = BaseInstruction.PUSH_64I;
                            if (code == BaseInstruction.PUSH_32U) code = BaseInstruction.PUSH_64U;
                            else
                            {
                                Log.Error("L isn't valid on this value");
                                return null;
                            }
                        }
                        else if (char.IsWhiteSpace(currentCharacter) == false)
                        {
                            rep += currentCharacter;
                            code = BaseInstruction.PUSH_OBJ;
                        }
                    }
                }
            } while (Advance(reader, 1));

            if (code == BaseInstruction.NOP) code = BaseInstruction.PUSH_32I;

            int baseN = 0;
            if (furtherType == 0) baseN = 10;
            else if (furtherType == 1) baseN = 8;
            else if (furtherType == 2) baseN = 16;
            else if (furtherType == 5) baseN = 2;
            else if (furtherType == 4) code = BaseInstruction.PUSH_STR;

            if (code == BaseInstruction.PUSH_16I) parameter = Convert.ToInt16(rep, baseN);
            else if (code == BaseInstruction.PUSH_32I) parameter = Convert.ToInt32(rep, baseN);
            else if (code == BaseInstruction.PUSH_64I) parameter = Convert.ToInt64(rep, baseN);
            else if (code == BaseInstruction.PUSH_16U) parameter = Convert.ToUInt16(rep, baseN);
            else if (code == BaseInstruction.PUSH_32U) parameter = Convert.ToUInt32(rep, baseN);
            else if (code == BaseInstruction.PUSH_64U) parameter = Convert.ToUInt64(rep, baseN);
            else if (code == BaseInstruction.PUSH_32F) parameter = Convert.ToSingle(rep);
            else if (code == BaseInstruction.PUSH_64F) parameter = Convert.ToDouble(rep);
            else if (code == BaseInstruction.PUSH_OBJ)
            {
                if (rep == "true")
                {
                    code = BaseInstruction.PUSH_BOOL;
                    parameter = true;
                }
                else if (rep == "false")
                {
                    code = BaseInstruction.PUSH_BOOL;
                    parameter = false;
                }
                else if (rep.Contains("."))
                {
                    // Then its a multi call
                    code = BaseInstruction.CALL;
                    parameter = "get " + string.Join(".", rep.Substring(rep.IndexOf('.') + 1).Split(AllowedSeperators.ToArray(), StringSplitOptions.RemoveEmptyEntries));
                }
                else if (Registers.ContainsKey(rep))
                {
                    parameter = Registers[rep];
                    code = BaseInstruction.PUSH_OBJ;
                }
                else
                {
                    Log.Error("Push REG Object error, register didn't contain rep");
                    return null;
                }
            }

            return new Instruction(code, parameter);
        }

        private static void RemoveWhitespacesAndComments(TextReader reader)
        {
            int blockCommentNesting = 0;
            int startingLine = 0;
            int startingColumn = 0;
            StringBuilder comment = new StringBuilder();

            do
            {
                if (currentCharacter == '*' && (char)reader.Peek() == '/')
                {
                    blockCommentNesting--;
                    Advance(reader, 1); // consume '/'
                    if (comment.Length > 0)
                    {
                        Instructions.Add(new Instruction(BaseInstruction.COMMENT, comment.ToString()));
                    }
                }

                if (!char.IsWhiteSpace(currentCharacter) && blockCommentNesting == 0)
                {
                    if (currentCharacter == '/')
                    {
                        char next = (char)reader.Peek();
                        if (next == '/')
                        {
                            Advance(reader, 1);
                            // Advance till end of line
                            Instructions.Add(new Instruction(BaseInstruction.COMMENT, AdvanceLine(reader)));
                        }
                        else if (next == '*')
                        {
                            if (blockCommentNesting == 0)
                            {
                                startingLine = CurrentLine;
                                startingColumn = CurrentColumn;
                            }
                            blockCommentNesting++;
                            Advance(reader, 1); // consume '*'
                        }
                        else
                        {
                            break; // no comments starting so we can break
                        }
                    }
                    else
                    {
                        break; // no comments starting so we can break
                    }
                }
                else
                {
                    comment.Append(currentCharacter);
                }
            } while (Advance(reader, 1));

            if (blockCommentNesting > 0)
            {
                Log.Error($"Didn't finish block comment starting at Line: /{startingLine}, Column: /{startingColumn}", true);
                return;
            }
        }

        /// <summary>
        /// Get an interpreter from a file path.
        /// </summary>
        /// <param name="filePath"> The path to the file to open. </param>
        /// <returns> An interpreter instance. </returns>
        public static Interpreter GetInterpreterFromPath(string filePath)
        {
            if (File.Exists(filePath))
            {
                return GetInterpreter(new StreamReader(new FileStream(filePath, FileMode.Open)));
            }
            else
            {
                throw new FileNotFoundException("File Path Invalid");
            }
        }

        /// <summary>
        /// Get an interpreter from text.
        /// </summary>
        /// <param name="text"> The text to interpret. </param>
        /// <returns> An interpreter instance. </returns>
        public static Interpreter GetInterpreterFromText(string text)
        {
            if (text != null)
            {
                return GetInterpreter(new StringReader(text));
            }
            else
            {
                throw new ArgumentNullException("Text is null");
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
