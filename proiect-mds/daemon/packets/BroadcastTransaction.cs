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
    public enum BroadcastTransactionResponseCode
    {
        [EnumMember]
        [ProtoEnum]
        Okay = 1,
        [EnumMember]
        [ProtoEnum]
        InvalidTransaction = 2,
        [EnumMember]
        [ProtoEnum]
        AlreadyReceived = 3,
    }

    [ProtoContract]
    internal class BroadcastTransactionResponse
    {
        [ProtoMember(1)]
        public BroadcastTransactionResponseCode Code { get; private set; }
        public BroadcastTransactionResponse(BroadcastTransactionResponseCode code)
        {
            Code = code;
        }
        public BroadcastTransactionResponse() { }
    }
}
