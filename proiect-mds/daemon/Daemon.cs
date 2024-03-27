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
                    _ = Task.Run(async () => await HandleClientAsync(client));
                }
            }
        }
        private async Task HandleClientAsync(TcpClient client)
        {
            try
            {
                using var networkStream = client.GetStream();

                byte[] lengthBytes = new byte[4];
                await networkStream.ReadAsync(lengthBytes.AsMemory(0, 4));
                var messageLength = BitConverter.ToInt32(lengthBytes, 0);

                byte[] messageBytes = new byte[messageLength];
                await networkStream.ReadAsync(messageBytes.AsMemory(0, messageLength));

                var message = DecodeMessage<NodeHello>(messageBytes);

                if (message == null)
                {
                    var response = new NodeWelcome(HelloResponseCode.BadRequest);
                    Serializer.SerializeWithLengthPrefix<NodeWelcome>(networkStream, response, PrefixStyle.Fixed32);
                    client.Close();
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
        public static T DecodeMessage<T>(byte[] data) where T : class
        {
            if (data == null || data.Length == 0)
            {
                throw new ArgumentException("Invalid message data");
            }

            using (var ms = new MemoryStream(data))
            {
                return Serializer.Deserialize<T>(ms);
            }
        }
    }
}
