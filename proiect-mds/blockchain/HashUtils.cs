using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace proiect_mds.blockchain
{
    [ProtoContract]
    internal class Hash
    {
        public static uint HASH_LENGTH = 32;
        [ProtoMember(1)]
        private byte[] value = new byte[HASH_LENGTH];

        public Hash(byte[] data)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                Buffer.BlockCopy(sha256.ComputeHash(data), 0, value, 0, value.Length);
            }
        }

        public byte[] Value
        {
            get { return value; }
        }

        public override string ToString()
        {
            return Convert.ToHexString(value);
        }

        public override int GetHashCode()
        {
            int sum = 0;
            foreach(byte b in value) { 
                sum += b; 
            }
            return sum;
        }

        public static Hash FromBlock(Block block)
        {
            StringBuilder dataBuilder = new StringBuilder();

            dataBuilder.Append($"{block.Index}{block.Timestamp}{block.PreviousHash.Value}{block.ValidatorId}");

            foreach (Transaction transaction in block.Transactions)
            {
                dataBuilder.Append($"{transaction.Sender}{transaction.Receiver}{transaction.Amount}{Convert.ToHexString(transaction.Signature)}");
            }

            byte[] dataBytes = Encoding.UTF8.GetBytes(dataBuilder.ToString());
            return new Hash(dataBytes);
        }
    }
}
