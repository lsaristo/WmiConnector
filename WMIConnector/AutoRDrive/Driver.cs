﻿/*
 * Driver.cs
 * 
 * Driver implementation. Primary program driver.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Xml.Linq;
using System.Data;
using System.Data.OleDb;
using System.Threading;
using System.Diagnostics;

namespace AutoBack 
{

/// <summary>
/// Primary driver class for this program. This class facilitates the running                               
/// logic of the program.
/// </summary>
/// <remarks>
/// The file path for config.xml, the program's primary configuration file, is
/// hard-coded in Constants. All other file paths and configuration options are
/// loaded from config.xml. Note that UNC paths require read/write access in the 
/// security context of the user running this program. 
/// </remarks>
public class Driver 
{
    //
    // Exit codes
    private const int EXIT_FAILURE   = -1;
    private const int EXIT_SUCCESS   = 0;
    private const int FATAL_INIT     = -2;
    private const int FATAL_ARGS     = -3;
    private const int FATAL_CONFIG   = -4;
    private const int FATAL_TARGETS  = -5;
    private const int FATAL_THREAD   = -6;
    private const int FATAL_PCOUNT   = -7;
    private const int FATAL_LOGGING  = -8;

    //
    // Threading constants
    private const int RUNNER_TIME    = 10 * 60000;  // 10 minutes
    private const int SERVER_TIME    = 1 * 60000;   // 1 minute
    private const int TS_TIMEOUT     = 1 * 60000;   // 1 minute
    public const int ORPHAN_TIMEOUT  = 150;         // minutes

    //
    // String constants for output / logging
    private const string INFO_ALL_DONE = "************ DONE **************";
    private const string INFO_WELCOME = "Auto R-Drive";
    private const string ERROR_ARGUMENTS = "ERROR: invalid program arguments";
    private const string ERROR_SENTINEL = "ERROR: Sentinel threw an exception";

    //
    // Misc
    public static readonly string[] VALID_ARGUMENTS = {
        Constants.WORKSTATION, 
        Constants.OTHER, 
        Constants.TEST, 
        Constants.SERVER
    };

    //
    // Class properties
    private static  XDocument           configXML;
    private static  List<String>        classesToTarget;
    private static  List<RemoteHost>    remoteHostList;
    private static  ResultServer        resultServer;
    public static   Boolean             LOG;
    public static   Boolean             DEBUG;
    public static   Boolean             NO_EXECUTE;
    public static   Object              logLock;
    public static   Object              runnerLock;
    public static   Semaphore           runnerPhore;
    public static   Int32               count;
    public static   Thread              serverThread;
    public static   Thread              runnerThread;
    public static   Dictionary<String, DateTime> currentRunners; 

    /// <summary>
    /// Main program entry point. Populate host list and execute commands
    /// against every host. 
    /// </summary>
    /// <param name="args">Desired class to target. See Constants.cs for 
    /// supported arguments.
    /// </param>
    public static int Main(string[] args)
    {
        //
        // Basic program setup
        if(!init())                 { return FATAL_INIT; }
        if(!Lib.init())             { return FATAL_LOGGING; }
        if(!isLoaner())             { return FATAL_PCOUNT; }
        if(!parseProgramArgs(args)) { return FATAL_ARGS; }
        if(!parseConfigOptions())   { return FATAL_CONFIG; }
        printWelcome();
        printProps();
        
        //
        // Get the list of targets from Excel and XML files.  
        try {
            parseTargetFile();
            parseTargetFileXLS();
        } catch(Exception e) {
            Lib.logException(e);
            return FATAL_TARGETS;
        }

        //
        // Execute main sentinel thread. Last block before exit. 
        if(!runSentinel()) { 
            Lib.log(ERROR_SENTINEL);
            return EXIT_FAILURE;
        } else { 
            Lib.log(INFO_ALL_DONE);
            return EXIT_SUCCESS;
        }
    }


    /// <summary>
    /// Check whether another instance of this executable is running.
    /// </summary>
    private static Boolean isLoaner()
    {
        String processes = "";
        String log1 = 
            "ERROR: Only one instance of this program may run "
            + "at a time. Another is already running.";

        var currentCount = 
            Process
            .GetProcesses()
            .Where(x => x.ToString().Contains("AutoRDrive"));

        if(currentCount.Count() > 1) {
            Lib.log(log1);

            foreach (var proc in currentCount) {
                processes += proc.ProcessName + " ";
            }
            Lib.debug("Process already found: " + processes);
            return false;
        }
        return true;
    }

    /// <summary>
    /// Construct a result server and initialize the thread that will run it.
    /// </summary>
    /// <returns>True if the thread to run the server has started.</returns>
    private static Boolean startServ()
    {
        resultServer = new ResultServer();
        if(!resultServer.init()) { return false; }
        serverThread = new Thread(new ThreadStart(resultServer.runServer));
        serverThread.Start();
        return !(!serverThread.IsAlive && serverThread.Join(TS_TIMEOUT));
    }

    /// <summary>
    /// Stop 
    /// </summary>
    /// <returns></returns>
    private static Boolean stopServ()
    {
        Lib.debug("Stopping listening server");
        resultServer.stop();
        if (serverThread.Join(SERVER_TIME)) {
            return true;
        } else {
            return false;
        }
    }


    
    /// <summary>
    /// Primary thread's execution loop. Create child threads and monitor their
    /// progress. Report back to Main() when done. 
    /// </summary>
    private static Boolean runSentinel()
    {
        String log1 = "Runner thread has finished";
        String log2 = "Sentinel is waiting for runnerThread to finish";
        String log5 = "ERROR: Failed to start one or more support threads.";
        String log6 = "ERROR: Result Server has crashed. Aborting.";

        //
        // Start support threads
        if(!startServ() || !startRunner()) {
            Lib.log(log5);
            return false;
        }

        //
        // Wait for runnerThread
        while (true) {
            if (!serverThread.IsAlive) {
                Lib.log(log6);
                runnerThread.Abort();
                return false;
            }
            if (runnerThread.Join(RUNNER_TIME)) { 
                Lib.debug(log1);
                break;
            } else {
                Lib.debug(log2);

                try { 
                    purgeOld();
                } catch (Exception e) {
                    Lib.log("Something went wrong in purge");
                    Lib.logException(e);
                }
            }
        }

        //
        // Shutdown the ResultServer
        if(!stopServ()) { Lib.debug("Warning: ResultServer not responding"); }
        return true;
    }

    /// <summary>
    /// Run the thread that will run runMainLoop()
    /// </summary>
    /// <returns>True if started successfully.</returns>
    private static bool startRunner()
    {
        runnerThread = new Thread(new ThreadStart(runnerInit));
        runnerThread.Start();    
        return !(!runnerThread.IsAlive && runnerThread.Join(TS_TIMEOUT));
    }

    /// <summary>
    /// Run the Runner thread. 
    /// </summary>
    private static void runnerInit()
    {
        runMainLoop();
    }

    /// <summary>
    /// Remote any stale hosts that haven't responded in awhile.
    /// </summary>
    private static void purgeOld()
    {
        List<String> hostsToDelete = new List<String>();
        lock (runnerLock) {
            foreach (String host in currentRunners.Keys) {
                TimeSpan diff = DateTime.Now - currentRunners[host];
                Lib.debug(host + " running for " + diff.TotalMinutes + " minutes so far.");
                RemoteHost rm = getHostFromString(host);
                
                if (rm.LastChance) {
                    Lib.debug(rm.HostName + " LastChance marker set. Abandoning");
                    hostsToDelete.Add(host);
                    continue;
                } 
                if (!rm.isUp()) {
                    Lib.log("WARNING: Lost contact with host " + rm.HostName + ". Abandoning");
                    hostsToDelete.Add(host);
                    continue;
                }
                if (!rm.processAlive()) {
                    Lib.log(
                        "WARNING: Process not found " + rm.HostName
                        + " will wait for one more check cycle before abandoning");
                    rm.LastChance = true;
                    continue;
                } else {
                    Lib.debug(rm.HostName + " PID " + rm.PID + " still active.");
                }
            }

            foreach(String host in hostsToDelete) {
                if(!currentRunners.Remove(host)) {
                    Lib.log("WARNING: Failed to ORPHAN " + host);
                } else {
                    Lib.log("Orphaned " + host + " (probably) releasing its lock");

                    try { 
                        runnerPhore.Release();
                    } catch(Exception e) {
                        Lib.logException(e);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Display the values of specific program arguments.
    /// </summary>
    /// <remarks>
    /// We implement this here because the DEBUG flag will supress a lot of 
    /// information from this point forth and so this is a good place to ensure
    /// that the appropriate flags are set the way they are thought to be.
    /// </remarks>
    private static void printProps()
    {
        String outString = 
            "Program Flags: Debug: " + DEBUG + ", Log: " + LOG 
            + ", NO_EXECUTE: " + NO_EXECUTE;
        Lib.debug(outString);
        outString = "";        
        foreach (String host in classesToTarget) {
            outString += host + " ";
        }
        Lib.log("Targeting class(s): " + outString);
    }

    /// <summary>
    /// Initialize basic static data members for use in this program. 
    /// </summary>
    /// <remarks>
    /// It's always easier to do it this way rather than assign static variables
    /// in their declaration since exceptions thrown during assignment make for 
    /// very unpleasant debugging since the exception is caught outside of our
    /// code.
    /// </remarks>
    private static bool init()
    {
        //
        // Basic parameters must be initialized first.
        classesToTarget = new List<String>();
        remoteHostList = new List<RemoteHost>();
        LOG = true;
        DEBUG = true;
        NO_EXECUTE = false;
        logLock = new Object();
        runnerLock = new Object();
        currentRunners = new Dictionary<String, DateTime>();

        //
        // Get config file data or die.
        try {
            configXML = readFileToXML(Constants.CONFIG_FILE);
        } catch (Exception) {
            return false;
        }
        return true;
    }

    /// <summary>
    /// Handle a failed backup node.
    /// </summary>
    /// <param name="failure">Hostname of failed RemoteHost</param>
    private static void handleFailure(String failure) 
    {
        RemoteHost failedHost = getHostFromString(failure);
        String log1 = "WARNING: " + failure + " reported failed backup";
        String log2 = "WARNING: Couldn't remove " + failure + " from runners";
        String log3 = "ERROR: Couldn't delete corrupt backup file";
        String log4 = "Deleted corrupt backup " + failedHost.SaveDir;

        Lib.log(log1);
        if(!removeFromRunners(failure)) {
            Lib.log(log2);
        }

        String badFile = 
            failedHost.SaveDir + "\\" + failedHost.SaveFile 
            + Constants.BU_FILE_EXT;

        if(File.Exists(badFile)) {
            try { 
                File.Delete(badFile);
                Lib.log(log4);
            } catch(Exception e) {
                Lib.log(log3);
                Lib.logException(e);
            }
        }
    }

    /// <summary>
    /// Atomically remove a host from the list of current runners.
    /// </summary>
    /// <param name="host">Hostname to remove.</param>
    private static Boolean removeFromRunners(String host)
    {
        lock(runnerLock) {
            if (currentRunners.Remove(host)) {
                runnerPhore.Release();
                return true;
            }
        } 
        return false;
    }


    /// <summary>
    /// Handle a successful host backup.
    /// </summary>
    /// <param name="host">Hostname of RemoteHost</param>
    private static void handleSuccess(String host)
    {
        String log1 = "ERROR: Couldn't get object for " + host;
        String log2 = "ERROR: " + host + " not found in runners list";
        String log3 = "Host: " + host + " reports result: SUCCESS";
        RemoteHost  host_object;
        
        if((host_object = getHostFromString(host)) == null) {
            Lib.log(log1);
        } else {
            host_object.cleanSaveDirectory();
        }

        if(!removeFromRunners(host)) {
            Lib.log(log2);
        }
        Lib.log(log3);
    }

    /// <summary>
    /// Deal with message received at the ResultServer.
    /// </summary>
    /// <param name="host">Hostname for message.</param>
    /// <param name="log7">Result of operation</param>
    public static void handleMsg(String host, Byte msg)
    {
        String log1 = "Handling success for " + host;
        String log2 = "Handling failure for " + host;

        switch(msg) {
            case Constants.RESULT_OK:
                Lib.debug(log1);
                handleSuccess(host);
                break;
            case Constants.RESULT_ERR:
                Lib.debug(log2);
                handleFailure(host);
                break;
        }
    }

    /// <summary>
    /// Retrieve a RemoteHost from a hostname String.
    /// </summary>
    /// <param name="host">Hostname to retrieve</param>
    /// <returns>RemoteHost matching this hostname, or NULL.</returns>
    private static RemoteHost getHostFromString(String host)
    {
        try { 
            return remoteHostList.Where(x => x.HostName.Equals(host)).ToArray()[0];
        } catch(Exception e) {
            Lib.logException(e);
            return null;
        }
    }

    /// <summary>
    /// Iterate through the remoteHostList and execute() or test() each host.
    /// </summary>
    private static Boolean runMainLoop()
    {
        String  log2    = "ERROR in runner thread. Listening server not alive";
        String  log3    = "FATAL: Server doesn't seem to have started. Leaving";
        int     stall   = 6;

        while(!serverThread.IsAlive && serverThread.Join(TS_TIMEOUT)) {
            if((stall -= 1) > 0) {
                Lib.log(log3);
                return false;
            }
            Lib.log(log2);
            Lib.log("Retrying " + stall + " more times before giving up");
        }

        foreach (RemoteHost host in remoteHostList) {
            String log6 = "Trying " + host.HostName;
            String log7 = "Asking for semaphore...";
            String log8 = "Executing " + host.HostName;
            String log4 = "Removing " + host.HostName + " (failed) from queue";
            String log5 = "Hosts in queue: ";
            String log9 = "Skipping " + host.HostName;

            foreach (String h in currentRunners.Keys) {
                log5 += h + " ";
            }

            Lib.debug("Working on " + host.HostName);
            Lib.debug(log7);
            runnerPhore.WaitOne();
            Lib.debug(log6);
            Lib.debug(log5);

            if (!host.Enabled) {
                Lib.log(host.HostName + " is disabled. Ignoring");
                runnerPhore.Release();
                continue;
            }
            if (!host.preConnect()) {
                Lib.log(host.HostName + " connection failed. Skipping");
                runnerPhore.Release();
                continue;
            }
            lock (runnerLock) {
                currentRunners.Add(host.HostName, DateTime.Now);
            }
            Lib.debug(log8);
            if (!host.execute()) {
                Lib.debug(log4);
                Lib.log(log9);
                removeFromRunners(host.HostName);
            } else {
                Lib.debug("Got PID for process: " + host.PID);
            }
        }
        while (true) {
            String log6 =
                "Done calling hosts. Still waiting on ~ "
                + currentRunners.Count + " more host replies";
            Lib.debug(log6);
            lock (runnerLock) {
                if (currentRunners.Count == 0) {
                    break;
                }
            }
            runnerPhore.WaitOne();
        }
        return true;
    }


    /// <summary>
    /// Print a help message to the Console. 
    /// </summary>
    private static void printHelp()
    {
        Console.WriteLine(
            "Version: " 
            + System.Reflection.Assembly.GetEntryAssembly().GetName().Version
            + Environment.NewLine 
            + "Usage: AutoRDrive.exe [class1] [class2] ...\n"
        );
    }


    /// <summary>
    /// Print a welcome message to the log. 
    /// </summary>
    private static void printWelcome()
    {
        string welcomeString = 
            Environment.NewLine
            + "###############################" + Environment.NewLine
            + INFO_WELCOME + Environment.NewLine
            + "Version: "
            + System.Reflection.Assembly.GetEntryAssembly().GetName().Version
            + Environment.NewLine
            + "###############################";
        Lib.log(welcomeString);
    }

    /// <summary>
    /// Read in data from an XML-formatted file.
    /// </summary>
    /// <param name="file">File to read.</param>
    /// <returns>XDocument representation of the file.</returns>
    private static XDocument readFileToXML(string file)
    {
        try {
            return XDocument.Load(file);
        } catch (Exception e) {
            Console.WriteLine("ERROR: Failed to read XML data");
            Lib.logException(e);
            return null;
        }
    }

    /// <summary>
    /// Read program configuration options from config.xml.
    /// </summary>
    /// <param name="param">Requested program option to be targeted.</param>
    /// <returns>String representation of param's value per config.xml</returns>
    public static string getConfigOption(XName param)
    {
        if (configXML == null)
            readFileToXML(Constants.CONFIG_FILE);
        foreach (XElement option in configXML.Descendants(Constants.OPTIONS))
            return option.Element(param).Value;
        return null;
    }

    /// <summary>
    /// Process command line arguments. See Constants for a list of supported
    /// arguments. If any arguments provided are invalid, an Exception is 
    /// thrown and the program will exit with status EXIT_FAILURE. 
    /// </summary>
    private static Boolean parseProgramArgs(string[] programArgs)
    {
        if(programArgs.Length == 0) {
            printHelp();
            return false;
        }

        foreach(string arg in programArgs) {
            if(!VALID_ARGUMENTS.Contains<string>(arg.ToLower())) {
                Lib.log(ERROR_ARGUMENTS);
                return false;
            }
            if(!classesToTarget.Contains(arg)) { 
                classesToTarget.Add(arg.ToLower());
            } else {
                Lib.log("WARNING: Duplicate class given");
            }
        }
        return true;
    }

    /// <summary>
    /// Parse program configuration options and set config variables. If any of 
    /// the required configuration parameters are missing from config.xml, an 
    /// Exception is thrown and thevprogram will exit with status EXIT_FAILURE.  
    /// </summary>
    /// <returns>True if successful. False otherwise.</returns>
    private static Boolean parseConfigOptions()
    {
        Func<XName, Boolean> cond = 
            x => getConfigOption(x).ToLower().Equals(Constants.TRUE);

        try { 
            DEBUG = cond(Constants.DEBUG);
            LOG = cond(Constants.LOGGING);
            NO_EXECUTE = cond(Constants.WHATIF);
            count = Convert.ToInt32(getConfigOption(Constants.CONCURRENT_LIMIT));
        } catch(Exception e) {
            Lib.log(ERROR_ARGUMENTS);
            Lib.logException(e);
            return false;
        }
        runnerPhore = new Semaphore(count, count);
        return true;
    }

    /// <summary>
    /// Read hosts in from a copy of Doc_0001473 stored in the directory given 
    /// by the appropriate config parameters in config.xml. 
    /// </summary>
    /// <remarks>
    /// The Excel file must not be in use when this method executes or it will
    /// fail with an IO Exception. Note that the "IP Address" column must be 
    /// in cononical form (specifically, no leading zeros are supported) or the 
    /// targeting for the coorsponding host will fail. 
    /// </remarks>
    private static void parseTargetFileXLS()
    {
        String connString = 
            @"Provider=Microsoft.Jet.OLEDB.4.0;Extended Properties=Excel 8.0;Data Source=" 
            + getConfigOption(Constants.TARGETFILEPATH) + "\\"
            + getConfigOption(Constants.TARGETFILENAME);
        OleDbConnection conn = new OleDbConnection(connString);
        DataTable dt = new DataTable();
        String query = @"Select * from [Sheet1$A2:L]";
        
        (new OleDbDataAdapter(query, conn)).Fill(dt);

        foreach (DataRow dr in dt.Rows) {
            if (classesToTarget.Contains(dr[@"Class"].ToString().ToLower())) {
                remoteHostList.Add(
                    new RemoteHost {
                        HostAddress = dr["IP Address"].ToString()
                        ,HostName = dr["Net Name"].ToString().ToUpper()
                        ,HostClass = dr["class"].ToString()
                        ,PrimaryUser = dr["Primary User"].ToString()
                        ,Enabled = dr["BU Enable"].ToString().ToLower().Equals(Constants.YES)
                        ,SaveDir = getConfigOption(Constants.SAVEDIRBASE) + "\\" + dr["class"]
                        ,ArgsSetter = 
                            getConfigOption(Constants.EXECUTABLE_PATH) + "\\" 
                            + getConfigOption(Constants.EXECUTABLE_NAME)
                        ,RdiFile = getConfigOption(Constants.RDIMASTERPATH) 
                            + "\\custom\\" + dr["Net Name"].ToString() + ".rdi"
                        ,HistoryCount = Convert.ToInt32(getConfigOption(Constants.HISTORY_COUNT))
                    }
                );
            }
        }
    }

    /// <summary>
    /// Read hosts in from targets.xml and populate the list of RemoteHosts.
    /// </summary>
    /// <remarks>
    /// This file is used to manually add systems to be added to the backup 
    /// roster that don't have any natural place in the standard Excel master 
    /// IP matrix. The only supported class that will be processed by this file 
    /// is "Other". The remaining classes listed are deprecated and will be 
    /// removed in future releases.
    /// </remarks>
    private static void parseTargetFile()
    {
        IEnumerable<XElement> typeList = 
            readFileToXML(
                getConfigOption(Constants.TARGETFILEPATH) + "\\" 
                + getConfigOption(Constants.TARGETXMLFILENAME)
            ).Descendants(Constants.CLASSES).Elements();

        Func<XElement, bool> condition =
            x => classesToTarget.Contains<string>(
                x.Element(Constants.CLASSNAME).Value.ToLower()
            );

        foreach (XElement classTypes in typeList) {
            if (condition(classTypes)) {
                foreach (XElement param in classTypes.Elements(Constants.HOST)) {
                    remoteHostList.Add(
                        new RemoteHost {
                            HostAddress = param.Element(Constants.HOST_ADDRESS).Value
                            ,HostName = param.Element(Constants.HOST_NAME).Value.ToUpper()
                            ,PrimaryUser = param.Element(Constants.USER).Value
                            ,SaveDir = classTypes.Element(Constants.SAVEDIR).Value
                            ,HostClass = classTypes.Element(Constants.CLASSNAME).Value
                            ,ArgsSetter = 
                                getConfigOption(Constants.EXECUTABLE_PATH) + "\\" 
                                + getConfigOption(Constants.EXECUTABLE_NAME)
                            ,Enabled = param.Element(Constants.ENABLED).Value.Equals(Constants.TRUE)
                            ,RdiFile = 
                                getConfigOption(Constants.RDIMASTERPATH) + "\\custom\\" 
                                + param.Element(Constants.HOST_NAME).Value + ".rdi"
                            ,HistoryCount = Convert.ToInt32(getConfigOption(Constants.HISTORY_COUNT))
                        }
                    );
                }
            }
        }
    }
} // End Driver class
} // End Namespace
