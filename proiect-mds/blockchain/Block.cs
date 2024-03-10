﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace proiect_mds.blockchain 
{
    internal class Transaction
    {
        public static int SIGNATURE_LENGTH = 16;
        public WalletId Sender { get; private set; }
        public WalletId Receiver { get; private set; }
        public UInt64 Amount { get; private set; }
        private readonly byte[] signature = new byte[SIGNATURE_LENGTH];
        public Transaction(WalletId sender, WalletId receiver, ulong amount, byte[] signature)
        {
            if (signature.Length != SIGNATURE_LENGTH)
                throw new ArgumentException("Signature length is invalid.");

            this.Sender = sender;
            this.Receiver = receiver;
            this.Amount = amount;
            this.signature = signature;
        }
        public byte[] Signature { get { return signature; } }
    }

    internal class Block 
    {
        public ulong Index { get; private set; }
        public DateTime Timestamp { get; private set; }
        public Hash? PreviousHash { get; private set; }
        public WalletId ValidatorId { get; private set; }
        public List<Transaction> Transactions { get; private set; }

        public Block(ulong index, DateTime timestamp, Hash previousHash, WalletId validatorId, List<Transaction> transactions)
        {
            if (transactions.Count == 0 || transactions.Count > 0xFF)
                throw new ArgumentException("Cannot have an empty block or a block with more than 255 transactions.");

            this.Index = index;
            this.Timestamp = timestamp;
            this.PreviousHash = previousHash;
            this.ValidatorId = validatorId;
            this.Transactions = transactions;
        }
    }
}
