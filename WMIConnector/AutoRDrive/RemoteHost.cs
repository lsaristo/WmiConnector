/*
 * RemoteHost.cs
 * 
 * RemoteHost class implementation.
 */

using System;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
namespace AutoBack 
{

/// <summary>
/// Container class for a remote host computer. Each target host 
/// will be represented by an instantiation of this class.
/// </summary>
class RemoteHost 
{
    //
    // WMI Constants
    private const string WMI_ROOT = @"\root\cimv2";

    //
    // Logging
    public const string LOG_SUCCESS = "Successfully executed command";
    public const string TEST_FAIL = "Error: Could not contact host";

    // Instance properties.
    public Boolean                 Enabled         { get; set; }
    public String                  SaveDir         { get; set; }
    public String                  HostAddress     { get; set; }
    public String                  HostClass       { get; set; }
    public String                  HostName        { get; set; }
    public String                  PrimaryUser     { get; set; }
    public String                  ArgsSetter      { get; set; }
    public String                  RdiFile         { get; set; }
    public String                  SaveFile        { get; set; }
    public Int32                   HistoryCount    { get; set; }
    public Int32                   PID             { get; set; }
    public ManagementScope         Scope           { get; set; }
    public ManagementClass         ConnectionClass { get; set; }
    public ManagementBaseObject    ProgramArgs     { get; set; }
    

    /// <summary>
    /// Connect to the remote host and execute the command. See config.xml. 
    /// If anything goes wrong during the process, the applicable exception
    /// is caught and we return false. 
    /// </summary>
    public bool execute()
    {
        try {
            if (!Enabled && !Driver.NO_EXECUTE) { return false; }
            if (!preConnect() || !Enabled) { return false; }
            makeSaveDirectory();
            if (!makeSaveFile()) { return false; }
            consolodateSaveDirs();
            cleanSaveDirectory();
            generateRdi();
            PID = invokeMethod();
        } catch (Exception e) {
            Lib.logException(e, HostName);
            return false;
        }
        Lib.log(LOG_SUCCESS + " " + HostName);
        return true;
    }

    /// <summary>
    /// Connect to the target system and execute the command.
    /// </summary>
    /// <returns>PID reported back from the target system.</returns>
    private Int32 invokeMethod()
    {
        return Convert.ToInt32(
            ConnectionClass.InvokeMethod(
                Constants.METHOD, 
                ProgramArgs, null
            )["ProcessID"]
        );
    }

    /// <summary>
    /// Query remote host and verify hostname is accurate. 
    /// </summary>
    public void verifyHostname() 
    {
        ObjectQuery query = 
            new ObjectQuery("Select csname from Win32_OperatingSystem");
        ManagementObjectSearcher searcher =
            new ManagementObjectSearcher(Scope, query);
        ManagementObjectCollection queryCol = searcher.Get();

        foreach (var m in queryCol) {
            if (m["csname"].ToString() != HostName) {
                var oldName = HostName;
                HostName = m["csname"].ToString();
                String warn =
                    "WARNING: Hostname per config: " + oldName 
                    + " but WMI says " + HostName + ". Changing";
                Lib.log(warn);
            }
        }
    }

    public String queryWMI(String inQuery, String property)
    {
        ObjectQuery query = 
            new ObjectQuery(inQuery);
        ManagementObjectSearcher searcher =
            new ManagementObjectSearcher(Scope, query);
        ManagementObjectCollection queryCol = searcher.Get();
        String outString = "";

        foreach(var m in queryCol) {
            outString += m[property] + ",";
        }
        return outString;
    }

    /// <summary>
    /// Ping host to see if it is alive.
    /// </summary>
    /// <returns>True if host responds to ICMP Echo</returns>
    public Boolean isUp()
    {
        return ((new Ping()).Send(HostAddress).Status == IPStatus.Success);
    }

    /// <summary>
    /// Test connection to the RemoteHost. This method is similar to execute()
    /// but only attempts to establish a WMI connection to the RemoteHost and
    /// does not actually execute any commands.
    /// </summary>
    /// <returns>True if success.</returns>
    public bool preConnect()
    {
        //
        // GET IP for host.
        if(!resolveHost()) { return false; }

        //
        // Ping to see if host is up.
        if(!isUp()) { 
            Lib.log("Host not responding. Skipping");
            return false; 
        }

        try {
            Scope = new ManagementScope("\\\\" + HostAddress + WMI_ROOT);
            Scope.Connect();
            ManagementPath mp = new ManagementPath("win32_process");
            ConnectionClass = new ManagementClass(Scope, mp, null);
            ProgramArgs = ConnectionClass.GetMethodParameters(Constants.METHOD);
            var ProgramArgs_Dummy = ConnectionClass.GetMethodParameters(Constants.METHOD);
            ProgramArgs["CommandLine"] = ArgsSetter;
            ConnectionClass.InvokeMethod(Constants.METHOD, ProgramArgs_Dummy, null);
        } catch (Exception e) {
            Lib.logException(e, TEST_FAIL + " " + HostName);
            return false;
        }
        Lib.debug("WMI Connection Established: " + " " + HostName);
        return true;
    }

    /// <summary>
    /// Try to query hostname via DNS. Log failures. 
    /// </summary>
    /// <returns>True if success.</returns>
    private bool resolveHost()
    {
        String log1 = HostAddress + " has an empty hostname, skipping";
        String log2 = "Resolved " + HostName + " to " + HostAddress;
        String log3 = 
            "Couldn't resolve host " + HostName + " falling back to " 
            + HostAddress;

        if(HostName.Equals("")) {
            Lib.log(log1);
            return false;
        }

        try {    
            HostAddress = Dns.GetHostEntry(HostName).AddressList[0].ToString();
            Lib.debug(log2);
            return true;
        } catch (Exception e) {
            Lib.log(log3);
            Lib.logException(e);
        }
        return false;
    }


    /// <summary>
    /// Generate the custom RDI file from this remote host.
    /// </summary>
    /// <remarks>
    /// The save directory for the resultant RDI is given by the appropriate
    /// config parameters in config.xml.
    /// </remarks>
    public void generateRdi()
    {
        string fileText = File.ReadAllText(
            Driver.getConfigOption(Constants.RDIMASTERPATH) + "\\" 
            + Driver.getConfigOption(Constants.RDIMASTER));

        String fullPath = SaveDir + "\\" + SaveFile;
        File.WriteAllText(RdiFile, fileText.Replace(Constants.PLACEHOLDER, fullPath));
    }

    /// <summary>
    /// Generate the formatted output file to be used as a backup file name. 
    /// </summary>
    /// <returns>True if filename does not already exist.</returns>
    private Boolean makeSaveFile()
    {
        SaveFile = HostName + "_" + DateTime.Now.ToString(Constants.DATE_FORMAT);

        if(File.Exists(SaveDir + "\\" + SaveFile + Constants.BU_FILE_EXT)) {
            Lib.debug(SaveDir + "\\" + SaveFile + " already exists, Skipping");
            return false;
        }
        return true;
    }

    /// <summary>
    /// Clean the destination directory of stale backups.
    /// </summary>
    public void cleanSaveDirectory() 
    {
        string[] files;
        Comparison<String> comp = (x, y) => {
            return File.GetCreationTime(x).CompareTo(File.GetCreationTime(y));
        };

        while ((files = Directory.GetFiles(SaveDir, Constants.BU_FILE_WILD).ToArray()).Length > HistoryCount) {
            Array.Sort(files, comp);
            String file = Enumerable.First<String>(files);
            File.Delete(file);
            Lib.log("Too many backups for host (limit " + HistoryCount +"). Removing: " + file);
        }
    }

    /// <summary>
    /// If multiple directories are found containing the HostName of this 
    /// RemoteHost they are consolidated into a single directory as defined
    /// by this RemoteHost's SaveDir property.
    /// </summary>
    private void consolodateSaveDirs() {
        String basePath = SaveDir.Substring(0, SaveDir.LastIndexOf("\\"));
        var dirs =
            from dir in Directory.GetDirectories(basePath, "*"+HostName+"*")
            where dir != SaveDir
            select dir;
      
        foreach (String dir in dirs) {
            Lib.log("Found duplicate folder for hostname " + HostName);

            foreach (String file in Directory.GetFiles(dir)) {
                File.Move(file, SaveDir + "\\" + Path.GetFileName(file));
                Lib.log("Moved " + file + " from duplicate dir to " + SaveDir);
            }
            
            foreach (String subdir in Directory.GetDirectories(dir)) {
                Directory.Move(subdir, SaveDir+"\\"+Path.GetFileName(subdir));
                Lib.log("Moved " + subdir + " from duplicate dir to " + SaveDir);
            }
            
            if (Directory.EnumerateFileSystemEntries(dir).ToArray().Length == 0) {
                Directory.Delete(dir);
                Lib.log("Removed duplicate directory " + dir);
            } else {
                Lib.log("ERROR: Duplicate directory not empty (but should be): " + dir);
            }
        }
    }

    /// <summary>
    /// Create the destination directory if it doesn't already exist. 
    /// </summary>
    /// <remarks>
    /// The destination directory is given by the base save directory as given
    /// by the appropriate config parameters in config.xml concatenated with 
    /// the hostname and primary user of this RemoteHost.
    /// </remarks>
    private void makeSaveDirectory() 
    {
        SaveDir =
            SaveDir + "\\"
            + HostName + " - " + (PrimaryUser == "" ? HostClass : PrimaryUser);

        if (!Directory.Exists(SaveDir))
            Directory.CreateDirectory(SaveDir);
    }
} // End of RemoteHost class. 
} // End of namespace
