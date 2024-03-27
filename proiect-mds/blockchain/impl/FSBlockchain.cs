using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

namespace proiect_mds.blockchain.impl
{

    internal class FSBlockIterator : FSIteratorUtil<Block>
    {
        private static readonly Int64 TRANSACTION_LENGTH = sizeof(ulong) + WalletId.WID_LENGTH * 2 + Transaction.SIGNATURE_LENGTH;
        private static readonly Int64 BLOCK_HEADER_LENGTH = sizeof(ulong) * 2 + Hash.HASH_LENGTH + WalletId.WID_LENGTH;

        public FSBlockIterator(Stream stream, int maxCache = 100) : base(stream, maxCache) { }

        public bool MoveNext()
        {
            Block? bl = readCache.Find(block => block.Index == currentObject.Index + 1);

            if (bl != null)
            {
                currentObject = bl;
                return true;
            }

            bl = writeCache.Find(block => block.Index == currentObject.Index + 1);

            if (bl != null)
            {
                currentObject = bl;
                return true;
            }

            ulong? index;

            while ((index = GetSeekBlockIndex()) != null
                && index != currentObject.Index + 1)
            {
                SkipBlock();
            }

            return false;
        }
        private bool IsGenesisBlock(Block block)
        {
            return block.Index == 0;
        }

        protected override Block? ReadFromStream()
        {
            try
            {
                BinaryReader reader = new BinaryReader(objectStream);
                ulong index = reader.ReadUInt64();
                DateTime blockTimestamp = new DateTime(reader.ReadInt64());
                Hash prevHash = new Hash(reader.ReadBytes((int)Hash.HASH_LENGTH));
                WalletId validatortId = new WalletId(reader.ReadBytes(WalletId.WID_LENGTH));
                byte tCount = reader.ReadByte();
                List<Transaction> transactions = new List<Transaction>();
                for (byte i = 0; i < tCount; i++)
                {
                    WalletId senderId = new WalletId(reader.ReadBytes(WalletId.WID_LENGTH));
                    WalletId receiverId = new WalletId(reader.ReadBytes(WalletId.WID_LENGTH));
                    DateTime timestamp = new DateTime(reader.ReadInt64());
                    ulong amount = reader.ReadUInt64();
                    byte[] signature = reader.ReadBytes((int)Transaction.SIGNATURE_LENGTH);
                    Transaction transaction = new Transaction(senderId, receiverId, amount, signature, timestamp);
                    transactions.Add(transaction);
                }
                return new Block(index, blockTimestamp, prevHash, validatortId, transactions); ;
            }
            catch (IOException)
            {
                return null;
            }
        }
        private bool SkipBlock()
        {
            try
            {
                objectStream.Seek(BLOCK_HEADER_LENGTH, SeekOrigin.Current);
                int tCount = objectStream.ReadByte();
                if (tCount == -1)
                    return false;
                objectStream.Seek(tCount * TRANSACTION_LENGTH, SeekOrigin.Current);
                return true;
            }
            catch (IOException)
            {
                endOfStreamReached = true;
                return false;
            }
        }
        private ulong? GetSeekBlockIndex()
        {
            try
            {
                using (BinaryReader reader = new BinaryReader(objectStream))
                {
                    ulong index = reader.ReadUInt64();
                    objectStream.Seek(-sizeof(ulong), SeekOrigin.Current);
                    return index;
                }
            }
            catch (IOException)
            {
                endOfStreamReached = true;
                return null;
            }
        }
        private bool MakeRoomForBlock(Block block)
        {
            long currentLength = objectStream.Length;
            long offset = BLOCK_HEADER_LENGTH + block.Transactions.Count * TRANSACTION_LENGTH;
            long pos = objectStream.Position;
            objectStream.SetLength(currentLength + offset);
            objectStream.Seek(-offset, SeekOrigin.End);

            return true;
        }
        protected override bool WriteToStream(Block block)
        {
            ulong? pIndex;
            while ((pIndex = GetSeekBlockIndex()) != null &&
                pIndex < block.Index)
                SkipBlock();
            if (pIndex == block.Index)
                return true;
            MakeRoomForBlock(block);
            return true;
        }
    }
}
