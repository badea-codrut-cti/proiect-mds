using proiect_mds.blockchain;
using proiect_mds.blockchain.impl;
using proiect_mds.daemon;
using proiect_mds.daemon.packets;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace proiect_mds.UnitTests
{
    [TestClass]
    public class DaemonTests
    {
        private static string ByteArrayToString(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2} ", b);
            return hex.ToString();
        }
        
        [TestMethod]
        public async Task ConnectToNode()
        {
            var memoryBlockIterator = new MemoryBlockIterator();
            var memoryWalletIterator = new MemoryWalletIterator();
            var blockchain = new Blockchain(memoryBlockIterator, memoryWalletIterator);
            var respHandler = new ResponseHandler(blockchain);
            var daemon = new Daemon(respHandler, 9005);
            daemon.StartAsync();

            var tcpClient = new TcpClient("localhost", 9005);
            var stream = tcpClient.GetStream();
            var helloPacket = new NodeHello(1, 9906, RequestType.SyncBlockchain);
            Serializer.SerializeWithLengthPrefix<NodeHello>(stream, helloPacket, PrefixStyle.Fixed32);
            var syncReqPacket = new SyncChainRequest(0, Hash.FromBlock());
            tcpClient.Close();
        }
    }
}
