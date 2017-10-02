#region License
// ====================================================
// Team DOML Copyright(C) 2017 Team DOML
// This program comes with ABSOLUTELY NO WARRANTY; This is free software, 
// and you are welcome to redistribute it under certain conditions; See 
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using DOML;
using DOML.IR;
using DOML.Logger;

namespace Test
{
    [Flags]
    public enum Config
    {
        NONE = 0,
        EMIT = 1 << 0,
        SAFE_EXECUTE = 1 << 1,
        EXECUTE = 1 << 2,
        EMIT_COMMENT = 1 << 3,
        READ_EMIT = 1 << 4,
        READ_EMIT_COMMENT = 1 << 5,
        ALL = EMIT | SAFE_EXECUTE | EXECUTE | EMIT_COMMENT | READ_EMIT | READ_EMIT_COMMENT
    }

    public static class TestDOML
    {
        public static void RunFileTest(string filepath, int iterations, Config options)
        {
            Log.HandleLogs = false;
            // throw out first 1000
            for (int i = 0; i < 1000; i++)
            {
                RunParseTest(new StreamReader(new FileStream(filepath, FileMode.Open)), iterations, true);

                if (options.HasFlag(Config.EMIT))
                {
                    RunEmitTest(Parser.GetInterpreterFromPath(filepath), iterations, true, false);
                }

                if (options.HasFlag(Config.EMIT_COMMENT))
                {
                    RunEmitTest(Parser.GetInterpreterFromPath(filepath), iterations, true, true);
                }

                if (options.HasFlag(Config.READ_EMIT))
                {
                    RunReadEmitTest(Parser.GetInterpreterFromPath(filepath), iterations, true, false);
                }

                if (options.HasFlag(Config.READ_EMIT_COMMENT))
                {
                    RunReadEmitTest(Parser.GetInterpreterFromPath(filepath), iterations, true, true);
                }

                if (options.HasFlag(Config.EXECUTE))
                {
                    RunExecuteTest(Parser.GetInterpreterFromPath(filepath), iterations, true, false);
                }

                if (options.HasFlag(Config.SAFE_EXECUTE))
                {
                    RunExecuteTest(Parser.GetInterpreterFromPath(filepath), iterations, true, true);
                }
            }

            Log.Info("Ran every test 1000 times to erase any change due to JIT");

            RunParseTest(new StreamReader(new FileStream(filepath, FileMode.Open)), iterations, true);

            if (options.HasFlag(Config.EMIT))
            {
                RunEmitTest(Parser.GetInterpreterFromPath(filepath), iterations, true, false);
            }

            if (options.HasFlag(Config.EMIT_COMMENT))
            {
                RunEmitTest(Parser.GetInterpreterFromPath(filepath), iterations, true, true);
            }

            if (options.HasFlag(Config.READ_EMIT))
            {
                RunReadEmitTest(Parser.GetInterpreterFromPath(filepath), iterations, true, false);
            }

            if (options.HasFlag(Config.READ_EMIT_COMMENT))
            {
                RunReadEmitTest(Parser.GetInterpreterFromPath(filepath), iterations, true, true);
            }

            if (options.HasFlag(Config.EXECUTE))
            {
                RunExecuteTest(Parser.GetInterpreterFromPath(filepath), iterations, true, false);
            }

            if (options.HasFlag(Config.SAFE_EXECUTE))
            {
                RunExecuteTest(Parser.GetInterpreterFromPath(filepath), iterations, true, true);
            }
        }

        public static void RunStringTest(string text, int iterations, Config options)
        {
            Log.HandleLogs = false;

            // throw out first 1000
            for (int i = 0; i < 1000; i++)
            {
                RunParseTest(new StringReader(text), iterations, true);

                if (options.HasFlag(Config.EMIT))
                {
                    RunEmitTest(Parser.GetInterpreterFromText(text), iterations, true, false);
                }

                if (options.HasFlag(Config.EMIT_COMMENT))
                {
                    RunEmitTest(Parser.GetInterpreterFromText(text), iterations, true, true);
                }

                if (options.HasFlag(Config.READ_EMIT))
                {
                    RunReadEmitTest(Parser.GetInterpreterFromText(text), iterations, true, false);
                }

                if (options.HasFlag(Config.READ_EMIT_COMMENT))
                {
                    RunReadEmitTest(Parser.GetInterpreterFromText(text), iterations, true, true);
                }

                if (options.HasFlag(Config.EXECUTE))
                {
                    RunExecuteTest(Parser.GetInterpreterFromText(text), iterations, true, false);
                }

                if (options.HasFlag(Config.SAFE_EXECUTE))
                {
                    RunExecuteTest(Parser.GetInterpreterFromText(text), iterations, true, true);
                }
            }

            Log.Info("Ran every test 1000 times to erase any change due to JIT");

            RunParseTest(new StringReader(text), iterations, false);

            if (options.HasFlag(Config.EMIT))
            {
                RunEmitTest(Parser.GetInterpreterFromText(text), iterations, false, false);
            }

            if (options.HasFlag(Config.EMIT_COMMENT))
            {
                RunEmitTest(Parser.GetInterpreterFromText(text), iterations, false, true);
            }

            if (options.HasFlag(Config.READ_EMIT))
            {
                RunReadEmitTest(Parser.GetInterpreterFromText(text), iterations, false, false);
            }

            if (options.HasFlag(Config.READ_EMIT_COMMENT))
            {
                RunReadEmitTest(Parser.GetInterpreterFromText(text), iterations, false, true);
            }

            if (options.HasFlag(Config.EXECUTE))
            {
                RunExecuteTest(Parser.GetInterpreterFromText(text), iterations, false, false);
            }

            if (options.HasFlag(Config.SAFE_EXECUTE))
            {
                RunExecuteTest(Parser.GetInterpreterFromText(text), iterations, false, true);
            }
        }

        public static void RunParseTest(TextReader reader, int iterations, bool throwOut)
        {
            long total = 0;
            Stopwatch stopwatch = new Stopwatch();

            for (int i = 0; i < iterations; i++)
            {
                stopwatch.Restart();
                Interpreter interpreter = Parser.GetInterpreter(reader, false);
                stopwatch.Stop();
                interpreter.HandleSafeInstruction(new Instruction(Opcodes.NOP, ""));

                if (throwOut == false)
                {
                    total += 1000 * stopwatch.ElapsedTicks;
                }
            }

            if (throwOut == false)
            {
                Log.HandleLogs = true;
                Log.Info($"<PARSE TEST> Took {((total / (double)Stopwatch.Frequency) / iterations)}ms to complete.  Performed: {iterations} times");
                Log.HandleLogs = false;
            }
        }

        public static void RunEmitTest(Interpreter interpreter, int iterations, bool throwOut, bool withComments)
        {
            Log.HandleLogs = false;
            Stopwatch stopwatch = new Stopwatch();
            long total = 0;

            for (int i = 0; i < iterations; i++)
            {
                StringBuilder builder = new StringBuilder();
                using (IRWriter writer = new IRWriter(builder))
                {
                    stopwatch.Restart();
                    interpreter.Emit(writer, withComments);
                    stopwatch.Stop();
                    if (throwOut == false)
                    {
                        total += 1000 * stopwatch.ElapsedTicks;
                    }
                }
            }

            if (throwOut == false)
            {
                Log.HandleLogs = true;
                Log.Info($"<EMIT {(withComments ? "WITH COMMENTS" : "WITHOUT COMMENTS")} TEST> Took average of {((total / (double)Stopwatch.Frequency) / iterations)}ms to complete.  Did {iterations} times");
                Log.HandleLogs = false;
            }
        }

        public static void RunReadEmitTest(Interpreter interpreter, int iterations, bool throwOut, bool withComments)
        {
            Log.HandleLogs = false;
            Stopwatch stopwatch = new Stopwatch();
            long total = 0;
            StringBuilder builder = new StringBuilder();

            using (IRWriter writer = new IRWriter(builder))
            {
                interpreter.Emit(writer, withComments);

                for (int i = 0; i < iterations; i++)
                {
                    stopwatch.Restart();
                    Parser.GetInterpreterFromText(builder.ToString(), true);
                    stopwatch.Stop();
                    if (throwOut == false)
                    {
                        total += 1000 * stopwatch.ElapsedTicks;
                    }
                }
            }

            if (throwOut == false)
            {
                Log.HandleLogs = true;
                Log.Info($"<READ EMIT {(withComments ? "WITH COMMENTS" : "WITHOUT COMMENTS")} TEST> Took average of {((total / (double)Stopwatch.Frequency) / iterations)}ms to complete.  Did {iterations} times");
                Log.HandleLogs = false;
            }
        }

        public static void RunExecuteTest(Interpreter interpreter, int iterations, bool throwOut, bool safe)
        {
            Log.HandleLogs = false;
            Stopwatch stopwatch = new Stopwatch();
            long total = 0;

            for (int i = 0; i < iterations; i++)
            {
                stopwatch.Restart();
                interpreter.Execute(safe);
                stopwatch.Stop();
                if (throwOut == false)
                {
                    total += 1000 * stopwatch.ElapsedTicks;
                }
            }

            if (throwOut == false)
            {
                Log.HandleLogs = true;
                Log.Info($"<{(safe ? "SAFE" : "UNSAFE")} EXECUTE TEST> Took average of {((total / (double)Stopwatch.Frequency) / iterations)}ms to complete.  Did {iterations} times");
                Log.HandleLogs = false;
            }
        }
    }
}
