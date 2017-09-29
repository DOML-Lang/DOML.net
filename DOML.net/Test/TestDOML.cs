using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        ALL = EMIT | EMIT_COMMENT | SAFE_EXECUTE | EXECUTE
    }

    public static class TestDOML
    {
        public static void RunFileTest(string filepath, int iterations, Config options)
        {
            Log.HandleLogs = false;
            // throw out first 1000
            for (int i = 0; i < 1000; i++)
            {
                Interpreter throwOut = Parser.GetInterpreterFromPath(filepath);
                RunInterpreterTest(throwOut, iterations, true, options);
            }

            Stopwatch stopwatch = new Stopwatch();

            long total = 0;
            for (int i = 0; i < iterations; i++)
            {
                stopwatch.Restart();
                Interpreter interpreter = Parser.GetInterpreterFromPath(filepath);
                stopwatch.Stop();
                total += stopwatch.ElapsedTicks;
            }

            Log.HandleLogs = true;
            Log.Info($"<Parse TEST> Took {(total / iterations) / 1000d}ms to complete.  Performed: {iterations} times after 1000 times");
            Log.HandleLogs = false;

            RunInterpreterTest(Parser.GetInterpreterFromPath(filepath), iterations, false, options);
        }

        public static void RunStringTest(string text, int iterations, Config options)
        {
            Log.HandleLogs = false;

            // throw out first 1000
            for (int i = 0; i < 1000; i++)
            {
                Interpreter throwOut = Parser.GetInterpreterFromText(text);
                RunInterpreterTest(throwOut, iterations, true, options);
            }

            Stopwatch stopwatch = new Stopwatch();

            long total = 0;
            for (int i = 0; i < iterations; i++)
            {
                stopwatch.Restart();
                Interpreter interpreter = Parser.GetInterpreterFromText(text);
                stopwatch.Stop();
                total += stopwatch.ElapsedTicks;
            }

            Log.HandleLogs = true;
            Log.Info($"<Parse TEST> Took {(total / iterations) / 1000d}ms to complete.  Performed: {iterations} times after 1000 times");
            Log.HandleLogs = false;

            RunInterpreterTest(Parser.GetInterpreterFromText(text), iterations, false, options);
        }

        public static void RunInterpreterTest(Interpreter interpreter, int iterations, bool throwOut, Config options)
        {
            Log.HandleLogs = false;

            Stopwatch stopwatch = new Stopwatch();

            if (options.HasFlag(Config.EMIT))
            {
                long total = 0;
                for (int i = 0; i < iterations; i++)
                {
                    StringBuilder builder = new StringBuilder();
                    using (IRWriter writer = new IRWriter(builder))
                    {
                        stopwatch.Restart();
                        interpreter.Emit(writer, false);
                        stopwatch.Stop();
                        if (throwOut == false)
                        {
                            total += stopwatch.ElapsedTicks;
                        }
                    }
                }

                if (throwOut == false)
                {
                    Log.HandleLogs = true;
                    Log.Info($"<EMIT TEST> Took average of {(total / iterations) / 1000d}ms to complete.  Did {iterations} times");
                    Log.HandleLogs = false;
                }
            }

            if (options.HasFlag(Config.EMIT_COMMENT))
            {
                long total = 0;
                for (int i = 0; i < iterations; i++)
                {
                    StringBuilder builder = new StringBuilder();
                    using (IRWriter writer = new IRWriter(builder))
                    {
                        stopwatch.Restart();
                        interpreter.Emit(writer, true);
                        stopwatch.Stop();
                        if (throwOut == false)
                        {
                            total += stopwatch.ElapsedTicks;
                        }
                    }
                }

                if (throwOut == false)
                {
                    Log.HandleLogs = true;
                    Log.Info($"<EMIT WITH COMMENTS TEST> Took average of {(total / iterations) / 1000d}ms to complete.  Did {iterations} times");
                    Log.HandleLogs = false;
                }
            }

            if (options.HasFlag(Config.SAFE_EXECUTE))
            {
                long total = 0;
                for (int i = 0; i < iterations; i++)
                {
                    stopwatch.Restart();
                    interpreter.Execute(true);
                    stopwatch.Stop();
                    if (throwOut == false)
                    {
                        total += stopwatch.ElapsedTicks;
                    }
                }

                if (throwOut == false)
                {
                    Log.HandleLogs = true;
                    Log.Info($"<SAFE EXECUTE TEST> Took average of {(total / iterations) / 1000d}ms to complete.  Did {iterations} times");
                    Log.HandleLogs = false;
                }
            }

            if (options.HasFlag(Config.EXECUTE))
            {
                long total = 0;
                for (int i = 0; i < iterations; i++)
                {
                    stopwatch.Restart();
                    interpreter.Execute(false);
                    stopwatch.Stop();
                    if (throwOut == false)
                    {
                        total += stopwatch.ElapsedTicks;
                    }
                }

                if (throwOut == false)
                {
                    Log.HandleLogs = true;
                    Log.Info($"<UNSAFE EXECUTE TEST> Took average of {(total / iterations) / 1000d}ms to complete.  Did {iterations} times");
                    Log.HandleLogs = false;
                }
            }
        }
    }
}
