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
        try {
            listener.Bind(localEndpoint);
            listener.Listen(backlog);
            listener.ReceiveTimeout = Constants.TIMEOUT;

            while(alive) {
                try {
                    String inData = "";
                    Lib.debug("Listening on port " + port);
                    Socket handler = listener.Accept();
                    Lib.debug("Got connection, Handling");

                    while(true) {
                        bytes = new byte[bufferSize];
                        int bytesRec = handler.Receive(bytes);
                        inData += Encoding.Unicode.GetString(bytes,0,bytesRec);
                        if(inData.IndexOf("<EOF>") > -1) {
                            Lib.debug("Got EOF, closing socket");
                            break; 
                        }
                    }
                    handler.Shutdown(SocketShutdown.Both);
                    handler.Close();
                    Lib.debug("Received connection with string " + inData);

                    String[] responseArray = inData.Split(':');
                    String responseHost = responseArray[0];
                    String responseResult = responseArray[1];

                    Byte msg = 
                        (responseResult.ToLower().Contains("Success"))
                        ? Constants.RESULT_OK
                        : Constants.RESULT_ERR;
                    Driver.handleMsg(responseHost, msg);
                   
                } catch(Exception e) { 
                    Lib.debug("Caught exception inside Server loop");
                    Lib.debug(e.Message);
                }
            }
      } catch(Exception e) {
            Lib.logException(e);
        }
    }

    /// <summary>
    /// Shutdown this server within at most 1 TIMEOUT period. 
    /// </summary>
    /// <see cref="Constants.TIMEOUT"/>
    public void stop()
    {
        alive = false;
    }
} // End ResultServer class
} // End AutoBack namespace
