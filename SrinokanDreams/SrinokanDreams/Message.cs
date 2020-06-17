using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SrinokanDreams
{
    class Message
    {
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

        public Guid PlayerID { get; set; }
        public MessageType MsgType { get; set; }
        public string Msg { get; set; }
        public delegate void MsgFunc();
        public string Response { get; set; }
    }
}
