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

class ResultServer 
{
    Int32 port;
    Int32 bufferSize = 1024;
    IPEndPoint localEndpoint;
    Socket listener;
    int backlog;
    byte[] bytes;
    bool alive;
    public static ManualResetEvent reset = new ManualResetEvent(false); 

    public ResultServer(int backlog = Constants.BACKLOG) 
    {
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

    public void runServer()
    {
        try {
            listener.Bind(localEndpoint);
            listener.Listen(backlog);
            listener.ReceiveTimeout = Constants.TIMEOUT;

            while(alive) {
                try {
                    String inData = "";
                    Lib.debug("Waiting for connections to " + port);
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

                    if (Driver.currentRunners.ContainsKey(responseHost)) {
                        String msg = 
                            responseHost + " reports " + responseResult
                            + ". Removing from array of runners";
                        Lib.debug(msg);
                        lock(Driver.runnerLock) {
                            if (!Driver.currentRunners.Remove(responseHost)) {
                                Lib.log("WARNING: {0} not found in runner list", responseHost);
                            }
                            Driver.runnerPhore.Release();
                        }
                    } else {
                        String msg = 
                            "Warning, " + responseHost + " reported " + responseResult
                            + ". This host is not present in the array of current"
                            + " runners. This should be dealt with.";
                        Lib.log(msg);
                    }
                    if (!responseResult.ToLower().Contains("success")) {
                        lock (Driver.runnerLock) {
                            Driver.failedRunners.Add(responseHost);
                        }
                    } else {
                        Lib.log(responseHost + " reported successful backup");
                    }
                } catch(Exception e) { 
                    Lib.debug(e.Message);
                }
            }
      } catch(Exception e) {
            Lib.logException(e);
        }
    }

    public void stop()
    {
        alive = false;
    }
} // End ResultServer class
} // End AutoBack namespace