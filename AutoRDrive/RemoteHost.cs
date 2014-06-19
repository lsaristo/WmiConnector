using System;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.IO;
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
    public bool execute() {
        try {
            if (!Enabled && !Driver.NO_EXECUTE) { return false; }
            if (!testConnection() || !Enabled) { return false; }
            makeSaveDirectory();
            generateRdi();
            Scope = new ManagementScope("\\\\" + HostAddress + Constants.WMI_ROOT);
            Scope.Connect();
            ManagementPath mp = new ManagementPath(Constants.CONNECTION_CLASS);
            ConnectionClass = new ManagementClass(Scope, mp, null);
            ProgramArgs = ConnectionClass.GetMethodParameters(Constants.METHOD);
            ProgramArgs["CommandLine"] = ArgsSetter;
            ConnectionClass.InvokeMethod(Constants.METHOD, ProgramArgs, null);
        } catch (Exception e) {
            Lib.log(
                Constants.LL_ERROR
                , HostName + " " 
                + e.Message + " " 
                + (e.InnerException != null ? e.InnerException.Message : "")
            );
            return false;
        }
        Lib.log(Constants.LL_INFO, Constants.LOG_SUCCESS + " " + HostName);
        return true;
    }

    /// <summary>
    /// Test connection to the RemoteHost. This method is similar to execute() but
    /// only attempts to establish a WMI connection to the RemoteHost and does not
    /// actually execute any commands.
    /// </summary>
    public bool testConnection() {
        try {
            if(HostName.Equals("")) {
                Lib.log(
                    Constants.LL_WARNING
                    , HostAddress 
                    + " has an empty hostname, skipping"
                );
                return false;
            }

            HostAddress = Dns.GetHostEntry(HostName).AddressList[0].ToString();
            Lib.debug("Resolved " + HostName + " to " + HostAddress);
        } catch (Exception e) {
            Lib.log(
                Constants.LL_WARNING
                , "Couldn't resolve host " 
                + HostName 
                + " falling back to " 
                + HostAddress
                + " DNS Server reported: "
                + e.Message
            );
        }

        bool pingSuccess = (new Ping()).Send(HostAddress).Status == IPStatus.Success; 
        
        if(!pingSuccess) {
            Lib.log(
                Constants.LL_WARNING
                , Constants.TEST_FAIL + " "
                + HostName
                + " (" + HostAddress + ") "
                + "Didn't respond to ICMP Echo Request"
            );
            return false;
        }

        try {
            Scope = new ManagementScope("\\\\" + HostAddress + Constants.WMI_ROOT);
            Scope.Connect();
            ManagementPath mp = new ManagementPath(Constants.CONNECTION_CLASS);
            ConnectionClass = new ManagementClass(Scope, mp, null);
            ProgramArgs = ConnectionClass.GetMethodParameters(Constants.METHOD);
            ConnectionClass.InvokeMethod(Constants.METHOD, ProgramArgs, null);
        } catch (Exception e) {
            Lib.log(Constants.LL_ERROR, Constants.TEST_FAIL + " " + HostName + ": " + e.Message);
            return false;
        }
        Lib.log(Constants.LL_INFO, "WMI Connection Established: " + " " + HostName);
        return true;
    }

    /// <summary>
    /// Generate the custom RDI file from this remote host.
    /// </summary>
    /// <remarks>
    /// The save directory for the resultant RDI is given by the appropriate
    /// config parameters in config.xml.
    /// </remarks>
    public void generateRdi() {
        string fileText = File.ReadAllText(
            Driver.getConfigOption(Constants.RDIMASTERPATH) 
            + "\\" 
            + Driver.getConfigOption(Constants.RDIMASTER)
        );
        string outputFile = 
            SaveDir 
            + "\\" 
            + HostName 
            + "_" 
            + DateTime.Now.ToString("yyyy_MM-dd");
        File.WriteAllText(RdiFile, fileText.Replace(Constants.PLACEHOLDER, outputFile));
    }

    /// <summary>
    /// Create the destination directory if it doesn't already exist. 
    /// </summary>
    /// <remarks>
    /// The destination directory is given by the base save directory as given
    /// by the appropriate config parameters in config.xml concatenated with 
    /// the hostname and primary user of this RemoteHost.
    /// </remarks>
    private void makeSaveDirectory() {
        SaveDir =
            SaveDir
            + "\\"
            + HostName
            + " - "
            + (PrimaryUser == "" ? HostClass : PrimaryUser);

        if (!System.IO.Directory.Exists(SaveDir))
            System.IO.Directory.CreateDirectory(SaveDir);
    }

} // End of RemoteHost class. 
} // End of namespace
