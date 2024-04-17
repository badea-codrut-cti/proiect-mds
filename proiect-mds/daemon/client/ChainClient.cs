using proiect_mds.blockchain;
using proiect_mds.blockchain.exception;
using proiect_mds.daemon.packets;
using ProtoBuf;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace proiect_mds.daemon.client
{
    internal class NodeConnection
    {
        private readonly TcpClient client;
        public NodeConnection(NodeAddressInfo nodeAddressInfo, uint daemonPort, RequestType reqType) 
        {
            byte[] ipBytes = BitConverter.GetBytes(nodeAddressInfo.IPv4);
            var ipAddress = new IPAddress(ipBytes);
            client = new TcpClient(ipAddress.ToString(), (int)nodeAddressInfo.Port);
            var stream = client.GetStream();
            var helloPacket = new NodeHello(Daemon.VERSION, daemonPort, reqType);
            Serializer.SerializeWithLengthPrefix<NodeHello>(stream, helloPacket, PrefixStyle.Fixed32);
        }
        public void Close()
        {
            client.Close();
        }
        public static async Task RequestChainSignature(List<NodeAddressInfo> peers, uint daemonPort, Blockchain chain)
        {
            var con = new List<NodeConnection>(peers.Count);
            for (int i=0; i<peers.Count; i++)
            {
                con[i] = new NodeConnection(peers[i], daemonPort, RequestType.SyncBlockchain);
                var stream = con[i].client.GetStream();
                var lastBlock = chain.GetLatestBlock();
                var syncReqPacket = new SyncChainRequest(lastBlock.Index, Hash.FromBlock(lastBlock));
                Serializer.SerializeWithLengthPrefix<SyncChainRequest>(stream, syncReqPacket, PrefixStyle.Fixed32);
            }

            Block? block = null;
            for (int i = 0; i < peers.Count; i++)
            {
                var stream = con[i].client.GetStream();
                try
                {
                    var syncResp = Daemon.DecodeMessage<SyncChainResponse>(stream);
                    if (syncResp.responseType != SyncChainResponseType.NextBlock || syncResp.Block == null)
                    {
                        con[i].Close();
                        con.RemoveAt(i);
                        i--;
                        continue;
                    }

                    if (block != null)
                    {
                        if (Hash.FromBlock(block) != Hash.FromBlock(syncResp.Block))
                            throw new BlockException("Two of the nodes queried had different responses. One of them is likely rogue.");
                    }
                } catch (BlockException)
                {
                    throw;
                }
                catch(Exception)
                {
                    con[i].Close();
                    con.RemoveAt(i);
                    i--;
                }
            }
            if (block != null)
                chain.AddBlock(block);
        }
        public static void BroadcastTransaction(List<NodeAddressInfo> peers, uint daemonPort, Transaction transaction)
        {
            foreach (var peer in peers)
            {
                var con = new NodeConnection(peer, daemonPort, RequestType.BroadcastTransaction);
                var stream = con.client.GetStream();
                Serializer.SerializeWithLengthPrefix(stream, transaction, PrefixStyle.Fixed32);
                var resp = Daemon.DecodeMessage<BroadcastTransactionResponse>(stream);
                con.client.Close();
            }
        }
        public static List<NodeAddressInfo> AskForPeers(List<NodeAddressInfo> peers, uint daemonPort)
        {
            List<NodeAddressInfo> ret = new();
            foreach (var peer in peers)
            {
                var con = new NodeConnection(peer, daemonPort, RequestType.AskForPeers);
                var stream = con.client.GetStream();
                var resp = Daemon.DecodeMessage<NodeAdvertiseResponse>(stream);
                if (resp == null)
                    continue;
                ret = ret.Concat(resp.Nodes).ToList();
                con.client.Close();
            }
            return ret;
        }
    }
}
