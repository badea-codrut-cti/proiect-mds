using proiect_mds.daemon.packets;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace proiect_mds.daemon
{
    internal class Daemon
    {
        public int Port { get; private set; }
        private readonly ResponseHandler responseHandler;
        public Daemon(ResponseHandler responseHandler, int port = 9005)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(port);
            Port = port;
            this.responseHandler = responseHandler;
        }
        public async Task StartAsync()
        {
            using var listener = new TcpListener(IPAddress.Any, Port);
            listener.Start();

            while (true)
            {
                var client = await listener.AcceptTcpClientAsync();
                _ = Task.Run(async () => await responseHandler.HandleResponse(client));
            }
        }
    }
}
