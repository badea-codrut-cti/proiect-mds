using proiect_mds.blockchain;
using ProtoBuf;
using System.Runtime.Serialization;

namespace proiect_mds.daemon.packets
{
    [ProtoContract]
    internal class BecomeValidatorPacket
    {
        [ProtoMember(1)]
        public WalletId WalletId { get; private set; }
        [ProtoMember(2)]
        public uint Stake { get; private set; }
        [ProtoMember(3)]
        public DateTime Timestamp { get; private set; }
        [ProtoMember(4)]
        private readonly byte[] signature;

        public byte[] Signature { get { return signature; } }

        public BecomeValidatorPacket(WalletId walletId, uint stake, DateTime timestamp, byte[] signature)
        {
            WalletId = walletId;
            Stake = stake;
            Timestamp = timestamp;
            this.signature = signature;
        }
        public BecomeValidatorPacket() { }
    }

    [DataContract]
    [ProtoContract]
    public enum ValidatorResponseType
    {
        [EnumMember]
        [ProtoEnum]
        Accepted = 1,
        [EnumMember]
        [ProtoEnum]
        InvalidWalletOrBalance = 2,
        [EnumMember]
        [ProtoEnum]
        BadFormatting = 3,
        [EnumMember]
        [ProtoEnum]
        BadSignature = 4
    }

    [ProtoContract]
    internal class ValidatorResponsePacket
    {
        [ProtoMember(1)]
        public ValidatorResponseType ResponseType { get; private set; }
        public ValidatorResponsePacket(ValidatorResponseType responseType) { 
            this.ResponseType = responseType;
        }
        public ValidatorResponsePacket() { }
    }
}
