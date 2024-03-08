using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace proiect_mds.blockchain
{
    internal class WalletId
    {
        public const string WID_PREFIX = "+codrea";
        private const int WID_LENGTH = 16;

        private byte[] value = new byte[WID_LENGTH];

        public WalletId(byte[] data)
        {
            if (data.Length != WID_LENGTH)
            {
                throw new ArgumentException($"Invalid data length: {data.Length}. Expected {WID_LENGTH} bytes.");
            }

            this.value = data;
        }

        public override string ToString()
        {
            return WID_PREFIX + Convert.ToHexString(value);
        }
    }

    internal class Wallet
    {
        public WalletId Identifier { get; private set; }

       // public Wallet(Blockchain blockchain, WalletId identifier)
        //{

       // }
    }
}
