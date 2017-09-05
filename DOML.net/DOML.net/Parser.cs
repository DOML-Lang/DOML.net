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
            int depth = -1;
            for (int i = parsed.Length - 1; i >= 0; i--)
            {
                if (parsed[i] == '.')
                    depth = i;
                else
                    break;
            }

            if (depth > -1)
            {
                parsed.Remove(depth, parsed.Length - depth);
            }

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
                if (currentCharacter == '@' || currentCharacter == ';' || reader.Peek() <= 0)
                {
                    // This allows you to have a comma at the very end
                    break;
                }

                if (ParseValue(reader, ref values) == false)
                {
                    Log.Error("Parsing values went wrong");
                    return false;
                }

                RemoveWhitespacesAndComments(reader);
            } while (currentCharacter == ',');

            // If we don't have enough values - compile time error
            if (values < InstructionRegister.SizeOf[variableName])
            {
                Log.Error("Too few variables.");
                return false;
            }

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
            while (currentCharacter == '@' || currentCharacter == ';' || Advance(reader, 1))
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
                    builder.Append(AllowedSeperators.Contains(currentCharacter) ? '.' : currentCharacter);
                }
                while (Advance(reader, 1) && currentCharacter != ',' && char.IsWhiteSpace(currentCharacter) == false);

                string result = builder.ToString();
                if (result == "true")
                {
                    Instructions.Add(new Instruction(BaseInstruction.PUSH_BOOL, true));
                    values++;
                    return true;
                }
                else if (result == "false")
                {
                    Instructions.Add(new Instruction(BaseInstruction.PUSH_BOOL, false));
                    values++;
                    return true;
                }
                else
                {
                    // Its an object
                    if (result.Contains('.'))
                    {
                        string[] splitObject = result.Split('.');
                        string callee;
                        if (Registers.ContainsKey(splitObject[0]))
                        {
                            // We are referring to one of our objects
                            CreationObjectInfo info = Registers[splitObject[0]];
                            Instructions.Add(new Instruction(BaseInstruction.PUSH_OBJ, info.RegisterID));
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

                        Instructions.Add(new Instruction(BaseInstruction.CALL, callee));
                        values += InstructionRegister.SizeOf[callee];
                        return true;
                    }
                    else if (Registers.ContainsKey(result))
                    {
                        // We are passing an object
                        Instructions.Add(new Instruction(BaseInstruction.PUSH_OBJ, Registers[result].RegisterID));
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
        }

        private static bool ParseStringOrCharacter(TextReader reader, bool character, ref int values)
        {
            StringBuilder builder = new StringBuilder();
            bool escaped = false;
            char endOn = character ? '\'' : '"';
            // We can do it like this because we want to throw the starting string character out anyway
            while (Advance(reader, 1))
            {
                if (currentCharacter == endOn && escaped == false)
                {
                    break;
                }
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

            if (currentCharacter == endOn)
            {
                Instructions.Add(new Instruction(character ? BaseInstruction.PUSH_CHAR : BaseInstruction.PUSH_STR, builder.ToString()));
                values++;
                return true;
            }
            else
            {
                Log.Error($"No ending {endOn}");
                return false;
            }
        }

        private static bool ParseNumber(TextReader reader, ref int values)
        {
            StringBuilder builder = new StringBuilder();
            int baseN = 10;
            bool underscore = false;

            if (currentCharacter == '-' || currentCharacter == '+')
            {
                builder.Append(currentCharacter);
            }
            else if (currentCharacter == '$')
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
                    if (next == '.')
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
                if (char.IsWhiteSpace(currentCharacter))
                    break;

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
                else if (baseN == 2 && (currentCharacter != '0' || currentCharacter != '1'))
                    break;
                else if (char.IsDigit(currentCharacter) == false && baseN != 16)
                    break;
                else if (baseN == 7 && (currentCharacter > '7'))
                    break;
                else if (baseN == 16 && (('a' <= currentCharacter) && (currentCharacter <= 'f') || ('A' <= currentCharacter) && (currentCharacter <= 'F')) == false)
                    break;
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
                Instructions.Add(new Instruction(BaseInstruction.PUSH_DEC, Convert.ToDecimal(builder.ToString())));
            else if (baseN == 0)
                // Floating Point
                Instructions.Add(new Instruction(BaseInstruction.PUSH_NUM, Convert.ToDouble(builder.ToString())));
            else
                // Int
                Instructions.Add(new Instruction(BaseInstruction.PUSH_INT, Convert.ToInt64(builder.ToString(), baseN)));

            values++;
            return true;
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
