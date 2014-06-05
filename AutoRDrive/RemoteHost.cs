using System;
using System.Management;
using System.IO;
using System.Data;
using System.Data.OleDb;
namespace AutoBack 
{

/// <summary>
/// Container class for a remote host computer. Each host listed in targets.xml
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
    /// </summary>
    public bool execute() {
        try {
            makeSaveDirectory();
            generateRdi();
            Scope = new ManagementScope("\\\\" + HostAddress + Constants.WMI_ROOT);
            Scope.Connect();
            ManagementPath mp = new ManagementPath(Constants.CONNECTION_CLASS);
            ConnectionClass = new ManagementClass(Scope, mp, null);
            ProgramArgs = ConnectionClass.GetMethodParameters(Constants.METHOD);
            ProgramArgs["CommandLine"] = ArgsSetter;
        } catch (Exception e) {
            Lib.log(Constants.LL_ERROR, HostName + ": " + e.Message);
            return false;
        }
        return true;
    }

    /// <summary>
    /// Generate the custom RDI file from this remote host.
    /// </summary>
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
    private void makeSaveDirectory() {
        SaveDir = 
            SaveDir 
            + "\\" 
            + HostName 
            + " - " 
            + PrimaryUser;

        if (!System.IO.Directory.Exists(SaveDir))
            System.IO.Directory.CreateDirectory(SaveDir);
    }
} // End of RemoteHost class. 
} // End of namespace
