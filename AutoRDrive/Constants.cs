using System.Xml.Linq;
namespace AutoBack {
    /// <summary>
    /// Global program constants and configuration parameters.
    /// </summary>
    static class Constants {

        // Configuration parameters
        public static string CONFIG_FILE = @"\\backups\computerimagingprimary\resources\bin\config.xml";
        public static string CONNECTION_CLASS = "win32_process";
        public static string WMI_ROOT = @"\root\cimv2";
        public static string TRUE = "true";
        public static string YES = "yes";
        public static string TEST = "test";
        public static string WORKSTATION = "workstation";
        public static string SERVER = "server";
        public static string OTHER = "other";
        public static string METHOD = "Create";
        public static string PLACEHOLDER = "IMAGE_PLACEHOLDER";
        
        // Valid log severity levels. 
        public static string LL_ERROR = "Error";
        public static string LL_WARNING = "Warning";
        public static string LL_INFO = "Information";

        // OLEDB-Related Constants
        public static string PROVIDER = @"Microsoft.Jet.OLEDB.4.0;Extended Properties=Excel 8.0";
        public static string QUERY = @"Select * from [Sheet1$A2:L]";

        // XName elements. These are used for XML parsing and must match XML tags. 
        public static XName SAVEDIRBASE = "SaveDirectoryBasePath";
        public static XName OPTIONS = "Options";
        public static XName EXECUTABLE_PATH = "ExecutablePath";
        public static XName EXECUTABLE_NAME = "ExecutableName";
        public static XName CLASS = "Class";
        public static XName CLASSNAME = "ClassName";
        public static XName HOST_NAME = "ComputerName";
        public static XName HOST = "Host";
        public static XName HOST_ADDRESS = "Address";
        public static XName ENABLED = "Enabled";
        public static XName CLASSES = "Classes";
        public static XName DEBUG = "Debug";
        public static XName USER = "User";
        public static XName LOGGING = "Logging";
        public static XName WHATIF = "Whatif";
        public static XName LOGFILE = "Logfile";
        public static XName LOGPATH = "Logpath";
        public static XName SAVEDIR = "Savedir";
        public static XName RDIMASTERPATH = "RdiMasterPath";
        public static XName RDIMASTER = "RdiMasterName";
        public static XName TARGETFILEPATH = "TargetsFilePath";
        public static XName TARGETFILENAME = "TargetsFileName";
        public static XName TARGETXMLFILENAME = "TargetsXMLFileName";

        // Valid program arguments
        public static string[] VALID_ARGUMENTS = {
            WORKSTATION, OTHER,TEST, SERVER
        };

        // Error messages
        public static string ERROR_FILE_PARSE = "Error reading file";
        public static string ERROR_ARGUMENTS = "Error invalid program arguments";
        public static string ERROR_ASSERT = "Error assertion failed";
        public static string ERROR_CONNECT = "Error connecting to target WMI";
        
        // Log messages
        public static string LOG_SUCCESS = "Successfully executed command";
        public static string LOG_FAIL = "Could not execute command";
    }
}