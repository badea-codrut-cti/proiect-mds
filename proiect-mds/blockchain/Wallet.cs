using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Crypto;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.X9;

namespace proiect_mds.blockchain
{
    [ProtoContract]
    internal class WalletId
    {
        public static string WID_PREFIX = "+codrea";
        public static UInt16 WID_LENGTH = 16;
        [ProtoMember(1)]
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

    internal class PrivateKey
    {
        public static int PRIVATE_KEY_LENGTH = 300;
        private readonly ECPrivateKeyParameters privateKeyParams; 

        public PrivateKey(byte[] data)
        {
            if (data.Length != PRIVATE_KEY_LENGTH)
                throw new ArgumentException("Private key length mismatch.");
            BigInteger privateKey = new BigInteger(1, Encoding.UTF8.GetBytes("-----BEGIN EC PRIVATE KEY-----" + Encoding.UTF8.GetString(data) + "-----END EC PRIVATE KEY-----"));
            var ecp = ECNamedCurveTable.GetByName("secp521k1");
            var domainParams = new ECDomainParameters(ecp.Curve, ecp.G, ecp.N, ecp.H, ecp.GetSeed());
            privateKeyParams = new ECPrivateKeyParameters(privateKey, domainParams);
            if (privateKeyParams == null)
                throw new ArgumentException("Invalid private key.");
        }

        public Transaction? SignTransaction(WalletId sender, WalletId receiver, ulong amount, DateTime timestamp)
        {
            byte[] transactionData = Encoding.UTF8.GetBytes($"{sender}{receiver}{amount}{timestamp}");
            var signer = new ECDsaSigner();
            signer.Init(true, privateKeyParams);
            BigInteger[] signature = signer.GenerateSignature(transactionData);
            byte[] signatureBytes = new byte[0];
            foreach ( var x in signature )
            {
                signatureBytes.Concat(x.ToByteArray());
            }
            return new Transaction(sender, receiver, amount, signatureBytes, timestamp);
        }
    }

    [ProtoContract]
    internal abstract class PublicKey
    {
        [ProtoMember(1)]
        private readonly byte[] value;
        public static int PUBKEY_SIZE /* = length here*/;
        public PublicKey(byte[] pKey)
        {
            this.value = pKey;
        }
        public bool validateTransaction(Transaction transaction)
        {
            // TODO
            return true;
        }
    }

    [ProtoContract]
    internal class PublicWallet
    {
        [ProtoMember(1)]
        public WalletId Identifier { get; private set; }
        [ProtoMember(2)]
        public PublicKey PublicKey { get; private set; }

        public PublicWallet(WalletId identifier, PublicKey publicKey)
        {
            Identifier = identifier;
            PublicKey = publicKey;
        }

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
            UInt64? balance = blockchain.GetWalletBalance(Identifier);
            if (balance == null)
            {
                throw new InvalidOperationException("Wallet is not synced to the blockchain.");
            }
            return (ulong)balance;
        }
        public Transaction GenerateTransaction(WalletId receiver, UInt64 amount)
        {
            if (GetBalance() < amount)
            {
                throw new InvalidOperationException("Insufficient funds.");
            }
            Transaction? transaction = privateKey.SignTransaction(Identifier, receiver, amount, DateTime.Now);
            if (transaction == null)
                throw new InvalidOperationException("Could not sign the transaction.");
            return transaction;
        }

        /*public static Wallet CreateUniqueWallet(Blockchain blockchain)
        {
            
        }*/
    }
}
