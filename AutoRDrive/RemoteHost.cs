using System;
using System.Management;
using System.IO;
using System.Data;
using System.Data.OleDb;

namespace AutoBack {
    /// <summary>
    /// Container class for a remote host computer. Each host listed in targets.xml
    /// will be represented by an instantiation of this class.
    /// </summary>
    class RemoteHost {
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
        /// Connect to the remote host and execute the command as specified in config.xml
        /// </summary>
        public bool execute() {
            makeSaveDirectory();
            generateRdi();
            ConnectionOptions opts = new ConnectionOptions();

            Scope = new ManagementScope("\\\\" + HostAddress + Constants.WMI_ROOT, opts);
            try {
                Scope.Connect();
            } catch (Exception e) {
                Lib.log(Constants.LL_ERROR, "Error at " + HostName + ": " + e.Message);
                return false ;
            }

            ConnectionClass =
                new ManagementClass(Scope, new ManagementPath(Constants.CONNECTION_CLASS), null);
            ProgramArgs = ConnectionClass.GetMethodParameters(Constants.METHOD);
            ProgramArgs["CommandLine"] = ArgsSetter;
            if (Enabled) {
                ConnectionClass.InvokeMethod(Constants.METHOD, ProgramArgs, null);
                Lib.log(Constants.LL_INFO, Constants.LOG_SUCCESS + ". Host: " + HostName);
            } else {
                Lib.log(Constants.LL_INFO, "Connected to host but Enabled was false" + ". Host: " + HostName);
            }
            return true;
        }

        /// <summary>
        /// Generate the custom RDI file from this remote host.
        /// </summary>
        public void generateRdi() {
            string fileText =
                File.ReadAllText(
                    Driver.getConfigOption(Constants.RDIMASTERPATH) + "\\" +
                    Driver.getConfigOption(Constants.RDIMASTER));
            string outputFile = SaveDir + "\\" + HostName + "_" + DateTime.Now.ToString("yyyy_MM-dd");
            string replacementText = fileText.Replace(Constants.PLACEHOLDER, outputFile);
            File.WriteAllText(RdiFile, replacementText);
        }

        /// <summary>
        /// Create the destination directory if it doesn't already exist. 
        /// </summary>
        private void makeSaveDirectory() {
            string dest = SaveDir + "\\" + HostName + " - " + PrimaryUser;
            SaveDir = dest;
            if (!System.IO.Directory.Exists(dest))
                System.IO.Directory.CreateDirectory(dest);
        }
    }
}