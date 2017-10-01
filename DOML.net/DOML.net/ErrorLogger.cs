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
    public static class Log
    {
        public enum Type
        {
            INFO,
            WARNING,
            ERROR,
        }

        public static bool HandleLogs { get; set; } = true;

        /// <summary>
        /// The handler for all logs.
        /// Event since that means you can have multiple handlers.
        /// </summary>
        public static event Action<string, Type, bool> LogHandler;

        /// <summary>
        /// Log an error.
        /// </summary>
        /// <param name="error"> The error to log. </param>
        public static void Error(string error, bool useLineNumbers = true)
        {
            if (HandleLogs) LogHandler(error, Type.ERROR, useLineNumbers);
        }

        /// <summary>
        /// Log a warning.
        /// </summary>
        /// <param name="warning"> The warning to log. </param>
        public static void Warning(string warning, bool useLineNumbers = true)
        {
            if (HandleLogs) LogHandler(warning, Type.WARNING, useLineNumbers);
        }

        /// <summary>
        /// Log information.
        /// </summary>
        /// <param name="info"> The info to log. </param>
        public static void Info(string info, bool useLineNumbers = true)
        {
            if (HandleLogs) LogHandler(info, Type.INFO, useLineNumbers);
        }
    }
}
