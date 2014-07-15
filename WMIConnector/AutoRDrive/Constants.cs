/*
 * Constants.cs
 * 
 * Constants implementation.
 */
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
    public const string BU_FILE_WILD        = "*.arc";
    public const string BU_FILE_EXT         = ".arc";
    public const string PROC_NAME           = "AutoRDrive"; // Don't use 'exe'

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
    public static readonly XName IMAGE_LOG          = "ImageLog";


    //
    // Network constants
    public const byte   RESULT_OK   = 0x35;
    public const byte   RESULT_ERR  = 0x36;

} // End Constants class
} // End namespace
