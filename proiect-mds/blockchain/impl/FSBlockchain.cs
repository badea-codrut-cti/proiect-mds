using proiect_mds.blockchain.exception;
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
        private static readonly Int64 TRANSACTION_LENGTH = sizeof(ulong) + WalletId.WID_LENGTH * 2 + Transaction.SIGNATURE_LENGTH;
        private static readonly Int64 BLOCK_HEADER_LENGTH = sizeof(ulong) * 2 + Hash.HASH_LENGTH + WalletId.WID_LENGTH;

        private Stream blockStream;
        private List<Block> writeCache;
        private bool disposed = false;
        private bool endOfStreamReached = false;
        private Block currentBlock;

        public FSBlockIterator(Stream stream, int maxCache = 100)
        {
            ArgumentNullException.ThrowIfNull(stream);
            if (maxCache <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxCache), "maxCache must be positive.");
            }

            this.blockStream = stream;
            this.writeCache = new List<Block>(maxCache);

            var cBlock = ReadBlock();
            if (cBlock == null || !Blockchain.IsGenesisBlock(cBlock)) 
            {
                throw new ArgumentException("Poorly formatted stream.");
            }

            currentBlock = cBlock;
        }
        public override bool MoveNext()
        {
            var nBlock = GetNextBlock();
            if (nBlock == null)
            {
                nBlock = ReadBlock();
                if (nBlock == null)
                    return false;
            }
            currentBlock = nBlock;
            return true;
        }
        private Block? GetNextBlock()
        {
            var previousPos = blockStream.Position;
            var lastBlockhash = Hash.FromBlock(currentBlock);

            var fBlock = writeCache.Find(block => block.Index == currentBlock.Index + 1);

            if (fBlock != null)
            {
                return fBlock;
            }

            ulong? index;

            while ((index = GetSeekBlockIndex()) != null
                && index != currentBlock.Index + 1)
            {
                SkipBlock();
            }

            if (index == currentBlock.Index + 1)
            {
                fBlock = ReadBlock();
            }
            blockStream.Seek(previousPos, SeekOrigin.Begin);
            return fBlock;
        }
        public override void Reset()
        {
            endOfStreamReached = false;
            blockStream.Seek(0, SeekOrigin.Begin);
        }
        public override void Dispose()
        {
            if (!disposed)
            {
                WriteCacheToStream();
                blockStream.Dispose();
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
            if (writeCache.Count >= writeCache.Capacity)
            {
                WriteCacheToStream();
            }
            return true;
        }

        private Block? ReadBlock()
        {
            try
            {
                BinaryReader reader = new BinaryReader(blockStream);
                ulong index = reader.ReadUInt64();
                DateTime blockTimestamp = new DateTime(reader.ReadInt64());
                Hash? prevHash = index > 0 ? new Hash(reader.ReadBytes((int)Hash.HASH_LENGTH)) : null;
                WalletId validatorId = new WalletId(reader.ReadBytes(WalletId.WID_LENGTH));
                uint tCount = reader.ReadUInt32();
                List<Transaction> transactions = new List<Transaction>();
                for (uint i = 0; i < tCount; i++)
                {
                    WalletId senderId = new WalletId(reader.ReadBytes(WalletId.WID_LENGTH));
                    WalletId receiverId = new WalletId(reader.ReadBytes(WalletId.WID_LENGTH));
                    DateTime timestamp = new DateTime(reader.ReadInt64());
                    ulong amount = reader.ReadUInt64();
                    byte[] signature = reader.ReadBytes((int)Transaction.SIGNATURE_LENGTH);
                    Transaction transaction = new Transaction(senderId, receiverId, amount, signature, timestamp);
                    transactions.Add(transaction);
                }
                return new Block(index, blockTimestamp, prevHash, validatorId, transactions); ;
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
                blockStream.Seek(BLOCK_HEADER_LENGTH, SeekOrigin.Current);
                int tCount = blockStream.ReadByte();
                if (tCount == -1)
                    return false;
                blockStream.Seek(tCount * TRANSACTION_LENGTH, SeekOrigin.Current);
                return true;
            }
            catch (IOException)
            {
                endOfStreamReached = true;
                return false;
            }
        }
        private void WriteCacheToStream()
        {
            writeCache.Sort((a, b) => (int)a.Index - (int)b.Index);
            foreach (var item in writeCache)
            {
                WriteBlockToStream(item);
            }
        }
        private ulong? GetSeekBlockIndex()
        {
            try
            {
                using BinaryReader reader = new BinaryReader(blockStream);
                ulong index = reader.ReadUInt64();
                blockStream.Seek(-sizeof(ulong), SeekOrigin.Current);
                return index;
            }
            catch (IOException)
            {
                endOfStreamReached = true;
                return null;
            }
        }
        private bool MakeRoomForBlock(Block block)
        {
            long currentLength = blockStream.Length;
            long offset = currentLength + BLOCK_HEADER_LENGTH + block.Transactions.Count * TRANSACTION_LENGTH;
            blockStream.Seek(0, SeekOrigin.End);
            blockStream.SetLength(offset);
            return true;
        }
        private bool WriteBlockToStream(Block block)
        {   
            MakeRoomForBlock(block);
            blockStream.Write(BitConverter.GetBytes(block.Index).Reverse().ToArray());
            blockStream.Write(BitConverter.GetBytes(block.Timestamp.Ticks).Reverse().ToArray());
            if (block.PreviousHash != null)
            {
                blockStream.Write(block.PreviousHash.Value);
            }
            blockStream.Write(block.ValidatorId.Value);
            blockStream.Write(BitConverter.GetBytes(block.Transactions.Count).Reverse().ToArray());
            return true;
        }
    }
}
