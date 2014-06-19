﻿using System.Xml.Linq;

namespace AutoBack 
{
/// <summary>
/// Global program constants and configuration parameters.
/// </summary>
static class Constants 
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
