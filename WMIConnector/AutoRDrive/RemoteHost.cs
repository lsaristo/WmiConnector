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
    public bool Enabled { get; set; }
    public string SaveDir { get; set; }
    public string HostAddress { get; set; }
    public string HostClass { get; set; }
    public string HostName { get; set; }
    public string PrimaryUser { get; set; }
    public ManagementScope Scope { get; set; }
    public ManagementClass ConnectionClass { get; set; }
    public ManagementBaseObject ProgramArgs { get; set; }
    public string ArgsSetter { get; set; }
    public string RdiFile { get; set; }

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
    /// Test connection to the RemoteHost. This method is similar to execute()
    /// but only attempts to establish a WMI connection to the RemoteHost and
    /// does not actually execute any commands.
    /// </summary>
    public bool preConnect()
	{
        try {
            if(HostName.Equals("")) {
                Lib.log(HostAddress + " has an empty hostname, skipping");
                return false;
            }
            HostAddress = Dns.GetHostEntry(HostName).AddressList[0].ToString();
            Lib.debug("Resolved " + HostName + " to " + HostAddress);
        } catch (Exception e) {
            Lib.log(
                "Couldn't resolve host " + HostName 
                + " falling back to " + HostAddress
                + " DNS Server reported: " + e.Message
                , Constants.LL_WARNING
            );
        }
        bool pingSuccess = (new Ping()).Send(HostAddress).Status == IPStatus.Success; 
        if(!pingSuccess) {
            Lib.log(
                HostName + " (" + HostAddress + ") "
                + "Didn't respond to ICMP Echo Request"
                , Constants.LL_WARNING
            );
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
        } catch (Exception e) {
            Lib.logException(e, Constants.TEST_FAIL + " " + HostName);
            return false;
        }
        Lib.log("WMI Connection Established: " + " " + HostName);
        return true;
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
            + Driver.getConfigOption(Constants.RDIMASTER)
        );
        string outputFile = 
            SaveDir + "\\" 
            + HostName + "_" + DateTime.Now.ToString("yyyy_MM-dd");
        File.WriteAllText(RdiFile, fileText.Replace(Constants.PLACEHOLDER, outputFile));
    }

    /// <summary>
    /// Clean the destination directory of stale backups.
    /// </summary>
    private void cleanSaveDirectory() 
    {
        string[] files;
        Comparison<String> comp = (x, y) => {
            return File.GetCreationTime(x).CompareTo(File.GetCreationTime(y));
        };
        
        while ((files = Directory.GetFiles(SaveDir).ToArray()).Length > 2) {
            Array.Sort(files, comp);
            String file = Enumerable.First<String>(files);
            File.Delete(file);
            Lib.log("Removed old file: " + file);
        }
    }

    /// <summary>
    /// Create the destination directory if it doesn't alrready exist. 
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

        if (!System.IO.Directory.Exists(SaveDir))
            System.IO.Directory.CreateDirectory(SaveDir);
    }
} // End of RemoteHost class. 
} // End of namespace
