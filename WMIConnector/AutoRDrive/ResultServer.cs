/*
 * ResultServer.cs
 * 
 * ResultServer implementation.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.Net.Sockets;

namespace AutoBack 
{

/// <summary>
/// The result server listens for TCP connections on a port as defined in 
/// configuration file for messages sent by RemoteHosts and acts accordingly. 
/// </summary>
class ResultServer 
{   
    //
    // Class networking constants.
    public const String MSG_OK      = "success";
    public const String MSG_EOF     = "<EOF>";
    public const char   MSG_DELIM   = ':';
    public const int    BACKLOG     = 100;
    public const int    REC_TIMEOUT = 20 * 1000; // 20 seconds
    public const int    BUFF_SIZE   = 1024;

    //
    // Static properties.
    public static ManualResetEvent reset;
 
    //
    // Instance properties.
    private Int32 port;
    private Int32 bufferSize;
    private Int32 backlog;
    private IPEndPoint localEndpoint;
    private Socket listener;
    private Byte[] bytes;
    private Boolean hasInit;

    private const Int32 DATA_IN_LIMIT = BUFF_SIZE;

    /// <summary>
    /// Constructor for the ResultServer
    /// </summary>
    /// <param name="backlog">Backlog as defined by .NET</param>
    public ResultServer(int backlog = BACKLOG) 
    {
        bufferSize = BUFF_SIZE;
        reset = new ManualResetEvent(false); 
        port = Convert.ToInt32(Driver.getConfigOption(Constants.SERVER_PORT));
        localEndpoint = new IPEndPoint(IPAddress.Any, port);
        listener = new Socket(
            AddressFamily.InterNetwork,
            SocketType.Stream,
            ProtocolType.Tcp
        );
        this.backlog = backlog;
        hasInit = false;
    }

    /// <summary>
    /// Initialize this socket server.
    /// </summary>
    /// <returns>True if Socket successfully bound and listening.</returns>
    public Boolean init()
    {
        try {
            listener.Bind(localEndpoint);
            listener.Listen(backlog);
            listener.ReceiveTimeout = REC_TIMEOUT;
            hasInit = true;
            return true;
        } catch(SocketException se) {
            Lib.log("ERROR: Socket error " + se.ErrorCode);
            return false;
        } catch(Exception e) {
            Lib.logException(e);
            return false;
        }
    }

    /// <summary>
    /// Public interface for use with ThreadStart class.
    /// </summary>
    public void runServer()
    {
        if(!hasInit && !init()) {
            Lib.log("ERROR: SERVER FAILED TO START");
            return;
        }
        if(!begin()) { Lib.log("WARNING: Server exited unexpectedly"); }
    }

    /// <summary>
    /// Start this server and enter a listening loop for connections until 
    /// stop() is called.
    /// </summary>  
    private Boolean begin()
    {
        String log1 = "Starting result server";
        String log2 = "ERROR: You must first init() this server before running.";
        String log3 = "Accept threw a socket exception. This might be normal";
        String log4 = "Server listening on port " + port;
        String log5 = "Server received TCP connection";
        String log6 = "Got EOF, closing socket";
        String log7 = "WARNING: Server ignored malformed data received";
        String log8 = 
            "WARNING: Caught general exception in Socket Server. This is not"
            + "normal";
        String log9 = "Trying to close and re-initialize listener...";
        String log10 = "ERROR: Couldn't restart listener. Aborting execution";

        Lib.debug(log1);
        if(!hasInit) {
            Lib.log(log2);
            return false;
        }

        while (true) {
            int totalDataIn = 0;
            int bytesRec = 0;
            String inData = "";
            Lib.debug(log4);
            Socket handler;

            try {
                handler = listener.Accept();
            } catch (SocketException) {
                Lib.debug(log3);
                break;
            } catch (Exception e) {
                Lib.log(log8);
                Lib.logException(e);
                Lib.debug(log9);
                if (!init()) {
                    Lib.log(log10);
                    return false;
                }
                Lib.log("Listener has re-established binding");
                continue;
            }

            Lib.debug(log5);

            while ((totalDataIn += bytesRec) < DATA_IN_LIMIT) {
                bytes = new byte[bufferSize];
                bytesRec = handler.Receive(bytes);
                inData += Encoding.Unicode.GetString(bytes,0,bytesRec);
                if(inData.IndexOf(MSG_EOF) > -1) {
                    Lib.debug(log6);
                    break; 
                }
            }

            handler.Shutdown(SocketShutdown.Both);
            handler.Close();
            if(!processReceivedData(inData)) { Lib.log(log7); }
        }
        return true;
    }

    /// <summary>
    /// Handle data that came in from a socket. 
    /// </summary>
    /// <param name="data">String of data</param>
    /// <returns>True if data format was acceptable.</returns>
    private Boolean processReceivedData(String data)
    {
        Lib.debug("Received connection with string " + data);

        String[] responseArray = data.Split(MSG_DELIM);

        if(!(responseArray.Length == 2)) { return false; }

        String responseHost = responseArray[0];
        String responseResult = responseArray[1];

        Byte msg = 
            (responseResult.ToLower().Contains(MSG_OK))
            ? Constants.RESULT_OK
            : Constants.RESULT_ERR;
        Driver.handleMsg(responseHost, msg);
        return true;
    }

    /// <summary>
    /// Shutdown this server within at most 1 TIMEOUT period. 
    /// </summary>
    /// <see cref="Constants.TIMEOUT"/>
    public void stop()
    {
        listener.Close(REC_TIMEOUT);
    }
} // End ResultServer class
} // End AutoBack namespace
