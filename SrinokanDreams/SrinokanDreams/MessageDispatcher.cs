using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Threading;

namespace SrinokanDreams
{
    class MessageDispatcher
    {
        public List<Message> MessagesToSend { get; set; }
        public bool readyToSend { get; set; }
        public Thread sendingThread { get; set; }

        public MessageDispatcher()
        {
            MessagesToSend = new List<Message>();
            sendingThread = new Thread(SendMessageLoop);
            sendingThread.Start();
            readyToSend = true;
        }

        public void SendMessageLoop()
        {
            while(true)
            {
                if (Globals.SendClientConnected && Globals.ReceiveClientConnected && readyToSend && MessagesToSend.Count > 0)
                {
                    Send(Message.MessageType.Player.ToString() + ":" + MessagesToSend[0].PlayerID.ToString() + ";." + MessagesToSend[0].Msg + "<EOF>");
                    MessagesToSend.RemoveAt(0);
                }
                Thread.Sleep(1);
            }
        }

        public void SendMessage(string msg)
        {
            Message m = new Message();
            m.Msg = msg;
            m.PlayerID = Globals.ThisPlayerID;
            MessagesToSend.Add(m);
        }

        public void Send(String data)
        {
            readyToSend = false;
            // Convert the string data to byte data using ASCII encoding.
            byte[] byteData = Encoding.ASCII.GetBytes(data);

            // Begin sending the data to the remote device.
            Globals.SendClient.Send(byteData, 0, byteData.Length, 0);
            readyToSend = true;
        }
    }
}
