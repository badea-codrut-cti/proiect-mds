using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Crypto;
using ProtoBuf;
using System.Text;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.OpenSsl;
using System.Globalization;
using Org.BouncyCastle.Crypto.Digests;
using proiect_mds.blockchain.exception;

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
        private readonly ECPrivateKeyParameters privateKeyParams; 

        public PrivateKey(string data)
        {
            try
            {
                var rdr = new PemReader(new StringReader(data));
                AsymmetricCipherKeyPair keyPair = (AsymmetricCipherKeyPair)rdr.ReadObject();
                privateKeyParams = (ECPrivateKeyParameters)keyPair.Private;
            } catch(FormatException) {
                throw new PrivateKeyException("Improperly formatted private key");
            }
        }

        public Transaction? SignTransaction(WalletId sender, WalletId receiver, ulong amount, DateTime timestamp)
        {
            string formattedTimestamp = timestamp.ToString("yyyyMMddHHmmssfff", CultureInfo.InvariantCulture);
            byte[] transactionData = Encoding.UTF8.GetBytes($"{sender}{receiver}{amount}{formattedTimestamp}");
            var signer = new ECDsaSigner(new HMacDsaKCalculator(new Sha256Digest()));
            signer.Init(true, privateKeyParams);
            BigInteger[] signature = signer.GenerateSignature(transactionData);
            var rBytes = signature[0].ToByteArrayUnsigned();
            var sBytes = signature[1].ToByteArrayUnsigned();
            byte[] signatureBytes = new byte[Transaction.SIGNATURE_LENGTH];
            Buffer.BlockCopy(rBytes, 0, signatureBytes, Transaction.SIGNATURE_LENGTH / 2 - rBytes.Length, rBytes.Length);
            Buffer.BlockCopy(sBytes, 0, signatureBytes, Transaction.SIGNATURE_LENGTH - sBytes.Length, sBytes.Length);
            return new Transaction(sender, receiver, amount, signatureBytes, timestamp);
        }
    }

    [ProtoContract]
    internal class PublicKey
    {
        [ProtoMember(1)]
        private readonly string pemString;

        public PublicKey(string pKey)
        {
            this.pemString = pKey;
        }
        public bool ValidateTransaction(Transaction transaction)
        {
            var rdr = new PemReader(new StringReader(pemString));
            var publicKeyParams = (ECPublicKeyParameters)rdr.ReadObject();

            var signer = new ECDsaSigner();
            signer.Init(false, publicKeyParams);
            string formattedTimestamp = transaction.Timestamp.ToString("yyyyMMddHHmmssfff", CultureInfo.InvariantCulture);
            byte[] transactionData = Encoding.UTF8.GetBytes($"{transaction.Sender}{transaction.Receiver}{transaction.Amount}{formattedTimestamp}");
            
            byte[] rBytes = new byte[Transaction.SIGNATURE_LENGTH / 2];
            Buffer.BlockCopy(transaction.Signature, 0, rBytes, 0, rBytes.Length);
            var r = new BigInteger(rBytes); 

            byte[] sBytes = new byte[Transaction.SIGNATURE_LENGTH / 2];
            Buffer.BlockCopy(transaction.Signature, Transaction.SIGNATURE_LENGTH / 2, sBytes, 0, sBytes.Length);
            var s = new BigInteger(sBytes);

            return signer.VerifySignature(transactionData, r, s);
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
