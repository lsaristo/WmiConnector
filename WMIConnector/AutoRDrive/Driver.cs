﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Xml.Linq;
using System.Data;
using System.Data.OleDb;
using System.Threading;

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
    private static  XDocument           targetXML;
    private static  XDocument           configXML;
    private static  List<String>        classesToTarget;
    private static  List<RemoteHost>    remoteHostList;
    public static   Boolean             LOG;
    public static   Boolean             DEBUG;
    public static   Boolean             NO_EXECUTE;
    public static   List<String>        failedRunners; 
    public static   Object              logLock;
    public static   Object              runnerLock;
    public static   Semaphore           runnerPhore;
    public static   Int32               count;
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
        if(!init())                 { return Constants.FATAL_INIT; }
        if(!parseProgramArgs(args)) { return Constants.FATAL_ARGS; }
        if(!parseConfigOptions())   { return Constants.FATAL_CONFIG; }
        printWelcome();
        printProps();
        
        //
        // Get the list of targets from Excel and XML files.  
        try {
            parseTargetFile();
            parseTargetFileXLS();
        } catch(Exception e) {
            Lib.logException(e);
            return Constants.FATAL_TARGETS;
        }
        if(!runSentinel()) { 
            Lib.log(Constants.ERROR_SENTINEL);
            return Constants.EXIT_FAILURE;
        } else { 
            Lib.log(Constants.INFO_ALL_DONE);
            return Constants.EXIT_SUCCESS;
        }
    }
    
    /// <summary>
    /// Primary thread's execution loop. Create child threads and monitor their
    /// progress. Report back to Main() when done. 
    /// </summary>
    private static Boolean runSentinel()
    {
        String  log1    = "Runner thread has finished";
        String  log2    = "Still waiting for runnerThread to finish";
        String  log3    = "Trying to stop socket server...";
        String  log4    = "Server shutdown. Leaving";

        ResultServer resultServer = new ResultServer();
        Thread runnerThread = new Thread(new ThreadStart(runMainLoop));
        Thread serverThread = new Thread(new ThreadStart(resultServer.runServer));
        serverThread.Start();
        runnerThread.Start();

        //
        // Wait for runnerThread
        while (true) {
            if (runnerThread.Join(Constants.RUNNER_TIME)) { 
                Lib.debug(log1);
                break;
            } else {
                Lib.debug(log2);
                purgeOld();
            }
        }
        resultServer.stop(); // TODO: This method doesn't seem to be working.
        Lib.debug(log3);
        if (serverThread.Join(Constants.SERVER_TIME)) {
            Lib.debug(log4);
        } else {
            Lib.log(Constants.WARN_SERVER_LOCK);
        }
      
        //
        // Deal with the hosts that failed. 
        foreach(String failure in failedRunners) {
            handleFailure(failure);
        }
        return true;
    }

    /// <summary>
    /// Remote any stale hosts that haven't responded in awhile.
    /// </summary>
    private static void purgeOld()
    {
        lock (runnerLock) {
            foreach (String host in currentRunners.Keys) {
                TimeSpan diff = DateTime.Now - currentRunners[host];
                if (diff.Minutes >= 100) {
                    Lib.log(host + " hasn't responded in " 
                        + diff.Minutes + " minutes. Orphaning");
                    currentRunners.Remove(host);
                    runnerPhore.Release();
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
        try {
            configXML = readFileToXML(Constants.CONFIG_FILE);
        } catch(Exception e) {
            Lib.logException(e);
            return false;
        }
        targetXML       = null;
        classesToTarget = new List<String>();
        remoteHostList  = new List<RemoteHost>();
        LOG             = true;
        DEBUG           = true;
        NO_EXECUTE      = false;
        failedRunners   = new List<String>();
        logLock         = new Object();
        runnerLock      = new Object();
        currentRunners  = new Dictionary<String,DateTime>();
        return true;
    }

    /// <summary>
    /// Handle a failed backup node.
    /// </summary>
    /// <param name="failure">Hostname of failed RemoteHost</param>
    private static void handleFailure(String failure) 
    {
        RemoteHost  failedHost  = getHostFromString(failure);
        String      log1        = "WARNING: " + failure + " reported failed backup";
        String      log2        = "WARNING: Couldn't remove " + failure + " from runners";
        String      log3        = "ERROR: Couldn't delete corrupt backup file";
        String      log4        = "Deleted corrupt backup " + failedHost.SaveDir;

        Lib.log(log1);
        if(!removeFromRunners(failure)) {
            Lib.log(log2);
        }

        if(File.Exists(failedHost.SaveDir)) {
            try { 
                File.Delete(failedHost.SaveDir);
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
        getHostFromString(host).cleanSaveDirectory();
    }

    /// <summary>
    /// Deal with message received at the ResultServer.
    /// </summary>
    /// <param name="host">Hostname for message.</param>
    /// <param name="log6">Result of operation</param>
    public static void handleMsg(String host, Byte msg)
    {
        switch(msg) {
            case Constants.RESULT_OK:
                handleSuccess(host);
                break;
            case Constants.RESULT_ERR:
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
    private static void runMainLoop()
    {
        foreach (RemoteHost host in remoteHostList) {
            String log1 = "Trying " + host.HostName;
            String log2 = "Trying to add to queue and semaphore";
            String log3 = "Executing " + host.HostName;
            String log4 = "Removing " + host.HostName + " (failed) from queue";
            String log5 = "Hosts in queue: ";

            foreach (String h in currentRunners.Keys) {
                log5 += h + " ";
            }

            Lib.debug(log1);
            Lib.debug(log5);
            Lib.debug(log2);
            runnerPhore.WaitOne();
            lock (runnerLock) {
                currentRunners.Add(host.HostName, DateTime.Now);
            }
            Lib.debug(log3);
            if (!host.execute()) {
                Lib.debug(log4);
                removeFromRunners(host.HostName);
            }
        }
        while (true) {
            lock (runnerLock) {
                if (currentRunners.Count == 0) {
                    break;
                }
            }
            String log6 =
                "Done calling hosts. Still waiting on ~ "
                + currentRunners.Count + " more host replies";
            Lib.debug(log6);
            runnerPhore.WaitOne();
        }
    }


    /// <summary>
    /// Print a help message to the Console. 
    /// </summary>
    private static void printHelp()
    {
        Console.WriteLine(Constants.INFO_HELP);
    }


    /// <summary>
    /// Print a welcome message to the log. 
    /// </summary>
    private static void printWelcome()
    {
        string welcomeString = 
            Environment.NewLine
            + "###############################" + Environment.NewLine
            + Constants.INFO_WELCOME + Environment.NewLine
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
            Lib.log(Constants.ERROR_FILE_PARSE + file + " " + e);
            throw;
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
            printWelcome();
            return false;
        }

        foreach(string arg in programArgs) {
            if(!Constants.VALID_ARGUMENTS.Contains<string>(arg.ToLower())) {
                Lib.log(Constants.ERROR_ARGUMENTS, Constants.LL_ERROR);
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
            targetXML = readFileToXML(
                getConfigOption(Constants.TARGETFILEPATH) + "\\"
                + getConfigOption(Constants.TARGETXMLFILENAME)
            );
        } catch(Exception e) {
            Lib.log(Constants.ERROR_ARGUMENTS);
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
            "Provider=" + Constants.PROVIDER + ";Data Source=" 
            + getConfigOption(Constants.TARGETFILEPATH) + "\\"
            + getConfigOption(Constants.TARGETFILENAME);

        OleDbConnection     conn    = new OleDbConnection(connString);
        DataTable           dt      = new DataTable();
        
        (new OleDbDataAdapter(Constants.QUERY, conn)).Fill(dt);

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
            targetXML.Descendants(Constants.CLASSES).Elements();
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
                            ,Enabled = 
                                param.Element(Constants.ENABLED)
                                .Value
                                .Equals(Constants.TRUE)
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
