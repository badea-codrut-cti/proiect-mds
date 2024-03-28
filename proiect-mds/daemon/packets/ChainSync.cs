using proiect_mds.blockchain;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
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
}
