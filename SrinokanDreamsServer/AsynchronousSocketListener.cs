using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Xml.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace SrinokanDreamsServer
{
    class AsynchronousSocketListener
    {
        public static Player currentplayer;
        public static ManualResetEvent allDone = new ManualResetEvent(false);
        public static Dictionary<Guid, Player> Players = new Dictionary<Guid, Player>();
        public static bool doOnce = false;
        public static bool doreceiveonce = false;

        public class Player
        {
            public Guid PlayerID { get; set; }
            public Socket SendSocket { get; set; }
            public Socket ReceiveSocket { get; set; }
            public float PlayerPositionX { get; set; }
            public float PlayerPositionY { get; set; }
            public float PlayerPositionZ { get; set; }
            public float PlayerRotation { get; set; }
            public float PlayerVelocityX { get; set; }
            public float PlayerVelocityY { get; set; }
            public float PlayerVelocityZ { get; set; }
        }

        public enum MessageType
        {
            Start,
            NPC,
            Player,
            PlayerPosition,
            PlayerRotation,
            PlayerVelocity,
            FlushSocket
        }

        public AsynchronousSocketListener()
        {
        }

        public static void StartListening()
        {
            // Data buffer for incoming data.
            byte[] bytes = new Byte[1024];

            // Establish the local endpoint for the socket.
            // The DNS name of the computer
            // running the listener is "host.contoso.com".
            IPHostEntry ipHostInfo = Dns.Resolve(Dns.GetHostName());
            IPAddress ipAddress = ipHostInfo.AddressList[0];
            IPEndPoint localEndPointSend = new IPEndPoint(ipAddress, 2112);

            // Create a TCP/IP socket.
            Socket listener = new Socket(AddressFamily.InterNetwork,
                SocketType.Stream, ProtocolType.Tcp);

            // Bind the socket to the local endpoint and listen for incoming connections.
            listener.Bind(localEndPointSend);
            listener.Listen(100);

            while (true)
            {
                // Set the event to nonsignaled state.
                allDone.Reset();

                // Start an asynchronous socket to listen for connections.
                Console.WriteLine("Waiting for a connection...");
                listener.BeginAccept(
                    new AsyncCallback(AcceptCallback),
                    listener);

                // Wait until a connection is made before continuing.
                allDone.WaitOne();
            }


            Console.WriteLine("\nPress ENTER to continue...");
            Console.Read();

        }

        public static void AcceptCallback(IAsyncResult ar)
        {
            // Signal the main thread to continue.
            allDone.Set();

            // Get the socket that handles the client request.
            Socket listener = (Socket)ar.AsyncState;
            Socket handler = listener.EndAccept(ar);

            // Create the state object.
            StateObject state = new StateObject();
            state.workSocket = handler;

            Thread t = new Thread(() => StartThread(state, handler));
            t.Start();
        }

        public static void StartThread(StateObject state, Socket handler)
        {
            int bytesRead = handler.Receive(state.buffer, 0, StateObject.BufferSize, 0);
            ParseMessage(bytesRead, state, handler);
        }

        public static void ParseMessage(int bytesRead, StateObject state, Socket handler) {
            String content = String.Empty;
            if (bytesRead > 0)
            {
                // There  might be more data, so store the data received so far.
                state.sb.Append(Encoding.ASCII.GetString(
                    state.buffer, 0, bytesRead));

                // Check for end-of-file tag. If it is not there, read 
                // more data.
                content = state.sb.ToString();
                List<string> EOFs = new List<string>();
                if (content.IndexOf("<EOF>") > -1 && content.IndexOf("<EOF>") != content.LastIndexOf("<EOF>"))
                {
                    EOFs = content.Split(new string[] { "<EOF>" }, StringSplitOptions.None).ToList<string>();
                }
                else
                {
                    EOFs.Add(content.Replace("<EOF>", ""));
                }
                //EOFs.RemoveAt(EOFs.Count - 1);
                foreach (string eof in EOFs)
                {
                    if (eof.StartsWith("ReceiveSocket:") && !doOnce)
                    {
                        doOnce = true;
                        Guid player = new Guid(eof.Replace("ReceiveSocket:", "").Replace(";.", ""));
                        bool foundPlayer = false;
                        foreach (Guid key in Players.Keys)
                        {
                            if (key.Equals(player))
                            {
                                foundPlayer = true;
                                currentplayer = Players[key];
                            }
                        }
                        if (!foundPlayer)
                        {
                            currentplayer = new Player();
                            currentplayer.PlayerID = player;
                            Players.Add(player, currentplayer);
                        }
                        currentplayer.ReceiveSocket = handler;
                        int x = currentplayer.ReceiveSocket.Receive(state.buffer, 0, StateObject.BufferSize, 0);
                        ParseMessage(x, state, currentplayer.ReceiveSocket);
                    }
                    else if (eof.StartsWith("ReceiveSocket:"))
                    {

                    }
                    else
                    {
                        // All the data has been read from the 
                        // client. Display it on the console.
                        Console.WriteLine("Read {0} bytes from socket. \n Data : {1}",
                            eof.Length, eof);
                        // Echo the data back to the client.
                        //Message format is . seperating groups and : seperating group label from values and ; seperating values
                        string[] msgs = eof.Split(new char[] { '.' });
                        foreach (string msg in msgs)
                        {
                            string[] submsgs = msg.Split(new char[] { ':' });
                            if (submsgs.Length > 1)
                            {
                                switch ((MessageType)Enum.Parse(typeof(MessageType), submsgs[0]))
                                {
                                    case MessageType.Start:
                                        string str = string.Empty;
                                        var doc = XDocument.Load(@"Areas\Start.xml");
                                        foreach (XElement d in doc.Root.Element("Asset").Elements())
                                        {
                                            switch (d.Name.ToString())
                                            {
                                                case "Model":
                                                    str += "0:" + d.Value + ";";
                                                    break;
                                            }
                                        }
                                        if (str.Length > 0)
                                        {
                                            Send(currentplayer.ReceiveSocket, str + "<EOF>");
                                        }
                                        else
                                        {
                                            str = @"ERROR:Areas\Start.xml : XML file has no known tags in it.<EOF>";
                                        }
                                        break;
                                    case MessageType.NPC:
                                        var doc2 = XElement.Load(@"NPC\" + submsgs[1].Replace(";", "") + ".xml");
                                        string response = string.Empty;
                                        response += doc2.Element("Asset").Value;
                                        if (response.Length > 0)
                                            Send(currentplayer.ReceiveSocket, response.Substring(0, response.Length - 1) + "<EOF>");
                                        else
                                        {
                                            response = @"ERROR:NPC\" + submsgs[1].Replace(";", "") + ".xml : XML file has no known tags in it.<EOF>";
                                            Send(currentplayer.ReceiveSocket, response);
                                        }
                                        break;
                                    case MessageType.Player:
                                        bool foundPlayer = false;
                                        Guid player = new Guid(submsgs[1].Replace(";", ""));
                                        foreach (Guid key in Players.Keys)
                                        {
                                            if (key.Equals(player))
                                            {
                                                foundPlayer = true;
                                                currentplayer = Players[key];
                                            }
                                        }
                                        if (!foundPlayer)
                                        {
                                            currentplayer = new Player();
                                            currentplayer.PlayerID = player;
                                            Players.Add(player, currentplayer);
                                        }
                                        currentplayer.SendSocket = handler;
                                        break;
                                    case MessageType.PlayerPosition:
                                        string[] values = submsgs[1].Split(new char[] { ';' });
                                        if (values.Length == 3)
                                        {
                                            Players[currentplayer.PlayerID].PlayerPositionX = float.Parse(values[0]);
                                            Players[currentplayer.PlayerID].PlayerPositionY = float.Parse(values[1]);
                                            Players[currentplayer.PlayerID].PlayerPositionZ = float.Parse(values[2]);
                                        }
                                        break;
                                    case MessageType.PlayerRotation:
                                        Players[currentplayer.PlayerID].PlayerRotation = float.Parse(submsgs[1].Replace(";", ""));
                                        break;
                                    case MessageType.PlayerVelocity:
                                        string[] values1 = submsgs[1].Split(new char[] { ';' });
                                        if (values1.Length == 3)
                                        {
                                            Players[currentplayer.PlayerID].PlayerVelocityX = float.Parse(values1[0]);
                                            Players[currentplayer.PlayerID].PlayerVelocityY = float.Parse(values1[1]);
                                            Players[currentplayer.PlayerID].PlayerVelocityZ = float.Parse(values1[2]);
                                        }
                                        break;
                                }
                            }
                        }
                        foreach (Guid p1 in Players.Keys)
                        {
                            if (!p1.Equals(currentplayer.PlayerID))
                            {
                                Send(Players[p1].ReceiveSocket, eof);
                            }
                        }
                    }
                }
            }
            while (!doreceiveonce && handler == currentplayer.SendSocket)
            {
                doreceiveonce = true;
                int y = currentplayer.SendSocket.Receive(state.buffer, 0, StateObject.BufferSize, 0);
                ParseMessage(y, state, currentplayer.SendSocket);
            }
        }

        public static void ReadCallback(IAsyncResult ar)
        {

            // Retrieve the state object and the handler socket
            // from the asynchronous state object.
            StateObject state = (StateObject)ar.AsyncState;
            Socket handler = state.workSocket;

            // Read data from the client socket. 
            int bytesRead = handler.EndReceive(ar);

        }

        private static void Send(Socket handler, String data)
        {
            // Convert the string data to byte data using ASCII encoding.
            byte[] byteData = Encoding.ASCII.GetBytes(data);
            Console.WriteLine("Sending data: {0} to client.", data);
            // Begin sending the data to the remote device.
            handler.Send(byteData, 0, byteData.Length, 0);
        }
    }
}