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
    public static bool LOG = true;
    public static bool DEBUG = true;
    public static bool NO_EXECUTE = false;

    /// <summary>
    /// Main program entry point. Populate host list and execute commands
    /// against every host. 
    /// </summary>
    /// <param name="args">Desired class to target. Refer to targets.xml</param>
    public static void Main(string[] args) {
        try {
            printWelcome();
            parseProgramArgs(args);
            parseConfigOptions();

            Lib.debug("Program Flags: Debug: " + DEBUG + ", Log: " + LOG + ", NO_EXECUTE: " + NO_EXECUTE);    
            
            string outString = null;
            foreach (string host in classesToTarget)
                outString += host + " ";

            Lib.log(Constants.LL_INFO, "Targeting class(s): " + outString);
            parseTargetFile();
            parseTargetFileXLS();
        } catch(Exception e) {
            Lib.log(Constants.LL_ERROR, "FATAL ERROR: " + e.Message + " " + e.ToString());
            System.Environment.Exit(Constants.EXIT_FAILURE);
        }

        Lib.debug("Trying " + remoteHostList.Count + " hosts");
        foreach (RemoteHost host in remoteHostList) {
            Lib.debug("Calling host " + host.HostName + " at " + host.HostAddress);
            if (host.Enabled) {
                host.execute();
            } else {
                Lib.debug(host.HostName + " is disabled, testing connection only");
                host.testConnection();
            }
        }
        Lib.debug("Finished execution, leaving with exit code " + Constants.EXIT_SUCCESS);
        System.Environment.Exit(Constants.EXIT_SUCCESS);
    }

    /// <summary>
    /// Print a welcome message to the log. 
    /// </summary>
    private static void printWelcome() {
        string welcomeString = 
            Environment.NewLine
            + "###############################"
            + Environment.NewLine
            + "AutoRDrive has started"
            + Environment.NewLine
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
            throw e;
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
        Func<XName, bool> cond = 
            x => getConfigOption(x).ToLower().Equals(Constants.TRUE);

        DEBUG = cond(Constants.DEBUG);
        LOG = cond(Constants.LOGGING);
        NO_EXECUTE = cond(Constants.WHATIF);
        targetXML = readFileToXML(
            getConfigOption(Constants.TARGETFILEPATH) 
            + "\\"
            + getConfigOption(Constants.TARGETXMLFILENAME)
        );

        Lib.assertTrue(DEBUG != null, Constants.ERROR_CONFIG);
        Lib.assertTrue(LOG != null, Constants.ERROR_CONFIG);
        Lib.assertTrue(NO_EXECUTE != null, Constants.ERROR_CONFIG);
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
        DataTable dt = new DataTable();
        (new OleDbDataAdapter(Constants.QUERY, conn)).Fill(dt);

        foreach (DataRow dr in dt.Rows) {
            if (classesToTarget.Contains(dr[@"Class"].ToString().ToLower())) {
                remoteHostList.Add(
                    new RemoteHost {
                        HostAddress = dr["IP Address"].ToString()
                        ,HostName = dr["Net Name"].ToString().ToUpper()
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
        IEnumerable<XElement> typeList = targetXML.Descendants(Constants.CLASSES).Elements();
        Func<XElement, bool> condition =
            x => classesToTarget.Contains<string>(x.Element(Constants.CLASSNAME).Value.ToLower());

        foreach (XElement classTypes in typeList) {
            if (condition(classTypes)) {
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
