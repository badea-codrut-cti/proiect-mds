using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace proiect_mds.daemon.packets
{
    [ProtoContract]
    internal class NodeAddressInfo
    {
        [ProtoMember(1)]
        public UInt32 IPv4 {  get; private set; }
        [ProtoMember(2)]
        public UInt32 Port { get; private set; }
        public NodeAddressInfo(UInt32 ipv4, UInt32 port)
        {
            IPv4 = ipv4;
            Port = port;
        }
    }

    [ProtoContract]
    internal class NodeAdvertiseResponse
    {
        [ProtoMember(1)]
        public UInt32 NodeCount { get; private set; }
        [ProtoMember(2)]
        public List<NodeAddressInfo> nodes { get; private set; }
        public NodeAdvertiseResponse(List<NodeAddressInfo> nodes)
        {
            NodeCount = (uint)nodes.Count;
            this.nodes = nodes;
        }
    }
}
