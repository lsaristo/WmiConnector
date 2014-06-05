using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Data;
using System.Data.OleDb;

namespace AutoBack 
{

/// <summary>
/// Primary driver class for this program. Execution begins and ends here. 
/// </summary>
public class Driver 
{
    private static XDocument targetXML = null; 
    private static XDocument configXML = readFileToXML(Constants.CONFIG_FILE);
    private static List<string> classesToTarget = new List<string>();
    private static List<RemoteHost> remoteHostList = new List<RemoteHost>();
    public static bool LOG = false;
    public static bool DEBUG = false;
    public static bool NO_EXECUTE = false;

    /// <summary>
    /// Main program entry point. Populate host list and execute commands
    /// against every host. 
    /// </summary>
    /// <param name="args">Desired class to target. Refer to targets.xml</param>
    public static void Main(string[] args) {
        parseProgramArgs(args);
        parseConfigOptions();
        string outString = null;
        foreach (string host in classesToTarget)
            outString += host + " ";
        printWelcome(outString);
        parseTargetFile();
        parseTargetFileXLS();
        foreach (RemoteHost host in remoteHostList) 
            if(host.Enabled)
                host.execute();
    }

    /// <summary>
    /// Print a welcome message to the log. 
    /// </summary>
    private static void printWelcome(string outString) {
        string welcomeString = 
            "###############################"
            + "AutoRDrive has started"
            + "Targeting class(s) : "
            + outString
            + "###############################";
       Lib.log(Constants.LL_INFO, welcomeString);
    }


    /// <summary>
    /// Read in data from an XML-formatted file.
    /// </summary>
    /// <param name="file">File to read.</param>
    /// <returns>XDocument representation of the file.</returns>
    private static XDocument readFileToXML(string file) {
        try {
            return XDocument.Load(file);
        } catch (Exception e) {
            Lib.log(Constants.LL_ERROR, Constants.ERROR_FILE_PARSE + file + " " + e);
            throw new Exception();
        }
    }

    /// <summary>
    /// Read program configuration options from config.xml.
    /// </summary>
    /// <param name="param">Requested program option to be targeted.</param>
    /// <returns>String representation of param's value per config.xml</returns>
    public static string getConfigOption(XName param) {
        if (configXML == null)
            readFileToXML(Constants.CONFIG_FILE);
        foreach (XElement option in configXML.Descendants(Constants.OPTIONS))
            return option.Element(param).Value;
        return null;
    }

    /// <summary>
    /// Process command line arguments.
    /// </summary>
    private static void parseProgramArgs(string[] programArgs) {
        foreach(string arg in programArgs) {
            if(!Constants.VALID_ARGUMENTS.Contains<string>(arg.ToLower())) {
                Lib.log(Constants.LL_ERROR, Constants.ERROR_ARGUMENTS);
                throw new Exception();
            }
            classesToTarget.Add(arg.ToLower());
        }
    }

    /// <summary>
    /// Parse program configuration options and set config variables. 
    /// </summary>
    private static void parseConfigOptions() {
        DEBUG = 
            getConfigOption(Constants.DEBUG)
            .ToLower()
            .Equals(Constants.TRUE);
        LOG = 
            getConfigOption(Constants.LOGGING)
            .ToLower()
            .Equals(Constants.TRUE);
        NO_EXECUTE = 
            getConfigOption(Constants.WHATIF)
            .ToLower()
            .Equals(Constants.TRUE);
        targetXML = 
            readFileToXML(
                getConfigOption(Constants.TARGETFILEPATH) 
                + "\\"
                + getConfigOption(Constants.TARGETXMLFILENAME)
            );
    }

    /// <summary>
    /// Read hosts in from Doc_0001473
    /// </summary>
    private static void parseTargetFileXLS() {
        OleDbConnection conn = new OleDbConnection(
            "Provider="
            + Constants.PROVIDER
            + ";Data Source="
            + getConfigOption(Constants.TARGETFILEPATH)
            + "\\"
            + getConfigOption(Constants.TARGETFILENAME)
        );
        OleDbDataAdapter da = new OleDbDataAdapter(Constants.QUERY, conn);
        DataTable dt = new DataTable();
        da.Fill(dt);

        foreach (DataRow dr in dt.Rows) {
            if (classesToTarget.Contains(dr[@"Class"].ToString().ToLower())) {
                remoteHostList.Add(
                    new RemoteHost {
                        HostAddress = dr["IP Address"].ToString()
                        ,HostName = dr["Net Name"].ToString()
                        ,HostClass = dr["class"].ToString()
                        ,PrimaryUser = dr["Primary User"].ToString()
                        ,SaveDir = 
                            getConfigOption(Constants.SAVEDIRBASE) 
                            + "\\" 
                            + dr["class"]
                        ,ArgsSetter = 
                            getConfigOption(Constants.EXECUTABLE_PATH) 
                            + "\\" 
                            + getConfigOption(Constants.EXECUTABLE_NAME)
                        ,Enabled = 
                            NO_EXECUTE 
                            ? false 
                            : dr["BU Enable"]
                                .ToString()
                                .ToLower()
                                .Equals(Constants.YES)
                        ,RdiFile = getConfigOption(Constants.RDIMASTERPATH) 
                            + "\\custom\\" 
                            + dr["Net Name"].ToString() 
                            + ".rdi"
                    }
                );
            }
        }
    }

    /// <summary>
    /// Read hosts in from targets.xml and populate the list of RemoteHosts.
    /// </summary>
    private static void parseTargetFile() {
        foreach (XElement classTypes in targetXML.Descendants(Constants.CLASSES).Elements()) {
            if (classesToTarget.Contains<string>(classTypes.Element(Constants.CLASSNAME).Value.ToLower())) {
                foreach (XElement param in classTypes.Elements(Constants.HOST)) {
                    remoteHostList.Add(
                        new RemoteHost {
                            HostAddress = param.Element(Constants.HOST_ADDRESS).Value
                            ,HostName = param.Element(Constants.HOST_NAME).Value
                            ,PrimaryUser = param.Element(Constants.USER).Value
                            ,SaveDir = classTypes.Element(Constants.SAVEDIR).Value
                            ,HostClass = classTypes.Element(Constants.CLASSNAME).Value
                            ,ArgsSetter = 
                                getConfigOption(Constants.EXECUTABLE_PATH)
                                + "\\" 
                                + getConfigOption(Constants.EXECUTABLE_NAME)
                            ,Enabled = 
                                NO_EXECUTE 
                                ? false 
                                : param.Element(Constants.ENABLED)
                                    .Value
                                    .Equals(Constants.TRUE)
                            ,RdiFile = 
                                getConfigOption(Constants.RDIMASTERPATH) 
                                + "\\custom\\" 
                                + param.Element(Constants.HOST_NAME).Value 
                                + ".rdi"
                        }
                    );
                }
            }
        }
    }
} // End Driver class
} // End Namespace
