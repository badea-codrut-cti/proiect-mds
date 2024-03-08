using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace proiect_mds.blockchain
{
    internal class Hash
    {
        private byte[] value = new byte[32];

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

        public static Hash FromBlock(Block block)
        {
            StringBuilder dataBuilder = new StringBuilder();

            dataBuilder.Append($"{block.Index}{block.Timestamp}{block.PreviousHash.Value}{block.ValidatorId}");

            foreach (Transaction transaction in block.Transactions)
            {
                // TODO: transaction sig
                dataBuilder.Append($"{transaction.Sender}{transaction.Receiver}{transaction.Amount}");
            }

            byte[] dataBytes = Encoding.UTF8.GetBytes(dataBuilder.ToString());
            return new Hash(dataBytes);
        }

        public override string ToString()
        {
            return Convert.ToHexString(value);
        }
    }
}
