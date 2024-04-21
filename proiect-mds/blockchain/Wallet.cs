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
using System.Transactions;
using proiect_mds.daemon.packets;
using System.Reflection;

namespace proiect_mds.blockchain
{
    [ProtoContract]
    internal class WalletId
    {
        public static string WID_PREFIX = "+codrea";
        public static UInt16 WID_LENGTH = 16;
        [ProtoMember(1)]
        public string Value { get; private set; }

        public WalletId(string data)
        {
            if (data.Length != WID_LENGTH)
            {
                throw new ArgumentException($"Invalid data length: {data.Length}. Expected {WID_LENGTH} bytes.");
            }

            Value = data;
        }
        public WalletId(byte[] data)
        {
            if (data.Length != WID_LENGTH)
            {
                throw new ArgumentException($"Invalid data length: {data.Length}. Expected {WID_LENGTH} bytes.");
            }

            Value = Encoding.ASCII.GetString(data);
        }
        public WalletId() { }
        public override string ToString()
        {
            return WID_PREFIX + Value;
        }
        public static WalletId MasterWalletId()
        {
            return new WalletId("0000000000000001");
        }
        public static bool operator ==(WalletId a, WalletId b)
        {
            return a.Value == b.Value;
        }
        public static bool operator !=(WalletId a, WalletId b)
        {
            return !(a == b);
        }
    }

    internal class PrivateKey
    {
        private readonly ECPrivateKeyParameters privateKeyParams;
        private readonly ECPublicKeyParameters publicKeyParams;
        public static int PRIVATE_KEY_LENGTH = 160;

        public PrivateKey(string PemString)
        {
            try
            {
                if (PemString.Length != PRIVATE_KEY_LENGTH)
                    throw new PrivateKeyException("Invalid private key length.");

                var rdr = new PemReader(new StringReader(
                   "-----BEGIN EC PRIVATE KEY-----" +
                   PemString +
                   "-----END EC PRIVATE KEY-----"
                ));
                AsymmetricCipherKeyPair keyPair = (AsymmetricCipherKeyPair)rdr.ReadObject();
                privateKeyParams = (ECPrivateKeyParameters)keyPair.Private;
                publicKeyParams = (ECPublicKeyParameters)keyPair.Public;
            } catch(FormatException) {
                throw new PrivateKeyException("Improperly formatted private key.");
            }
        }

        public Transaction? SignTransaction(WalletId sender, WalletId receiver, ulong amount, DateTime timestamp)
        {
            string formattedTimestamp = timestamp.ToString("yyyyMMddHHmmssfff", CultureInfo.InvariantCulture);
            byte[] transactionData = Encoding.UTF8.GetBytes($"{sender.Value}{receiver.Value}{amount}{formattedTimestamp}");
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

        public BecomeValidatorPacket? SignValidatorPacket(WalletId wId, uint stake, DateTime timestamp)
        {
            string formattedTimestamp = timestamp.ToString("yyyyMMddHHmmssfff", CultureInfo.InvariantCulture);
            byte[] electData = Encoding.UTF8.GetBytes($"{wId.Value}{stake}{formattedTimestamp}");
            var signer = new ECDsaSigner(new HMacDsaKCalculator(new Sha256Digest()));
            signer.Init(true, privateKeyParams);
            BigInteger[] signature = signer.GenerateSignature(electData);
            var rBytes = signature[0].ToByteArrayUnsigned();
            var sBytes = signature[1].ToByteArrayUnsigned();
            byte[] signatureBytes = new byte[Transaction.SIGNATURE_LENGTH];
            Buffer.BlockCopy(rBytes, 0, signatureBytes, Transaction.SIGNATURE_LENGTH / 2 - rBytes.Length, rBytes.Length);
            Buffer.BlockCopy(sBytes, 0, signatureBytes, Transaction.SIGNATURE_LENGTH - sBytes.Length, sBytes.Length);
            return new BecomeValidatorPacket(wId, stake, timestamp, signatureBytes);
        }

        public PublicKey ToPublicKey()
        {
            var sWriter = new StringWriter();
            var pemWriter = new PemWriter(sWriter);
            pemWriter.WriteObject(publicKeyParams);
            var str = sWriter.ToString();
            str = str.Replace("-----BEGIN PUBLIC KEY-----", "").Replace("-----END PUBLIC KEY-----", "").Replace("\r\n", "");
            return new PublicKey(str);
        }
    }

    [ProtoContract]
    internal class PublicKey
    {
        public static int PUBLIC_KEY_LENGTH = 120;

        [ProtoMember(1)]
        public string PemString { get; private set; }

        public PublicKey(string pKey)
        {
            if (pKey.Length != PUBLIC_KEY_LENGTH)
            {
                throw new PublicKeyException("Invalid public key length.");
            }
            this.PemString = pKey;
        }
        public PublicKey() { }
        public bool ValidateTransaction(Transaction transaction)
        {
            var rdr = new PemReader(new StringReader(
                "-----BEGIN PUBLIC KEY-----" +
                PemString +
                "-----END PUBLIC KEY-----"
             ));
            var publicKeyParams = (ECPublicKeyParameters)rdr.ReadObject();

            var signer = new ECDsaSigner();
            signer.Init(false, publicKeyParams);
            string formattedTimestamp = transaction.Timestamp.ToString("yyyyMMddHHmmssfff", CultureInfo.InvariantCulture);
            byte[] transactionData = Encoding.UTF8.GetBytes($"{transaction.Sender.Value}{transaction.Receiver.Value}{transaction.Amount}{formattedTimestamp}");
            
            byte[] rBytes = new byte[Transaction.SIGNATURE_LENGTH / 2];
            Buffer.BlockCopy(transaction.Signature, 0, rBytes, 0, rBytes.Length);
            var r = new BigInteger(rBytes); 

            byte[] sBytes = new byte[Transaction.SIGNATURE_LENGTH / 2];
            Buffer.BlockCopy(transaction.Signature, Transaction.SIGNATURE_LENGTH / 2, sBytes, 0, sBytes.Length);
            var s = new BigInteger(sBytes);

            return signer.VerifySignature(transactionData, r, s);
        }
        public bool ValidateElectionSignature(WalletId wId, DateTime timestamp, uint stake, byte[] signature)
        {
            
            if (signature.Length != Transaction.SIGNATURE_LENGTH)
                return false;
            var rdr = new PemReader(new StringReader(
                "-----BEGIN PUBLIC KEY-----" +
                PemString +
                "-----END PUBLIC KEY-----"
             ));
            var publicKeyParams = (ECPublicKeyParameters)rdr.ReadObject();

            var signer = new ECDsaSigner();
            signer.Init(false, publicKeyParams);
            string formattedTimestamp = timestamp.ToString("yyyyMMddHHmmssfff", CultureInfo.InvariantCulture);
            byte[] packetData = Encoding.UTF8.GetBytes($"{wId.Value}{stake}{formattedTimestamp}");

            byte[] rBytes = new byte[Transaction.SIGNATURE_LENGTH / 2];
            Buffer.BlockCopy(signature, 0, rBytes, 0, rBytes.Length);
            var r = new BigInteger((new byte[] { 0 }).Concat(rBytes).ToArray());

            byte[] sBytes = new byte[Transaction.SIGNATURE_LENGTH / 2];
            Buffer.BlockCopy(signature, Transaction.SIGNATURE_LENGTH / 2, sBytes, 0, sBytes.Length);
            var s = new BigInteger((new byte[] { 0 }).Concat(sBytes).ToArray());
            return signer.VerifySignature(packetData, r, s);
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
        public PublicWallet() { }
        public static PublicWallet MasterWallet()
        {
            var pKey = "MFYwEAYHKoZIzj0CAQYFK4EEAAoDQgAEldg0iRPZCBeNLb3AeCE0JfLghwkwItRG/+U3uwYLicULXEDz9hjG9tFR52fxsseF/z41cwvCS14tBk+pK/CmfQ==";
            return new PublicWallet(WalletId.MasterWalletId(), new PublicKey(pKey));
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
            ulong? balance = blockchain.GetWalletBalance(Identifier);
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
        public PublicWallet ToPublicWallet()
        {
            return new PublicWallet(Identifier, privateKey.ToPublicKey());
        }
        public static Wallet CreateUniqueWallet(Blockchain blockchain, PrivateKey privateKey)
        {
            var random = new SecureRandom();
            byte[] randomBytes = new byte[WalletId.WID_LENGTH];
            random.NextBytes(randomBytes);

            while (blockchain.GetKeyFromWalletId(new WalletId(randomBytes)) != null)
                random.NextBytes(randomBytes); 

            return new Wallet(blockchain, new WalletId(randomBytes), privateKey);
        }
    }
}
