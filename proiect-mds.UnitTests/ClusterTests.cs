using proiect_mds.blockchain;
using proiect_mds.blockchain.impl;
using proiect_mds.daemon;
using proiect_mds.daemon.client;
using proiect_mds.daemon.packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace proiect_mds.UnitTests
{
    [TestClass]
    public class ClusterTests
    {
        public static string[] keys = [
            "MHQCAQEEIMgPlbOxLoocMnN68H1ThwJ3BBM/2ylqCRCDPGrvQnEHoAcGBSuBBAAKoUQDQgAElJYG+9/KLJWqoQjSqQXzEX06saeiV1Pheq4wAKgVkzHPzRKkOz1bkuwy9htqun1ut5kwxPKYZRUcKolSSF8ROA==",
            "MHQCAQEEIJIq44Tbdp1fPA5TWlfQDQxoMaqikQvvU8km45Kb9O+ioAcGBSuBBAAKoUQDQgAEP5/NJUX0BEEC8ych1XPtoMxcP39KqsPAeztr1954NIjIMkpguxv/EhIhYS2zOEJK+GYt2eTjofRDtS6zWJY+/g==",
            "MHQCAQEEIBZFUAVlrPesJpR6gWdwT/h9WtTxamKd8v4I6LjpO61ZoAcGBSuBBAAKoUQDQgAEifW8GrpzXLnqcd/w7LjsuAHo7iV6nqm/Qs1iwUvvg2Fq4uHMrzuSdlMi19tGGARsP2TnN5No9JVTsT7Y6AW4iA=="
            ];
        public static string[] pubKeys = [
            "MFYwEAYHKoZIzj0CAQYFK4EEAAoDQgAElJYG+9/KLJWqoQjSqQXzEX06saeiV1Pheq4wAKgVkzHPzRKkOz1bkuwy9htqun1ut5kwxPKYZRUcKolSSF8ROA==",
            "MFYwEAYHKoZIzj0CAQYFK4EEAAoDQgAEP5/NJUX0BEEC8ych1XPtoMxcP39KqsPAeztr1954NIjIMkpguxv/EhIhYS2zOEJK+GYt2eTjofRDtS6zWJY+/g==",
            "MFYwEAYHKoZIzj0CAQYFK4EEAAoDQgAEifW8GrpzXLnqcd/w7LjsuAHo7iV6nqm/Qs1iwUvvg2Fq4uHMrzuSdlMi19tGGARsP2TnN5No9JVTsT7Y6AW4iA=="
            ];
        public static string[] wIds = [
            "0123456789ABCDEF",
            "123456789ABCDEFG",
            "23456789ABCDEFGH"
            ];
        private static Blockchain MockChain()
        {
            var memoryWalletIterator = new MemoryWalletIterator();
            var memoryBlockIterator = new MemoryBlockIterator();

            ulong[] weights = [50, 90, 40];
            List<Transaction> transactions = new();

            for (int i = 0; i < wIds.Length; i++)
            {
                memoryWalletIterator.AddWallet(
                    new PublicWallet(
                        new WalletId(Encoding.UTF8.GetBytes(wIds[i])),
                        new PublicKey(pubKeys[i])
                   )
               );
            }

            for (int i = 0; i < weights.Length; i++)
            {
                // Doesn't matter which key is used as long as everyone believes 
                // This is the right one.
                transactions.Add(new PrivateKey(keys[0]).SignTransaction(
                    WalletId.MasterWalletId(),
                    new WalletId(Encoding.UTF8.GetBytes(wIds[i])),
                    100 + weights[i],
                    new DateTime(2024, 4, 19)
                ));
            }

            var b1 = new Block(
                    1,
                    new DateTime(2024, 4, 19),
                    Hash.FromBlock(Block.GenesisBlock()),
                    WalletId.MasterWalletId(),
                    transactions
                );
            memoryBlockIterator.AddBlock(b1);

            memoryBlockIterator.AddBlock(
                new Block(2, new DateTime(2024, 4, 20), 
                Hash.FromBlock(b1), WalletId.MasterWalletId(),
                [
                    new PrivateKey(keys[1]).SignTransaction(
                        new WalletId(wIds[1]),
                        new WalletId(wIds[0]),
                        weights[0],
                        new DateTime(2024, 4, 20)
                    ),
                    new PrivateKey(keys[2]).SignTransaction(
                        new WalletId(wIds[2]),
                        new WalletId(wIds[1]),
                        weights[2],
                        new DateTime(2024, 4, 20)
                    )
                ])
            );
            var blockchain = new Blockchain(memoryBlockIterator, memoryWalletIterator);
            return blockchain;
        }

        [TestMethod]
        public void ClusterConnectivity()
        {
            var mockchain = MockChain();
            Daemon[] daemons = [
                new Daemon(mockchain, 8001, [
                    new NodeAddressInfo(BitConverter.ToUInt32([127, 0, 0, 1]), 8005)
                ]),
                new Daemon(mockchain, 8005, [
                    new NodeAddressInfo(BitConverter.ToUInt32([127, 0, 0, 1]), 8009)
                ]),
                new Daemon(mockchain, 8009)
            ];
            for (int i = 0; i < daemons.Length; i++)
                daemons[i].StartAsync();

            daemons[0].FetchPeers();
            daemons[0].FetchPeers();
            daemons[1].FetchPeers();
            Assert.IsTrue(daemons[2].Peers.Count == 2);

            for (int i = 0; i < daemons.Length; i++)
                daemons[i].Stop();
        }
        
        [TestMethod]
        public void NewBlockValidated()
        {
            var mockchain = MockChain();
            Daemon[] daemons = [
                new Daemon(mockchain, 8001),
                new Daemon(mockchain, 8005),
                new Daemon(mockchain, 8009)
            ];
            for (int i = 0; i < daemons.Length; i++)
                daemons[i].StartAsync();

            List< NodeAddressInfo> peers = [
                new NodeAddressInfo(BitConverter.ToUInt32([127, 0, 0, 1]), 8001),
                new NodeAddressInfo(BitConverter.ToUInt32([127, 0, 0, 1]), 8005),
                new NodeAddressInfo(BitConverter.ToUInt32([127, 0, 0, 1]), 8009)
                ];

            NodeConnection.BroadcastTransaction(peers, daemons[0].Port, 
            new PrivateKey(keys[0]).SignTransaction(new WalletId(Encoding.UTF8.GetBytes(wIds[0])), 
            new WalletId(wIds[1]), 10, new DateTime(2024, 4, 20, 19, 27, 35)));
            
            NodeConnection.BroadcastTransaction(peers, daemons[0].Port,
            new PrivateKey(keys[0]).SignTransaction(new WalletId(Encoding.UTF8.GetBytes(wIds[0])),
            new WalletId(wIds[2]), 10, new DateTime(2024, 4, 20, 19, 27, 40)));

            NodeConnection.BroadcastTransaction(peers, daemons[1].Port,
            new PrivateKey(keys[1]).SignTransaction(new WalletId(Encoding.UTF8.GetBytes(wIds[1])),
            new WalletId(wIds[2]), 10, new DateTime(2024, 4, 20, 19, 27, 42)));

            for (int i=0; i<peers.Count; i++)
            {
                NodeConnection.BecomeValidator(peers, daemons[i].Port,
                new PrivateKey(keys[i]).SignValidatorPacket(new WalletId(wIds[i]), 10, new DateTime(2024, 4, 20, 19, 27, 45 + i)));
            }

            for (int i = 0; i < daemons.Length; i++)
                daemons[i].BlockBirth(true);

            Assert.IsTrue(daemons[1].Blockchain.GetLatestBlock().Index == 3);

           for (int i = 0; i < daemons.Length; i++)
                daemons[i].Stop();
        }
    }
}
