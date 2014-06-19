using System.Xml.Linq;

namespace AutoBack 
{
/// <summary>
/// Global program constants and configuration parameters.
/// </summary>
const class Constants 
{
    // Configuration parameters
    public const string CONFIG_FILE = @"\\backups\computerimagingprimary\resources\bin\config.xml";
    public const string CONNECTION_CLASS = "win32_process";
    public const string WMI_ROOT = @"\root\cimv2";
    public const string TRUE = "true";
    public const string YES = "yes";
    public const string TEST = "test";
    public const string WORKSTATION = "workstation";
    public const string SERVER = "server";
    public const string OTHER = "other";
    public const string METHOD = "Create";
    public const string PLACEHOLDER = "IMAGE_PLACEHOLDER";
    public const string FALLBACK_LOG = "AutoRDrive_Fallback_Log";
    
    // Valid log severity levels. 
    public const string LL_ERROR = "Error";
    public const string LL_WARNING = "Warning";
    public const string LL_INFO = "Information";

    // OLEDB-Related Constants
    public const string PROVIDER = @"Microsoft.Jet.OLEDB.4.0;Extended Properties=Excel 8.0";
    public const string QUERY = @"Select * from [Sheet1$A2:L]";

    // XName elements. These are used for XML parsing and must match XML tags. 
    public const XName SAVEDIRBASE = "SaveDirectoryBasePath";
    public const XName OPTIONS = "Options";
    public const XName EXECUTABLE_PATH = "ExecutablePath";
    public const XName EXECUTABLE_NAME = "ExecutableName";
    public const XName CLASS = "Class";
    public const XName CLASSNAME = "ClassName";
    public const XName HOST_NAME = "ComputerName";
    public const XName HOST = "Host";
    public const XName HOST_ADDRESS = "Address";
    public const XName ENABLED = "Enabled";
    public const XName CLASSES = "Classes";
    public const XName DEBUG = "Debug";
    public const XName USER = "User";
    public const XName LOGGING = "Logging";
    public const XName WHATIF = "Whatif";
    public const XName LOGFILE = "Logfile";
    public const XName LOGPATH = "Logpath";
    public const XName SAVEDIR = "Savedir";
    public const XName RDIMASTERPATH = "RdiMasterPath";
    public const XName RDIMASTER = "RdiMasterName";
    public const XName TARGETFILEPATH = "TargetsFilePath";
    public const XName TARGETFILENAME = "TargetsFileName";
    public const XName TARGETXMLFILENAME = "TargetsXMLFileName";

    // Valid program arguments
    public const string[] VALID_ARGUMENTS = {
        WORKSTATION, OTHER,TEST, SERVER
    };

    // Error messages
    public const string ERROR_FILE_PARSE = "Error reading file";
    public const string ERROR_ARGUMENTS = "FATAL ERROR: invalid program arguments";
    public const string ERROR_ASSERT = "Error assertion failed";
    public const string ERROR_CONNECT = "Error connecting to target WMI";
    public const string ERROR_CONFIG = "FATAL ERROR: Malformed config file";
    
    // Log messages
    public const string LOG_SUCCESS = "Successfully executed command";
    public const string LOG_FAIL = "Could not execute command";
    public const string TEST_SUCCESS = "Successfully connected to host";
    public const string TEST_FAIL = "Error: Could not contact host";

    // Exit status
    public const int EXIT_FAILURE = -1;
    public const int EXIT_SUCCESS = 0;
} // End Constants class
} // End namespace
