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
    public  Boolean                 Enabled         { get; set; }
    public  String                  SaveDir         { get; set; }
    public  String                  HostAddress     { get; set; }
    public  String                  HostClass       { get; set; }
    public  String                  HostName        { get; set; }
    public  String                  PrimaryUser     { get; set; }
    public  String                  ArgsSetter      { get; set; }
    public  String                  RdiFile         { get; set; }
    public  Int32                   HistoryCount    { get; set; }
    public  ManagementScope         Scope           { get; set; }
    public  ManagementClass         ConnectionClass { get; set; }
    public  ManagementBaseObject    ProgramArgs     { get; set; }
    

    /// <summary>
    /// Connect to the remote host and execute the command. See config.xml. 
    /// If anything goes wrong during the process, the applicable exception
    /// is caught and we return false. 
    /// </summary>
    public bool execute()
    {
        try {
            if (!Enabled && !Driver.NO_EXECUTE) { return false; }
            if (!preConnect() || !Enabled)      { return false; }
            makeSaveDirectory();
            consolodateSaveDirs();
            cleanSaveDirectory();
            generateRdi();
            ConnectionClass.InvokeMethod(Constants.METHOD, ProgramArgs, null);
        } catch (Exception e) {
            Lib.logException(e, HostName);
            return false;
        }
        Lib.log(Constants.LOG_SUCCESS + " " + HostName);
        return true;
    }

    /// <summary>
    /// Query remote host and verify hostname is accurate. 
    /// </summary>
    private void verifyHostname() 
    {
        ObjectQuery query = 
            new ObjectQuery(Constants.WMI_QUERY);
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
                lock (Driver.runnerLock) {
                    if (Driver.currentRunners.ContainsKey(oldName)) {
                        DateTime dt = Driver.currentRunners[oldName];
                        Driver.currentRunners.Remove(oldName);
                        Driver.currentRunners.Add(HostName, dt);
                        Lib.debug("Changed currentRunner for " + oldName
                            + "to " + HostName + " at "
                            + Driver.currentRunners[HostName]);
                    } else {
                        Lib.log("WARNING: Couldn't find " + oldName + " in list");
                    }
                }
            }
        }
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
        if(!((new Ping()).Send(HostAddress).Status == IPStatus.Success)) {
            Lib.log("Host not responding. Skipping");
            return false;
        }

        try {
            Scope = new ManagementScope("\\\\" + HostAddress + Constants.WMI_ROOT);
            Scope.Connect();
            ManagementPath mp = new ManagementPath(Constants.CONNECTION_CLASS);
            ConnectionClass = new ManagementClass(Scope, mp, null);
            ProgramArgs = ConnectionClass.GetMethodParameters(Constants.METHOD);
            var ProgramArgs_Dummy = ConnectionClass.GetMethodParameters(Constants.METHOD);
            ProgramArgs["CommandLine"] = ArgsSetter;
            ConnectionClass.InvokeMethod(Constants.METHOD, ProgramArgs_Dummy, null);
            verifyHostname();
        } catch (Exception e) {
            Lib.logException(e, Constants.TEST_FAIL + " " + HostName);
            return false;
        }
        Lib.log("WMI Connection Established: " + " " + HostName);
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

        try {
            if(HostName.Equals("")) {
                Lib.log(log1);
                return false;
            }
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
        string outputFile = 
            SaveDir + "\\" 
            + HostName + "_" + DateTime.Now.ToString(Constants.DATE_FORMAT);
        File.WriteAllText(RdiFile, fileText.Replace(Constants.PLACEHOLDER, outputFile));
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

        while ((files = Directory.GetFiles(SaveDir, Constants.BU_FILE_EXT).ToArray()).Length > HistoryCount) {
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
