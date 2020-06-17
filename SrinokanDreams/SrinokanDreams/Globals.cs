using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Threading;
using System.Net;

namespace SrinokanDreams
{
    static class Globals
    {
        public static Guid ThisPlayerID { get; set; }
        public static Player ThisPlayer {get;set;}
        public static List<Player> OtherPlayers = new List<Player>();

        public const int MaxNumberOfAnimationsPerModel = 6;
        public const int AttackAction = 0;
        public const int CastAction = 1;
        public const int DeathAction = 2;
        public const int HoldAction = 3;
        public const int StunAction = 4;
        public const int WalkAction = 5;
        public const int RunAction = 6;
        public static string[] ActionNames = new string[] { "Attack", "Cast", "Death", "Hold", "Stun", "Walk", "Run" };
        public const int HPBarTexture = 6;

        public static Socket SendClient { get; set; }
        public static Socket ReceiveClient { get; set; }

        public static bool SendClientConnected = false;
        public static bool ReceiveClientConnected = false;

        public static MessageDispatcher MsgDispatcher { get; set; }
        public static MessageReceiver MsgReceiver { get; set; }

        public static void StartSendClient()
        {
            // Connect to a remote device.
            // Establish the remote endpoint for the socket.
            // The name of the 
            // remote device is "host.contoso.com".
            int sendPort = 2112;
            IPHostEntry ipHostInfo = Dns.Resolve("gilgamesh");
            IPAddress ipAddress = ipHostInfo.AddressList[0];
            IPEndPoint remoteEP = new IPEndPoint(ipAddress, sendPort);

            // Create a TCP/IP socket.
            SendClient = new Socket(AddressFamily.InterNetwork,
                SocketType.Stream, ProtocolType.Tcp);

            // Connect to the remote endpoint.
            SendClient.BeginConnect(remoteEP,
                new AsyncCallback(SendConnectCallback), SendClient);
        }

        public static void CloseSendClient()
        {
            // Release the socket.
            SendClient.Shutdown(SocketShutdown.Both);
            SendClient.Close();
            SendClientConnected = false;
        }

        private static void SendConnectCallback(IAsyncResult ar)
        {
            try
            {
                SendClientConnected = true;
                Socket client = (Socket)ar.AsyncState;

                // Complete the connection.
                client.EndConnect(ar);
            }
            catch (Exception e)
            {
                //Console.WriteLine(e.ToString());
            }
        }

        public static void StartReceiveClient()
        {
            // Connect to a remote device.
            try
            {
                // Establish the remote endpoint for the socket.
                // The name of the 
                // remote device is "host.contoso.com".
                int sendPort = 2112;
                IPHostEntry ipHostInfo = Dns.Resolve("gilgamesh");
                IPAddress ipAddress = ipHostInfo.AddressList[0];
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, sendPort);

                // Create a TCP/IP socket.
                ReceiveClient = new Socket(AddressFamily.InterNetwork,
                    SocketType.Stream, ProtocolType.Tcp);

                // Connect to the remote endpoint.
                ReceiveClient.BeginConnect(remoteEP,
                    new AsyncCallback(ReceiveConnectCallback), ReceiveClient);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public static void CloseReceiveClient()
        {
            // Release the socket.
            ReceiveClient.Shutdown(SocketShutdown.Both);
            ReceiveClient.Close();
            ReceiveClientConnected = false;
        }

        private static void ReceiveConnectCallback(IAsyncResult ar)
        {
            try
            {
                Socket client = (Socket)ar.AsyncState;

                // Complete the connection.
                client.EndConnect(ar);
                ReceiveClientConnected = true;
                Send("ReceiveSocket:" + Globals.ThisPlayerID.ToString() + ";.<EOF>");
            }
            catch (Exception e)
            {
                //Console.WriteLine(e.ToString());
            }
        }

        public static void Send(String data)
        {
            // Convert the string data to byte data using ASCII encoding.
            byte[] byteData = Encoding.ASCII.GetBytes(data);

            // Begin sending the data to the remote device.
            Globals.ReceiveClient.Send(byteData, 0, byteData.Length, 0);
        }
    }
}
