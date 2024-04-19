using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace proiect_mds.blockchain.impl
{
    internal class FSKeychain : WalletIterator
    {
        private List<PublicWallet> writecache;
        private Stream stream;
        private PublicWallet? currentWallet;
        public FSKeychain(Stream stream, int maxCache = 100)
        {
            writecache = new List<PublicWallet>(maxCache);
            this.stream = stream;
            currentWallet = ReadKeychain();
        }
        public override PublicWallet Current { 
            get {
                if (currentWallet == null)
                {
                    throw new InvalidOperationException("No current wallet keychain.");
                }
                return currentWallet;
            }
        }
        public override bool AddWallet(PublicWallet pWallet)
        {
            writecache.Add(pWallet);
            if (writecache.Count >= writecache.Capacity)
            {
                WriteCacheToStream();
            }
            return true;
        }

        public override void Dispose()
        {
            WriteCacheToStream();
        }
        public override bool MoveNext()
        {
            currentWallet = ReadKeychain();
            return currentWallet != null;
        }
        public override void Reset()
        {
            WriteCacheToStream();
            stream.Seek(0, SeekOrigin.Begin);
        }
        private PublicWallet? ReadKeychain()
        {
            try
            {
                BinaryReader reader = new BinaryReader(stream);
                var wallet = new WalletId(reader.ReadBytes(WalletId.WID_LENGTH));
                var publicKey = new PublicKey(Encoding.UTF8.GetString(reader.ReadBytes(PublicKey.PUBLIC_KEY_LENGTH)));
                return new PublicWallet(wallet, publicKey);
            } catch(IOException)
            {
                return null;
            }
        }
        private bool MakeRoomForKeychain()
        {
            long currentLength = stream.Length;
            long offset = currentLength + WalletId.WID_LENGTH + PublicKey.PUBLIC_KEY_LENGTH;
            stream.Seek(0, SeekOrigin.End);
            stream.SetLength(offset);
            return true;
        }
        private void WriteCacheToStream()
        {
            foreach (var item in writecache)
            {
                WriteWallet(item);
            }
        }
        private bool WriteWallet(PublicWallet pWallet)
        {
            MakeRoomForKeychain();
            stream.Write(pWallet.Identifier.Value);
            stream.Write(Encoding.UTF8.GetBytes(pWallet.PublicKey.PemString));
            return true;
        }
    }
}
