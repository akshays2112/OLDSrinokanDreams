using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Sockets;

namespace SrinokanDreams
{
    class MessageReceiver
    {
        public delegate void ReceivedData(string data);
        public List<ReceivedData> ReceiveRequests = new List<ReceivedData>();
        public bool readyToReceive { get; set; }
        public Thread receivingThread { get; set; }
        public string lastMsg = string.Empty;

        public MessageReceiver()
        {
            receivingThread = new Thread(ReceiveMessageLoop);
            receivingThread.Start();
            readyToReceive = true;
        }

        public void AskForReceive(ReceivedData rd)
        {
            ReceiveRequests.Add(rd);
        }

        public void ReceiveMessageLoop()
        {
            while (true)
            {
                if (Globals.ReceiveClientConnected && readyToReceive && ReceiveRequests.Count > 0)
                {
                    Receive();
                }
                //Thread.Sleep(1);
            }
        }

        public void Receive()
        {
            // Create the state object.
            readyToReceive = false;
            StateObject state = new StateObject();
            state.workSocket = Globals.ReceiveClient;

            // Begin receiving the data from the remote device.
            int bytesRead = Globals.ReceiveClient.Receive(state.buffer, 0, StateObject.BufferSize, 0);
            state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));

            if (state.sb.ToString().IndexOf("<EOF>") > -1)
            {
                if (ReceiveRequests.Count > 0)
                {
                    if (lastMsg.Length > 0)
                    {
                        ReceiveRequests[0](state.sb.ToString().Replace("<EOF>", "").Replace(lastMsg, ""));
                    }
                    else
                    {
                        ReceiveRequests[0](state.sb.ToString().Replace("<EOF>", ""));
                    }
                    ReceiveRequests.RemoveAt(0);
                    lastMsg = state.sb.ToString().Replace("<EOF>", "");
                }
                readyToReceive = true;
            }
        }
    }
}