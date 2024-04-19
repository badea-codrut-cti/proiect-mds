using proiect_mds.blockchain;
using proiect_mds.blockchain.impl;
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
                transactions.Add(new Transaction(
                    WalletId.MasterWalletId(),
                    new WalletId(Encoding.UTF8.GetBytes(wIds[i])),
                    100 + weights[i],
                    null,
                    new DateTime(19, 4, 2024)
                ));
            }

            var b1 = new Block(
                    1,
                    new DateTime(19, 4, 2024),
                    Hash.FromBlock(Block.GenesisBlock()),
                    WalletId.MasterWalletId(),
                    transactions
                );
            memoryBlockIterator.AddBlock(b1);

            memoryBlockIterator.AddBlock(
                new Block(2, new DateTime(20, 4, 2024), 
                Hash.FromBlock(b1), WalletId.MasterWalletId(),
                [
                    new PrivateKey(keys[1]).SignTransaction(
                        new WalletId(Encoding.UTF8.GetBytes(wIds[1])),
                        new WalletId(Encoding.UTF8.GetBytes(wIds[0])),
                        weights[0],
                        new DateTime(20, 4, 2024)
                    ),
                    new PrivateKey(keys[2]).SignTransaction(
                        new WalletId(Encoding.UTF8.GetBytes(wIds[2])),
                        new WalletId(Encoding.UTF8.GetBytes(wIds[1])),
                        weights[2],
                        new DateTime(20, 4, 2024)
                    )
                ])
            );
            var blockchain = new Blockchain(memoryBlockIterator, memoryWalletIterator);
            return blockchain;
        }
    }
}
