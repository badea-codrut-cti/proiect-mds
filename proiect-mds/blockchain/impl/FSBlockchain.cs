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

    internal class FSBlockIterator : BlockIterator
    {
        private readonly Stream stream;
        private readonly int maxCache;
        private List<Block> readCache;
        private List<Block> writeCache;
        private int readCacheIndex;
        private int writeCacheIndex;
        private bool endOfStreamReached;
        private bool disposed = false;
        private Block currentBlock;

        public FSBlockIterator(Stream stream, int maxCache = 100)
        {
            ArgumentNullException.ThrowIfNull(stream);
            if (maxCache <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxCache), "maxCache must be positive.");
            }

            this.stream = stream;
            this.maxCache = maxCache;
            this.readCache = new List<Block>(maxCache);
            this.writeCache = new List<Block>(maxCache);
            this.readCacheIndex = 0;
            this.writeCacheIndex = 0;
            this.endOfStreamReached = false;

            FillReadCache();

            if (readCache.Count == 0)
            {
                throw new InvalidOperationException("No blocks found in the file.");
            }

            currentBlock = readCache[0];
        }
        public override bool MoveNext()
        {
            Block? bl = readCache.Find(block => block.Index == currentBlock.Index + 1);

            if (bl != null)
            {
                currentBlock = bl;
                return true;
            }

            bl = writeCache.Find(block => block.Index == currentBlock.Index + 1);

            if (bl != null)
            {
                currentBlock = bl;
                return true;
            }

            ulong? index;

            while ((index = GetSeekBlockIndex()) != null
                && index != currentBlock.Index + 1)
            {
                SkipBlock();
            }

            return false;
        }
        private void FillReadCache()
        {
            while (readCache.Count < maxCache && !endOfStreamReached)
            {
                Block? block = ReadBlock();
                if (block != null)
                {
                    readCache.Add(block);
                }
                else
                {
                    endOfStreamReached = true;
                }
            }
        }
        private bool IsGenesisBlock(Block block)
        {
            return block.Index == 0;
        }
        public override void Reset()
        {
            endOfStreamReached = false;
            readCache.Clear();
            stream.Seek(0, SeekOrigin.Begin);
            FillReadCache();
        }
        public override void Dispose()
        {
            if (!disposed)
            {
                WriteCacheToDisk();
                stream.Dispose();
                disposed = true;
            }
        }

        public override Block Current
        {
            get
            {
                if (currentBlock == null)
                {
                    throw new InvalidOperationException("No current block.");
                }
                return currentBlock;
            }
        }

        public override bool AddBlock(Block block)
        {
            writeCache.Add(block);
            if (writeCache.Count > maxCache)
            {
                WriteCacheToDisk();
            }
            return true;
        }

        private Block? ReadBlock()
        {
            try
            {
                BinaryReader reader = new BinaryReader(stream);
                ulong index = reader.ReadUInt64();
                DateTime timestamp = new DateTime((long)reader.ReadUInt64());
                Hash prevHash = new Hash(reader.ReadBytes((int)Hash.HASH_LENGTH));
                WalletId validatortId = new WalletId(reader.ReadBytes(WalletId.WID_LENGTH));
                byte tCount = reader.ReadByte();
                List<Transaction> transactions = new List<Transaction>();
                for (byte i = 0; i < tCount; i++)
                {
                    WalletId senderId = new WalletId(reader.ReadBytes(WalletId.WID_LENGTH));
                    WalletId receiverId = new WalletId(reader.ReadBytes(WalletId.WID_LENGTH));
                    ulong amount = reader.ReadUInt64();
                    byte[] signature = reader.ReadBytes((int)Transaction.SIGNATURE_LENGTH);
                    Transaction transaction = new Transaction(senderId, receiverId, amount, signature);
                    transactions.Add(transaction);
                }
                return new Block(index, timestamp, prevHash, validatortId, transactions);
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
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    stream.Seek(sizeof(ulong) * 2 + Hash.HASH_LENGTH + WalletId.WID_LENGTH, SeekOrigin.Current);
                    byte tCount = reader.ReadByte();
                    stream.Seek(
                        tCount * (WalletId.WID_LENGTH * 2 + sizeof(ulong) + Transaction.SIGNATURE_LENGTH),
                        SeekOrigin.Current
                    );
                    return true;
                }
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
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    ulong index = reader.ReadUInt64();
                    stream.Seek(-sizeof(ulong), SeekOrigin.Current);
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

        }
        private bool WriteBlockToStream(Block block)
        {
            
        }
    }

    internal class FSBlockchain
    {
    }
}
