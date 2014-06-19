using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace AutoBack 
{
    /// <summary>
    /// Common library routines used by the program. 
    /// Debugging and logging are done here.
    /// </summary>
    /// <see cref="Constants.cs"/>
    static class Lib 
    {
        private static string logFile = null;
        private static string logPath = null;

        /// <summary>
        /// Write an event to the System log.
        /// </summary>
        /// <param name="level">Severity level. Valid inputs are those
        /// logging severity levels supported by the Windows API.</param>
        /// <param name="message">Message to log.</param>
        /// <see cref="Constants.cs"/> 
        /// <see cref="config.xml"/>
        public static void log(string level, string message) {
            if (Driver.LOG) {
                string fullLogPath = null;
                string fallbackLog = null;
                string logString = null;

                try {
                    logFile = Driver.getConfigOption(Constants.LOGFILE);
                    logPath = Driver.getConfigOption(Constants.LOGPATH);
                    fullLogPath = logPath + "\\" + logFile;
                    fallbackLog = 
                        Environment.CurrentDirectory + Constants.FALLBACK_LOG;
                    logString = 
                        DateTime.Now.ToString() 
                        + ": " 
                        + message 
                        + Environment.NewLine;

                    using(StreamWriter w = File.AppendText(fullLogPath)) {
                        w.Write(logString);
                    }
                } catch(Exception e) {
                    using(StreamWriter w = File.AppendText(fallbackLog)) {
                        w.Write(logString);
                    }
                }

                if (Driver.DEBUG) {
                    Console.WriteLine(logString);
                }
            }
        }

        /// <summary>
        /// Write debugging output to the console. Enabled with '-d' program 
        /// argument. 
        /// </summary>
        /// <remarks>This method does nothing if Driver.DEBUG is false.
        /// </remarks>
        /// <param name="message">Message to write.</param>
        public static void debug(string message) {
            if(Driver.DEBUG) {
                log(Constants.LL_INFO, "[ DEBUG ]: " + message);
            }
        }

        /// <summary>
        /// Assert that a condition holds. Used for debugging. 
        /// </summary>
        /// <remarks>This method does nothing if Driver.DEBUG is false.
        /// </remarks>
        /// <param name="condition">Condition that must be true</param>
        /// <param name="message">Error message if condition is false to log.
        /// </param>
        public static void assertTrue(bool condition, string message) {
            if (Driver.DEBUG && !condition) {
                debug(message + " " + condition);
                Environment.Exit(Constants.EXIT_FAILURE);
            }              
        }

        public static void assertTrue(bool condition) {
            assertTrue(condition, Constants.ERROR_ASSERT);
        }
    }
}
