﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace SrinokanDreamsServer
{
    class Program
    {
        static void Main(string[] args)
        {
            AsynchronousSocketListener.StartListening();
        }
    }
}
