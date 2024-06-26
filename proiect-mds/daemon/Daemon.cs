﻿using proiect_mds.blockchain;
using proiect_mds.daemon.client;
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
        public static uint VERSION = 1;
        public uint Port { get; private set; }
        public Blockchain Blockchain { get; private set; }
        public List<Validator> Validators { get; private set; } = [];
        public List<Transaction> TransactionsQueued { get; private set; } = [];
        private bool isStarted = true;
        public List<NodeAddressInfo> Peers { get; private set; }
        public Daemon(Blockchain blockchain, uint port = 9005, List<NodeAddressInfo>? peers = null)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(port);
            Port = port;
            if (peers != null)
                Peers = peers;
            else
                Peers = [];
            this.Blockchain = blockchain;
        }
        public async Task StartAsync()
        {
            using var listener = new TcpListener(IPAddress.Any, (int)Port);
            listener.Start();

            HandleTimedValdidation();

            while (isStarted)
            {
                var client = await listener.AcceptTcpClientAsync();
                _ = Task.Run(async () => await HandleResponse(client));
                Thread.Sleep(1);
            }
        }
        private void HandleTimedValdidation()
        {
            _ = Task.Run(() =>
            {
                while (isStarted)
                {
                    BlockBirth();
                    Thread.Sleep(1000);
                }
            });
        }

        public void BlockBirth(bool forced=false)
        {
            var dateNow = DateTime.Now;
            if ((dateNow.Second == 30 && TransactionsQueued.Count >= ValidatorSelector.MIN_TRANSACTIONS) || forced)
            {
                var lastBlock = Blockchain.GetLatestBlock();
                var tempBlock = new Block(lastBlock.Index + 1, DateTime.Now, Hash.FromBlock(lastBlock), WalletId.MasterWalletId(), TransactionsQueued);
                var validatorSelector = new ValidatorSelector(Validators, tempBlock);
                var selected = validatorSelector.GetPickedValidator();
                Blockchain.AddBlock(new Block(tempBlock.Index, dateNow, tempBlock.PreviousHash, selected.WalletId, tempBlock.Transactions));
                Validators.Clear();
                TransactionsQueued.Clear();
            }
        }

        public void Stop()
        {
            isStarted = false;
        }

        private async Task HandleResponse(TcpClient client)
        {
            using var networkStream = client.GetStream();
            var helloPacket = DecodeMessage<NodeHello>(networkStream);
            if (helloPacket == null)
            {
                var response = new NodeWelcome(HelloResponseCode.BadRequest);
                Serializer.SerializeWithLengthPrefix<NodeWelcome>(networkStream, response, PrefixStyle.Fixed32);
                client.Close();
                return;
            }
            switch (helloPacket.RequestType)
            {
                case RequestType.SyncBlockchain:
                    {
                        HandleSyncChain(networkStream);
                        break;
                    }
                case RequestType.BroadcastTransaction:
                    {
                        ReceiveTransactionMessage(networkStream); 
                        break;  
                    }
                case RequestType.AskForPeers:
                    {
                        BroadcastPeers(networkStream);
                        break;
                    }
                case RequestType.BecomeValidator:
                    {
                        AcceptValidator(networkStream);
                        break;
                    }
                case RequestType.CreateWallet:
                    {
                        GetNewWallet(networkStream);
                        break;
                    }
            }
            var ip = (IPEndPoint?)client.Client.RemoteEndPoint;
            if (ip != null)
            {
                var newPeer = new NodeAddressInfo(BitConverter.ToUInt32(ip.Address.GetAddressBytes()), helloPacket.Port);
                if (!Peers.Exists(el => el.IPv4 == newPeer.IPv4 && el.Port == newPeer.Port))
                    Peers.Add(newPeer);
            }
        }
        private void HandleSyncChain(NetworkStream networkStream)
        {
            var syncInitPacket = DecodeMessage<SyncChainRequest>(networkStream);
            if (syncInitPacket == null)
            {
                return;
            }

            var lastKnownBlock = Blockchain.GetBlock(syncInitPacket.LastKnownBlockIndex);
            if (lastKnownBlock == null)
            {
                return;
            }

            if (Hash.FromBlock(lastKnownBlock) != syncInitPacket.LastKnownBlockHash)
            {
                return;
            }

            var latestBlock = Blockchain.GetLatestBlock();
            if (latestBlock == null || lastKnownBlock.Index >= latestBlock.Index)
            {
                return;
            }

            UInt64 cIndex = lastKnownBlock.Index;
            var nextBlock = Blockchain.GetBlock(cIndex);
            if (nextBlock == null)
            {
                var response = new SyncChainResponse(SyncChainResponseType.BlockNotFound, null);
                Serializer.SerializeWithLengthPrefix(networkStream, response, PrefixStyle.Fixed32);
                return;
            }

            if (Hash.FromBlock(nextBlock) == Hash.FromBlock(lastKnownBlock))
            {
                var response = new SyncChainResponse(SyncChainResponseType.HashMismatch, null);
                Serializer.SerializeWithLengthPrefix(networkStream, response, PrefixStyle.Fixed32);
                return;
            }

            while (cIndex < latestBlock.Index)
            {
                try
                {
                    nextBlock = Blockchain.GetBlock(cIndex++);
                    if (nextBlock == null)
                    {
                        var response = new SyncChainResponse(SyncChainResponseType.BlockNotFound, null);
                        Serializer.SerializeWithLengthPrefix(networkStream, response, PrefixStyle.Fixed32);
                        return;
                    }
                }
                catch (Exception)
                {
                    var response = new SyncChainResponse(SyncChainResponseType.BlockNotFound, null);
                    Serializer.SerializeWithLengthPrefix(networkStream, response, PrefixStyle.Fixed32);
                    return;
                }
            }
        }
        private void ReceiveTransactionMessage(NetworkStream networkStream)
        {
            var trans = DecodeMessage<Transaction>(networkStream);
            if (trans == null)
                return;
            if (TransactionsQueued.Exists(tr => tr == trans))
            {
                var aReceived = new BroadcastTransactionResponse(BroadcastTransactionResponseCode.Okay);
                Serializer.SerializeWithLengthPrefix(networkStream, aReceived, PrefixStyle.Fixed32);
                return;
            }
            TransactionsQueued.Add(trans);
            var resp = new BroadcastTransactionResponse(BroadcastTransactionResponseCode.Okay);
            Serializer.SerializeWithLengthPrefix(networkStream, resp, PrefixStyle.Fixed32);
        }
        private void AcceptValidator(NetworkStream networkStream)
        {
            var elect = DecodeMessage<BecomeValidatorPacket>(networkStream);
            if (elect == null)
            {
                var response = new ValidatorResponsePacket(ValidatorResponseType.BadFormatting);
                Serializer.SerializeWithLengthPrefix(networkStream, response, PrefixStyle.Fixed32);
                return;
            }
            var balance = Blockchain.GetWalletBalance(elect.WalletId);
            var publicKey = Blockchain.GetKeyFromWalletId(elect.WalletId);
            if (publicKey == null || balance == null || balance < elect.Stake)
            {
                var response = new ValidatorResponsePacket(ValidatorResponseType.InvalidWalletOrBalance);
                Serializer.SerializeWithLengthPrefix(networkStream, response, PrefixStyle.Fixed32);
                return;
            }
            var valid = publicKey.ValidateElectionSignature(elect.WalletId, elect.Timestamp, elect.Stake, elect.Signature);
            if (!valid)
            {
                var response = new ValidatorResponsePacket(ValidatorResponseType.BadSignature);
                Serializer.SerializeWithLengthPrefix(networkStream, response, PrefixStyle.Fixed32);
                return;
            }
            Validators.Add(new Validator(elect.WalletId, elect.Stake));
            Serializer.SerializeWithLengthPrefix(networkStream, new ValidatorResponsePacket(ValidatorResponseType.Accepted), PrefixStyle.Fixed32);
        }
        private void BroadcastPeers(NetworkStream networkStream)
        {
            var peerPacket = new NodeAdvertiseResponse(Peers);
            Serializer.SerializeWithLengthPrefix(networkStream, peerPacket, PrefixStyle.Fixed32);
            return;
        }
        private void GetNewWallet(NetworkStream networkStream)
        {
            var pubWallet = DecodeMessage<PublicWallet>(networkStream);
            if (pubWallet == null)
                return;
            Blockchain.RegisterWallet(pubWallet);
        }
        public void FetchPeers()
        {
            Peers = NodeConnection.AskForPeers(Peers, Port);
        }
        public static T? DecodeMessage<T>(Stream stream) where T : class
        {
            try
            {
                var ret = Serializer.DeserializeWithLengthPrefix<T>(stream, PrefixStyle.Fixed32);
                return ret;
            } catch(Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }
        }
    }
}
