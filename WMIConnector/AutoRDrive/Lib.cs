using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
    private static string logFile       = null;
    private static string logPath       = null;
    private static string fullLogPath   = null;
    private static string imageLog      = null;
    private static string filerPath     = null;

    /// <summary>
    /// Write an event to the System log.
    /// </summary>
    /// <param name="level">Severity level. Valid inputs are those
    /// logging severity levels supported by the Windows API.
    /// </param>
    /// <param name="log7">Message to log.</param>
    /// <see cref="Constants.cs"/> 
    /// <see cref="config.xml"/>
    public static void log(string msg, string host = null, string level = Constants.LL_INFO)
    {
        lock (Driver.logLock) {
            if (!Driver.LOG) { return; }

            string fallbackLog = null;
            string logString = null;

            try {
                fallbackLog = Environment.CurrentDirectory + Constants.FALLBACK_LOG;
                logString = DateTime.Now.ToString() + ": " + msg + Environment.NewLine;
                Console.WriteLine(logString);

                using (StreamWriter w = File.AppendText(fullLogPath)) {
                    w.Write(logString);
                }
            } catch (Exception e) {
                using (StreamWriter w = File.AppendText(fallbackLog)) {
                    w.Write(logString + " " + e);
                }
            }
        }
    }

    /// <summary>
    /// Perform log initialization. 
    /// </summary>
    public static void logInit()
    {
        logFile = Driver.getConfigOption(Constants.LOGFILE);
        logPath = Driver.getConfigOption(Constants.LOGPATH);
        fullLogPath = logPath + "\\" + logFile;
        imageLog = Driver.getConfigOption(Constants.IMAGE_LOG);
        filerPath = logPath + "\\" + Constants.FILE_LOGS;
        turnover();
    }

    /// <summary>
    /// Move logs into a filer if they are too big. 
    /// </summary>
    public static void turnover() 
    {
        int i = 1;
        String newLogName = logFile.Split('.')[0];
        String newImageName =
            imageLog.Substring(imageLog.LastIndexOf("\\") + 1).Split('.')[0];

        if (!Directory.Exists(filerPath)) {
            Directory.CreateDirectory(filerPath);
        }

        if (File.Exists(fullLogPath) 
                && (new FileInfo(fullLogPath)).Length > Constants.LOG_SIZE_LIMIT) {

            for ( ; File.Exists(filerPath + "\\" + newLogName + "_" + i + ".log"); i++)
                ;
            File.Move(fullLogPath, filerPath + "\\" + newLogName + "_" + i + ".log");
            log("INFO: Log turned over");
        }

        if (File.Exists(imageLog)
                && (new FileInfo(imageLog)).Length > Constants.LOG_SIZE_LIMIT) {

            for (i = 1; File.Exists(filerPath + "\\" + newImageName + "_" + i + ".log"); i++)
                ;
            File.Move(imageLog, filerPath + "\\" + newImageName + "_" + i + ".log");

            using (StreamWriter w = File.AppendText(imageLog)) {
                w.Write(
                    DateTime.Now.ToString() + "INFO: Log turned over"  
                    + Environment.NewLine
                );
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
    public static void debug(string message, string host = null)
    {
        if (Driver.DEBUG) {
            log(msg: "[ DEBUG ]: " + message);
        }
    }

    /// <summary>
    /// Write an exception to the log. 
    /// </summary>
    /// <param name="e">Exception object</param>
    /// <param name="hostIdentifier">Hostname (if applicable) that caused it</param>
    public static void logException(Exception e, string hostIdentifier = null)
    {
        string error = 
            "Caught exception: " 
            + (hostIdentifier != null ? hostIdentifier : " ") 
            + e.Message + " " 
            + (e.InnerException != null ? e.InnerException.Message : "");
        log(error);
    }

    /// <summary>
    /// Assert that a condition holds. Used for debugging. 
    /// </summary>
    /// <remarks>This method does nothing if Driver.DEBUG is false.
    /// </remarks>
    /// <param name="condition">Condition that must be true</param>
    /// <param name="log7">Error log7 if condition is false to log.
    /// </param>
    public static void assertTrue(bool cond, string msg = Constants.ERROR_ASSERT) 
    {
        if (Driver.DEBUG && !cond) {
            debug(msg + " " + cond);
            Environment.Exit(Constants.EXIT_FAILURE);
        }              
    }

    internal static void log(object p)
    {
        throw new NotImplementedException();
    }
}
}
