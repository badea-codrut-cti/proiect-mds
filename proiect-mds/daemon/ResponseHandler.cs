﻿using proiect_mds.blockchain;
using proiect_mds.daemon.packets;
using ProtoBuf;
using System.Net.Sockets;

namespace proiect_mds.daemon
{
    internal class ResponseHandler
    {
        public static int VERSION = 1;
        private Blockchain blockchain;
        
        public ResponseHandler(Blockchain blockchain)
        {
            this.blockchain = blockchain;
        }
        public async Task HandleResponse(TcpClient client)
        {
            using var networkStream = client.GetStream();
            var helloPacket = await DecodeMessage<NodeHello>(networkStream);

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
                        await HandleSyncChain(networkStream);
                        break;
                    }
            }
        }
        public async Task HandleSyncChain(NetworkStream networkStream)
        {
            var syncInitPacket = await DecodeMessage<SyncChainRequest>(networkStream);
            if (syncInitPacket == null)
            {
                return;
            }

            var lastKnownBlock = blockchain.GetBlock(syncInitPacket.LastKnownBlockIndex);
            if (lastKnownBlock == null)
            {
                return;
            }

            if (Hash.FromBlock(lastKnownBlock) != syncInitPacket.LastKnownBlockHash)
            {
                return;
            }

            var latestBlock = blockchain.GetLatestBlock();
            if (latestBlock == null || lastKnownBlock.Index >= latestBlock.Index)
            {
                return;
            }

            UInt64 cIndex = lastKnownBlock.Index;
            var nextBlock = blockchain.GetBlock(cIndex);
            if (nextBlock == null)
            {
                var response = new SyncChainResponse(SyncChainResponseType.BlockNotFound, null);
                Serializer.SerializeWithLengthPrefix<SyncChainResponse>(networkStream, response, PrefixStyle.Fixed32);
                return;
            }

            if (Hash.FromBlock(nextBlock) == Hash.FromBlock(lastKnownBlock))
            {
                var response = new SyncChainResponse(SyncChainResponseType.HashMismatch, null);
                Serializer.SerializeWithLengthPrefix<SyncChainResponse>(networkStream, response, PrefixStyle.Fixed32);
                return;
            }

            while (cIndex < latestBlock.Index)
            {
                nextBlock = blockchain.GetBlock(cIndex++);
                if (nextBlock == null)
                {
                    var response = new SyncChainResponse(SyncChainResponseType.BlockNotFound, null);
                    Serializer.SerializeWithLengthPrefix<SyncChainResponse>(networkStream, response, PrefixStyle.Fixed32);
                    return;
                }
            }
        }
        public static async Task<T> DecodeMessage<T>(Stream stream) where T : class
        {
            byte[] lengthBytes = new byte[4];
            await stream.ReadAsync(lengthBytes.AsMemory(0, 4));
            var messageLength = BitConverter.ToInt32(lengthBytes, 0);

            byte[] data = new byte[messageLength];
            await stream.ReadAsync(data.AsMemory(0, messageLength));

            using var ms = new MemoryStream(data);
            return Serializer.Deserialize<T>(ms);
        }
    }
}
