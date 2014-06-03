﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace AutoBack {

    /// <summary>
    /// Common library routines used by the program. 
    /// Debugging and logging are done here.
    /// </summary>
    /// <see cref="Constants.cs"/>
    static class Lib {
        private static string logFile = null;
        private static string logPath = null;

        /// <summary>
        /// Write an event to the System log.
        /// </summary>
        /// <remarks>Logging must be enabled in the program config file</remarks>
        /// <param name="level">Severity level. Valid inputs are those
        /// logging severity levels supported by the Windows API.</param>
        /// <param name="message">Message to log.</param>
        /// <see cref="Constants.cs"/> 
        /// <see cref="config.xml"/>
        public static void log(string level, string message) {
            if (Driver.LOG_ENABLED) {
                if (logFile == null || logPath == null) {
                    logFile = Driver.getConfigOption(Constants.LOGFILE);
                    logPath = Driver.getConfigOption(Constants.LOGPATH);
                }
                
                using(StreamWriter w = File.AppendText(logPath + "\\" + logFile)) {
                    w.Write(DateTime.Now.ToString() + ": " + message + Environment.NewLine);
                }
            }
        }

        /// <summary>
        /// Write debugging output to the console. Enabled with '-d' program argument. 
        /// </summary>
        /// <remarks>This method does nothing if Driver.DEBUG is false.</remarks>
        /// <param name="message">Message to write.</param>
        public static void debug(string message) {
            if (Driver.DEBUG_ENABLED)
                Console.WriteLine("[ DEBUG ]: " + message);
        }

        /// <summary>
        /// Assert that a condition holds. Used for debugging. 
        /// </summary>
        /// <remarks>This method does nothing if Driver.DEBUG is false.</remarks>
        /// <param name="condition">Condition that must be true</param>
        /// <param name="message">Error message if condition is false to log.</param>
        public static void assertTrue(bool condition, string message) {
            if (Driver.DEBUG_ENABLED) {
                if (!condition) {
                    debug(message + " " + condition);
                    throw new Exception();
                }
            }              
        }

        public static void assertTrue(bool condition) {
            assertTrue(condition, Constants.ERROR_ASSERT);
        }
    }

}