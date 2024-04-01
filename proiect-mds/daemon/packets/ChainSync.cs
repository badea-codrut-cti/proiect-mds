using proiect_mds.blockchain;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace proiect_mds.daemon.packets
{
    [ProtoContract]
    internal class SyncChainRequest
    {
        [ProtoMember(1)]
        public UInt64 LastKnownBlockIndex { get; private set; }
        [ProtoMember(2)]
        public Hash LastKnownBlockHash {  get; private set; }

        public SyncChainRequest(UInt64 lastKnownBlockIndex, Hash lastKnownBlockHash)
        {
            this.LastKnownBlockIndex = lastKnownBlockIndex;
            this.LastKnownBlockHash = lastKnownBlockHash;
        }
    }

    [DataContract]
    [ProtoContract]
    public enum SyncChainResponseType
    {
        [EnumMember]
        [ProtoEnum]
        NextBlock = 1,
        [EnumMember]
        [ProtoEnum]
        BlockNotFound = 2,
        [EnumMember]
        [ProtoEnum]
        HashMismatch = 3
    }

    [ProtoContract]
    internal class SyncChainResponse
    {
        [ProtoMember(1)]
        public SyncChainResponseType responseType { get; private set; }
        [ProtoMember(2)]
        public Block? Block { get; private set; }

        public SyncChainResponse(SyncChainResponseType responseType, Block? block)
        {
            if (responseType == SyncChainResponseType.NextBlock) 
                ArgumentNullException.ThrowIfNull(block);

            this.responseType = responseType;
            this.Block = block;
        }
    }
}
