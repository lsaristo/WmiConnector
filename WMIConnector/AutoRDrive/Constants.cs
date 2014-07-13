using System.Xml.Linq;
using System;

namespace AutoBack 
{
/// <summary>
/// Global program constants and configuration parameters.
/// </summary>
static class Constants 
{
    //
    // Configuration parameters
    public const string CONFIG_FILE         = @"\\backups\computerimagingprimary\resources\bin\config.xml";
    public const string CONNECTION_CLASS    = "win32_process";
    public const string WMI_ROOT            = @"\root\cimv2";
    public const string TRUE                = "true";
    public const string YES                 = "yes";
    public const string TEST                = "test";
    public const string WORKSTATION         = "workstation";
    public const string SERVER              = "server";
    public const string OTHER               = "other";
    public const string METHOD              = "Create";
    public const string PLACEHOLDER         = "IMAGE_PLACEHOLDER";
    public const string FALLBACK_LOG        = "AutoRDrive_Fallback_Log";
    public const string DATE_FORMAT         = "yyyy_MM-dd";
    public const string BU_FILE_EXT         = "*.arc";
    public const string PROC_NAME           = "AutoRDrive"; // Don't use 'exe'
    
    //
    // WMI
    public const string WMI_QUERY = "Select csname from Win32_OperatingSystem";

    //
    // Valid log severity levels. 
    public const string LL_ERROR    = "Error";
    public const string LL_WARNING  = "Warning";
    public const string LL_INFO     = "Information";
    
    //
    // Database
    public const string PROVIDER    = @"Microsoft.Jet.OLEDB.4.0;Extended Properties=Excel 8.0";
    public const string QUERY       = @"Select * from [Sheet1$A2:L]";

    //
    // XName elements. These are used for XML parsing and must match XML tags. 
    public static readonly XName SAVEDIRBASE        = "SaveDirectoryBasePath";
    public static readonly XName HISTORY_COUNT      = "KeepMaxCount";
    public static readonly XName SERVER_PORT        = "ServerListenPort";
    public static readonly XName OPTIONS            = "Options";
    public static readonly XName EXECUTABLE_PATH    = "ExecutablePath";
    public static readonly XName EXECUTABLE_NAME    = "ExecutableName";
    public static readonly XName CLASS              = "Class";
    public static readonly XName CLASSNAME          = "ClassName";
    public static readonly XName HOST_NAME          = "ComputerName";
    public static readonly XName HOST               = "Host";
    public static readonly XName HOST_ADDRESS       = "Address";
    public static readonly XName ENABLED            = "Enabled";
    public static readonly XName CLASSES            = "Classes";
    public static readonly XName DEBUG              = "Debug";
    public static readonly XName USER               = "User";
    public static readonly XName LOGGING            = "Logging";
    public static readonly XName WHATIF             = "Whatif";
    public static readonly XName LOGFILE            = "Logfile";
    public static readonly XName LOGPATH            = "Logpath";
    public static readonly XName SAVEDIR            = "Savedir";
    public static readonly XName RDIMASTERPATH      = "RdiMasterPath";
    public static readonly XName RDIMASTER          = "RdiMasterName";
    public static readonly XName TARGETFILEPATH     = "TargetsFilePath";
    public static readonly XName TARGETFILENAME     = "TargetsFileName";
    public static readonly XName TARGETXMLFILENAME  = "TargetsXMLFileName";
    public static readonly XName CONCURRENT_LIMIT   = "ConcurrentLimit";

    //
    // Valid program arguments
    public static readonly string[] VALID_ARGUMENTS = {
        WORKSTATION, OTHER,TEST, SERVER
    };

    //
    // Error messages
    public const string ERROR_FILE_PARSE    = "Error reading file";
    public const string ERROR_ARGUMENTS     = "ERROR: invalid program arguments";
    public const string ERROR_ASSERT        = "Error assertion failed";
    public const string ERROR_CONNECT       = "Error connecting to target WMI";
    public const string ERROR_CONFIG        = "ERROR: Malformed config file";
    public const string ERROR_SENTINEL      = "ERROR: Something went wrong during sentinel execution";

    //
    // Log messages
    public const string LOG_SUCCESS     = "Successfully executed command";
    public const string LOG_FAIL        = "Could not execute command";
    public const string TEST_SUCCESS    = "Successfully connected to host";
    public const string TEST_FAIL       = "Error: Could not contact host";

    //
    // Informational messages
    public const string INFO_RUNNER_DONE        = "Runner thread has finished";
    public const string INFO_ALL_DONE           = "**************** DONE ******************";
    public const string INFO_WELCOME            = "Auto R-Drive Has Started";
    public const string INFO_HELP               = "Usage: AutoRDrive.exe [class1] [class2] ...\n";


    //
    // Warning messages
    public const string WARN_SERVER_LOCK    = "WARNING: Couldn't stop socket server. Forced stop.";

    //
    // Exit status
    public const int EXIT_FAILURE   = -1;
    public const int EXIT_SUCCESS   = 0;
    public const int FATAL_INIT     = -2;
    public const int FATAL_ARGS     = -3;
    public const int FATAL_CONFIG   = -4;
    public const int FATAL_TARGETS  = -5;

    //
    // Network constants
    public const int    BACKLOG     = 100;
    public const int    TIMEOUT     = 100 * 1000; // (milliseconds)
    public const int    BUFF_SIZE   = 1024;
    public const byte   RESULT_OK   = 0x35;
    public const byte   RESULT_ERR  = 0x36;
    public const String MSG_OK      = "success";
    public const String MSG_EOF     = "<EOF>";
    public const char   MSG_DELIM   = ':';

    //
    // Threading constants
    public const int RUNNER_TIME    = 10 * 60000;   // 10 minutes
    public const int SERVER_TIME    = 1 * 60000;    // 1 minute


} // End Constants class
} // End namespace
