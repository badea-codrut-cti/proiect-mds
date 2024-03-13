using System;
using System.Collections.Generic;
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

        public Daemon(int port=9005)
        {
            if (port < 0)
                throw new ArgumentOutOfRangeException("Listening port cannot be negative.");
            Port = port;
        }
        public async Task StartAsync()
        {
            using (var listener = new TcpListener(IPAddress.Any, (int)Port))
            {
                listener.Start();

                while (true)
                {
                    var client = await listener.AcceptTcpClientAsync();
                    Console.WriteLine("Client connected");

                    _ = Task.Run(async () => await HandleClientAsync(client));
                }
            }
        }
        private async Task HandleClientAsync(TcpClient client)
        {
            try
            {
                using (var networkStream = client.GetStream())
                {
                    byte[] lengthBytes = new byte[4];
                    await networkStream.ReadAsync(lengthBytes, 0, 4);
                    int messageLength = BitConverter.ToInt32(lengthBytes, 0);

                    // Read message payload
                    byte[] messagePayload = new byte[messageLength];
                    await networkStream.ReadAsync(messagePayload, 0, messageLength);

                    
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling client: {ex.Message}");
            }
            finally
            {
                client.Close();
            }
        }
    }
}
