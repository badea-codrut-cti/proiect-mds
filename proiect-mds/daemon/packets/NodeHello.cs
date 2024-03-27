using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace proiect_mds.daemon.packets
{
    [DataContract]
    [ProtoContract]
    public enum RequestType
    {
        AskForPeers,
        SyncBlockchain,
        BroadcastTransaction
    }

    [DataContract]
    [ProtoContract]
    public enum HelloResponseCode
    {
        VersionMismatch,
        BadRequest,
        Success
    }

    [ProtoContract]
    internal class NodeHello
    {
        [ProtoMember(1)]
        public UInt32 Version { get; private set; }
        [ProtoMember(2)]
        public UInt32 Port { get; private set; }
        [ProtoMember(3)]
        public RequestType RequestType { get; private set; }
        public NodeHello(UInt32 version, UInt32 port) 
        { 
            this.Version = version;
            this.Port = port;
        }
    }

    [ProtoContract]
    internal class NodeWelcome
    {
        [ProtoMember(1)]
        public HelloResponseCode Code {  get; private set; }
        public NodeWelcome(HelloResponseCode code)
        {
            this.Code = code;
        }
    }
}
