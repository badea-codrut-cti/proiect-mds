using proiect_mds.blockchain;
using proiect_mds.blockchain.impl;
using proiect_mds.daemon;
using proiect_mds.daemon.client;
using proiect_mds.daemon.packets;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace proiect_mds.UnitTests
{
    [TestClass]
    public class DaemonTests
    {
        [TestMethod]
        public async Task ConnectToNode()
        {
            var memoryBlockIterator = new MemoryBlockIterator();
            var memoryWalletIterator = new MemoryWalletIterator();
            var blockchain = new Blockchain(memoryBlockIterator, memoryWalletIterator);
            var daemon = new Daemon(blockchain, 9005);
            daemon.StartAsync();

            var tcpClient = new TcpClient("localhost", 9005);
            var stream = tcpClient.GetStream();
            var helloPacket = new NodeHello(1, 9906, RequestType.SyncBlockchain);
            Serializer.SerializeWithLengthPrefix<NodeHello>(stream, helloPacket, PrefixStyle.Fixed32);
            var syncReqPacket = new SyncChainRequest(0, Hash.FromBlock(Block.GenesisBlock()));
            Serializer.SerializeWithLengthPrefix<SyncChainRequest>(stream, syncReqPacket, PrefixStyle.Fixed32);
            tcpClient.Close();
            daemon.Stop();
        }

        [TestMethod]
        public async Task BroadcastTransaction()
        {
            var senderId = new WalletId(Encoding.UTF8.GetBytes("0123456789ABCDEF"));
            var receiverId = new WalletId(Encoding.UTF8.GetBytes("123456789ABCDEFG"));

            var key = new PrivateKey(KeyBasedSignChecks.privateKeyString);
            var transaction = key.SignTransaction(senderId, receiverId, 100, DateTime.Now);
            Assert.IsNotNull(transaction);

            var nodeInfo = new NodeAddressInfo(BitConverter.ToUInt32([127, 0, 0, 1]), 9005);

            var memoryBlockIterator = new MemoryBlockIterator();
            var memoryWalletIterator = new MemoryWalletIterator();
            var blockchain = new Blockchain(memoryBlockIterator, memoryWalletIterator);
            var daemon = new Daemon(blockchain, 9005);
            daemon.StartAsync();

            NodeConnection.BroadcastTransaction([nodeInfo], 9091, transaction);
            Assert.IsTrue(daemon.TransactionsQueued.Count == 1);

            daemon.Stop();
        }

        private static List<NodeAddressInfo> GeneratePeers(int count)
        {
            var peers = new List<NodeAddressInfo>(count);
            while (count > 0)
            {
                byte[] ipBytes = new byte[4];
                new Random().NextBytes(ipBytes);
                uint ip = BitConverter.ToUInt32(ipBytes.Reverse().ToArray(), 0);
                uint port = (uint)new Random().Next(1024, 65536);
                peers.Add(new NodeAddressInfo(ip, port));
                count--;
            }
            return peers;
        }

        [TestMethod]
        public async Task FetchPeerList()
        {
            var nodeInfo = new NodeAddressInfo(BitConverter.ToUInt32([127, 0, 0, 1]), 9005);
            var nodeCount = 10;
            var memoryBlockIterator = new MemoryBlockIterator();
            var memoryWalletIterator = new MemoryWalletIterator();
            var blockchain = new Blockchain(memoryBlockIterator, memoryWalletIterator);
            var daemon = new Daemon(blockchain, 9005, GeneratePeers(nodeCount));

            daemon.StartAsync();

            var peers = NodeConnection.AskForPeers([nodeInfo], 9091);
            Assert.IsNotNull(peers);
            Assert.IsTrue(peers.Count == nodeCount);
            daemon.Stop();
        }

        [TestMethod]
        public async Task WalletCreation()
        {
            var nodeInfo = new NodeAddressInfo(BitConverter.ToUInt32([127, 0, 0, 1]), 9005);
            var memoryBlockIterator = new MemoryBlockIterator();
            var memoryWalletIterator = new MemoryWalletIterator();
            var blockchain = new Blockchain(memoryBlockIterator, memoryWalletIterator);
            var daemon = new Daemon(blockchain, 9005);
            daemon.StartAsync();

            var wallet = Wallet.CreateUniqueWallet(blockchain, new PrivateKey(ClusterTests.keys[0]));

            NodeConnection.AnnounceNewWallet([nodeInfo], 9091, wallet.ToPublicWallet());
            blockchain.RegisterWallet(wallet.ToPublicWallet());
            Assert.IsNotNull(blockchain.GetKeyFromWalletId(wallet.Identifier));
            daemon.Stop();
        }
    }
}
