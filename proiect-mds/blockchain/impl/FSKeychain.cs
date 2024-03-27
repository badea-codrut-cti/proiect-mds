using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace proiect_mds.blockchain.impl
{
    internal class FSKeychain : FSIteratorUtil<PublicWallet>
    {
        public FSKeychain(Stream stream, int maxCache) : base(stream, maxCache) { }
        protected override PublicWallet? ReadFromStream()
        {
            try
            {
                using var reader = new BinaryReader(objectStream);
                var walletId = reader.ReadBytes(WalletId.WID_LENGTH);
                string parsedKey = Encoding.UTF8.GetString(reader.ReadBytes(PublicKey.PUBLIC_KEY_LENGTH));
                return new PublicWallet(new WalletId(walletId), new PublicKey(parsedKey));
            }
            catch (IOException)
            {
                return null;
            }
        }
        protected override bool WriteToStream(PublicWallet obj)
        {
            objectStream.Write(obj.Identifier.ToBytes());
            //objectStream.Write(obj.PublicKey.);
            return true;
        }
    }
}
