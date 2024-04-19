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
    public enum WalletAnnouncementResponseType
    {
        [EnumMember]
        [ProtoEnum]
        Okay = 1,
        [EnumMember]
        [ProtoEnum]
        KeyMismatch = 2
    }
    [ProtoContract]
    internal class WalletAnnouncementResponse
    {
        [ProtoMember(1)]
        public WalletAnnouncementResponseType responseType { get; private set; }
        public WalletAnnouncementResponse(WalletAnnouncementResponseType responseType)
        {
            this.responseType = responseType;
        }

        public WalletAnnouncementResponse() { }
    }
}
