#region License
// ====================================================
// Team DOML Copyright(C) 2017 Team DOML
// This program comes with ABSOLUTELY NO WARRANTY; This is free software, 
// and you are welcome to redistribute it under certain conditions; See 
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion

using System;

namespace DOML.Logger
{
    /// <summary>
    /// Use this class to log errors to the user.
    /// </summary>
    public static class Log
    {
        /// <summary>
        /// The type of log.
        /// </summary>
        public enum Type
        {
            /// <summary>
            /// Info, just generic log.
            /// </summary>
            INFO,

            /// <summary>
            /// Something non-serious went wrong.
            /// </summary>
            WARNING,

            /// <summary>
            /// Fatal exception.
            /// </summary>
            ERROR,
        }

        /// <summary>
        /// Information about the log.
        /// </summary>
        public struct Information
        {
            /// <summary>
            /// The starting line, used for error messages stretching over multiple lines.
            /// </summary>
            public int StartingLine;

            /// <summary>
            /// The current line, used for error messages.
            /// </summary>
            public int CurrentLine;

            /// <summary>
            /// The starting column, used for error messages stretching over multiple columns.
            /// </summary>
            public int StartingColumn;

            /// <summary>
            /// The current column, used for error messages.
            /// </summary>
            public int CurrentColumn;

            /// <summary>
            /// Creates a new information struct.
            /// </summary>
            /// <param name="startingLine"> The starting line. </param>
            /// <param name="currentLine"> The current line. </param>
            /// <param name="startingColumn"> The starting column. </param>
            /// <param name="currentColumn"> The current column. </param>
            public Information(int startingLine, int currentLine, int startingColumn, int currentColumn)
            {
                StartingLine = startingLine;
                CurrentLine = currentLine;

                StartingColumn = startingColumn;
                CurrentColumn = currentColumn;
            }
        }

        /// <summary>
        /// If true it'll handle logs else it won't.
        /// </summary>
        public static bool HandleLogs { get; set; } = true;

        /// <summary>
        /// The handler for all logs.
        /// Event since that means you can have multiple handlers.
        /// </summary>
        public static event Action<string, Type, Information?> LogHandler = (string message, Log.Type type, Information? info) =>
        {
            if (info != null)
            {
                Console.WriteLine($"{type} from Line/Col: {info.Value.StartingLine}/{info.Value.StartingColumn} to Line/Col: {info.Value.CurrentLine}/{info.Value.CurrentColumn}; {message}");
            }
            else
            {
                Console.WriteLine(type + ": " + message);
            }
        };

        /// <summary>
        /// Log an error.
        /// </summary>
        /// <param name="error"> The error to log. </param>
        /// <param name="info"> Line information. </param>
        public static void Error(string error, Information? info = null)
        {
            if (HandleLogs) LogHandler(error, Type.ERROR, info);
        }

        /// <summary>
        /// Log a warning.
        /// </summary>
        /// <param name="warning"> The warning to log. </param>
        /// <param name="info"> Line information. </param>
        public static void Warning(string warning, Information? info = null)
        {
            if (HandleLogs) LogHandler(warning, Type.WARNING, info);
        }

        /// <summary>
        /// Log information.
        /// </summary>
        /// <param name="infoLog"> The info to log. </param>
        /// <param name="info"> Line information. </param>
        public static void Info(string infoLog, Information? info = null)
        {
            if (HandleLogs) LogHandler(infoLog, Type.INFO, info);
        }
    }
}
