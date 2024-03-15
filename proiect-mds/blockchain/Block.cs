﻿using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace proiect_mds.blockchain 
{
    [ProtoContract]
    internal class Transaction
    {
        public static UInt16 SIGNATURE_LENGTH = 16;

        [ProtoMember(1)]
        public WalletId Sender { get; private set; }
        [ProtoMember(2)]
        public WalletId Receiver { get; private set; }
        [ProtoMember(3)]
        public UInt64 Amount { get; private set; }
        [ProtoMember(4)]
        public DateTime Timestamp { get; private set; }
        private readonly byte[] signature = new byte[SIGNATURE_LENGTH];
        public Transaction(WalletId sender, WalletId receiver, ulong amount, byte[] signature, DateTime timestamp)
        {
            if (signature.Length != SIGNATURE_LENGTH)
                throw new ArgumentException("Signature length is invalid.");

            this.Sender = sender;
            this.Receiver = receiver;
            this.Amount = amount;
            this.signature = signature;
            this.Timestamp = timestamp;
        }
        public byte[] Signature { get { return signature; } }
    }

    [ProtoContract]
    internal class Block 
    {
        [ProtoMember(1)]
        public ulong Index { get; private set; }
        [ProtoMember(2)]
        public DateTime Timestamp { get; private set; }
        [ProtoMember(3)]
        public Hash? PreviousHash { get; private set; }
        [ProtoMember(4)]
        public WalletId ValidatorId { get; private set; }
        [ProtoMember(5)]
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
