/*
 * Lib.cs
 * 
 * Lib implementation for logging.
 */
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
    //
    // Logging constants.
    private const string FILE_LOGS = "Filed Logs";
    private const int LOG_SIZE_LIMIT = 200000; // Bytes
    
    private static string logFile = null;
    private static string logPath = null;
    private static string fullLogPath = null;
    private static string imageLog = null;
    private static string filerPath = null;

    /// <summary>
    /// Write an event to the System log.
    /// </summary>
    /// <param name="level">Severity level. Valid inputs are those
    /// logging severity levels supported by the Windows API.
    /// </param>
    /// <param name="log7">Message to log.</param>
    /// <see cref="Constants.cs"/> 
    /// <see cref="config.xml"/>
    public static void log(string msg, string host = null)
    {
        if (!Driver.LOG) { return; }
        String logString = 
        DateTime.Now.ToString() + ": " + msg + Environment.NewLine;

        lock (Driver.logLock) {
            Console.WriteLine(logString);
            try {
                using (StreamWriter w = File.AppendText(fullLogPath)) {
                    w.Write(logString);
                }
            } catch (Exception e) {
                Console.WriteLine("ERROR: LOGGING FAILURE: " + e.Message);
            }
        }
    }

    /// <summary>
    /// Perform log initialization. 
    /// </summary>
    public static Boolean init()
    {
        logFile = Driver.getConfigOption(Constants.LOGFILE);
        logPath = Driver.getConfigOption(Constants.LOGPATH);
        fullLogPath = logPath + "\\" + logFile;
        imageLog = Driver.getConfigOption(Constants.IMAGE_LOG);
        filerPath = logPath + "\\" + FILE_LOGS;

        try { 
            turnover();
        } catch(IOException) {
            return false;
        }
        return true;
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
        if(!File.Exists(fullLogPath)) {
            File.Create(fullLogPath).Close();
            log("INFO: Log created");
        }
        if(!File.Exists(imageLog)) {
            // TODO: Log something.
        }

        if ((new FileInfo(fullLogPath)).Length > LOG_SIZE_LIMIT) {
            for (; File.Exists( filerPath+"\\"+newLogName+"_"+i+".log" ); i++)
                ;
            File.Move(fullLogPath, filerPath+"\\"+newLogName+"_"+i+".log");
            log("INFO: Log turned over" + Environment.NewLine);
        }

        if ((new FileInfo(imageLog)).Length > LOG_SIZE_LIMIT) {
            // TODO probably do something with turning the image log.
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
        if (Driver.DEBUG) { log(msg: "[ DEBUG ]: " + message); }
    }

    /// <summary>
    /// Write an exception to the log. 
    /// </summary>
    /// <param name="e">Exception object</param>
    /// <param name="hostIdentifier">Hostname (if applicable) that caused it
    /// </param>
    public static void logException(Exception e, string hostIdentifier = null)
    {
        string error = 
            "Caught exception: " 
            + (hostIdentifier != null ? hostIdentifier : " ") 
            + e.Message + " " 
            + (e.InnerException != null ? e.InnerException.Message : "");
        log(error);
    }


}
}
