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
        [EnumMember]
        [ProtoEnum]
        AskForPeers = 1,
        [EnumMember]
        [ProtoEnum]
        SyncBlockchain = 2,
        [EnumMember]
        [ProtoEnum]
        BroadcastTransaction = 3,
        [EnumMember]
        [ProtoEnum]
        BecomeValidator = 4
    }

    [DataContract]
    [ProtoContract]
    public enum HelloResponseCode
    {
        [EnumMember]
        [ProtoEnum]
        VersionMismatch = 1,
        [EnumMember]
        [ProtoEnum]
        BadRequest = 2,
        [EnumMember]
        [ProtoEnum]
        Success = 3
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
        public NodeHello(UInt32 version, UInt32 port, RequestType requestType) 
        { 
            this.Version = version;
            this.Port = port;
            this.RequestType = requestType;
        }
        public NodeHello()
        {

        }
    }

    [ProtoContract]
    internal class NodeWelcome
    {
        [ProtoMember(1)]
        public HelloResponseCode Code { get; private set; }
        public NodeWelcome(HelloResponseCode code)
        {
            this.Code = code;
        }
    }
}
