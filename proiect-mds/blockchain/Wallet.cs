using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace proiect_mds.blockchain
{
    internal class WalletId
    {
        public static string WID_PREFIX = "+codrea";
        public static int WID_LENGTH = 16;

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

    internal abstract class PrivateKey
    {
        public abstract Transaction SignTransaction(WalletId sender, WalletId receiver, UInt64 Amount);
    }

    internal abstract class PublicKey
    {
        public abstract bool validateTransaction(Transaction transaction);
    }

    internal class Wallet
    {
        public WalletId Identifier { get; private set; }

        private readonly Blockchain blockchain;
        private readonly PrivateKey privateKey;

        public Wallet(Blockchain blockchain, WalletId identifier, PrivateKey privateKey)
        {
            this.blockchain = blockchain;
            this.Identifier = identifier;
            this.privateKey = privateKey;
        }
        public UInt64 GetBalance()
        {
            return blockchain.GetWalletBalance(Identifier);
        }
        public Transaction GenerateTransaction(WalletId receiver, UInt64 amount)
        {
            if (GetBalance() < amount)
            {
                throw new InvalidOperationException("Insufficient funds.");
            }

            return privateKey.SignTransaction(Identifier, receiver, amount);
        }
    }
}
