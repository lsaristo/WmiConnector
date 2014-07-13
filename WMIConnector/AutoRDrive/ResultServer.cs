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
    public static       ManualResetEvent    reset;
    
    private Int32       port;
    private Int32       bufferSize;
    private Int32       backlog;
    private IPEndPoint  localEndpoint;
    private Socket      listener;
    private Byte[]      bytes;
    private Boolean     alive;

    /// <summary>
    /// Constructor for the ResultServer
    /// </summary>
    /// <param name="backlog">Backlog as defined by .NET</param>
    /// <see cref="Constants.BACKLOG"/>
    public ResultServer(int backlog = Constants.BACKLOG) 
    {
        bufferSize = Constants.BUFF_SIZE;
        reset = new ManualResetEvent(false); 
        port = Convert.ToInt32(Driver.getConfigOption(Constants.SERVER_PORT));
        localEndpoint = new IPEndPoint(IPAddress.Any, port);
        listener = new Socket(
            AddressFamily.InterNetwork,
            SocketType.Stream,
            ProtocolType.Tcp
        );
        this.backlog = backlog;
        alive = true;
    }

    /// <summary>
    /// Start this server and enter a listening loop for connections until 
    /// stop() is called.
    /// </summary>  
    public void runServer()
    {
        String log1 = "Starting result server";
        String log2 = "Caught exception inside server loop...";
        String log3 = "ERROR: Caught exception in runServer()...";
        String log4 = "Server listening on port " + port;
        String log5 = "Server received TCP connection";
        String log6 = "Got EOF, closing socket";

        Lib.debug(log1);

        try {
            listener.Bind(localEndpoint);
            listener.Listen(backlog);
            listener.ReceiveTimeout = Constants.REC_TIMEOUT;

            while(alive) {
                try {
                    String inData = "";
                    Lib.debug(log4);
                    Socket handler = listener.Accept();
                    Lib.debug(log5);

                    while(true) {
                        bytes = new byte[bufferSize];
                        int bytesRec = handler.Receive(bytes);
                        inData += Encoding.Unicode.GetString(bytes,0,bytesRec);
                        if(inData.IndexOf(Constants.MSG_EOF) > -1) {
                            Lib.debug(log6);
                            break; 
                        }
                    }
                    handler.Shutdown(SocketShutdown.Both);
                    handler.Close();
                    Lib.debug("Received connection with string " + inData);

                    String[] responseArray = inData.Split(Constants.MSG_DELIM);
                    String responseHost = responseArray[0];
                    String responseResult = responseArray[1];

                    Byte msg = 
                        (responseResult.ToLower().Contains(Constants.MSG_OK))
                        ? Constants.RESULT_OK
                        : Constants.RESULT_ERR;
                    Driver.handleMsg(responseHost, msg);
                   
                } catch(Exception e) { 
                    Lib.debug(log2);
                    Lib.debug(e.Message);
                }
            }
      } catch(Exception e) {
            Lib.debug(log3);
            Lib.logException(e);
        }
    }

    /// <summary>
    /// Shutdown this server within at most 1 TIMEOUT period. 
    /// </summary>
    /// <see cref="Constants.TIMEOUT"/>
    public void stop()
    {
        listener.Close(Constants.REC_TIMEOUT);
        alive = false;
    }
} // End ResultServer class
} // End AutoBack namespace
