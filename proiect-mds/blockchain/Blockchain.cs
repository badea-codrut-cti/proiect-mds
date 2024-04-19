using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace proiect_mds.blockchain
{
    internal abstract class BlockIterator : IEnumerator<Block>
    {
        protected BlockIterator() { }
        public abstract bool MoveNext();
        public abstract void Reset();
        public abstract void Dispose();
        public abstract Block Current { get; }
        object IEnumerator.Current => Current;
        public abstract bool AddBlock(Block block);
    }

    internal abstract class WalletIterator : IEnumerator<PublicWallet>
    {
        protected WalletIterator() { }
        public abstract bool MoveNext();
        public abstract void Reset();
        public abstract void Dispose();
        public abstract PublicWallet Current { get; }
        object IEnumerator.Current => Current;
        public abstract bool AddWallet(PublicWallet pWallet);
    }

    internal class Blockchain
    {
        private readonly BlockIterator blockIterator;
        private readonly WalletIterator walletIterator;
        public Blockchain(BlockIterator blockIterator, WalletIterator walletIterator)
        {
            this.blockIterator = blockIterator;
            this.walletIterator = walletIterator;
        }
        public UInt64? GetWalletBalance(WalletId walletId)
        {
            UInt64 received = 0;
            UInt64 sent = 0;
            bool wasFound = false;
            blockIterator.Reset();
            while (blockIterator.MoveNext())
            {
                Block block = blockIterator.Current;

                foreach (Transaction transaction in block.Transactions)
                {
                    if (transaction.Sender == walletId)
                        sent += transaction.Amount;
                    else if (transaction.Receiver == walletId)
                        received += transaction.Amount;

                    if (transaction.Sender == walletId ||  transaction.Receiver == walletId)
                        wasFound = true;
                }
            }

            if (!wasFound)
                return null;

            if (received < sent)
                throw new InvalidDataException("Wallets cannot have negative balance. Maybe the blockchain is out of sync.");

            return received - sent;
        }
        public bool AddBlock(Block block)
        {
            if (IsGenesisBlock(block))
            {
                return blockIterator.AddBlock(block);
            }

            var previousBlock = GetBlock(block.Index - 1);
            if (previousBlock == null || Hash.FromBlock(previousBlock) != block.PreviousHash)
                return false;

            return blockIterator.AddBlock(previousBlock);
        }
        public PublicKey? GetKeyFromWalletId(WalletId walletId)
        {
            walletIterator.Reset();
            do
            {
                try
                {
                    if (walletIterator.Current.Identifier == walletId)
                        return walletIterator.Current.PublicKey;
                } catch (InvalidOperationException)
                {
                    return null;
                }
            } while (walletIterator.MoveNext());

            return null;
        }
        public Block GetLatestBlock()
        {
            blockIterator.Reset();
            Block ret = blockIterator.Current;
            while (blockIterator.MoveNext())
            {
                Block block = blockIterator.Current;
                if (block.Index > ret.Index)
                    ret = block;
            }
            return ret;
        }
        public Block? GetBlock(UInt64 index)
        {
            blockIterator.Reset();
            Block block = blockIterator.Current;
            do
            {
                if (block.Index == index)
                    return block;
                block = blockIterator.Current;
            } while (blockIterator.MoveNext());
            return null;
        }
        public bool RegisterWallet(PublicWallet wallet)
        {
            if (GetKeyFromWalletId(wallet.Identifier) != null)
                return false;
            return walletIterator.AddWallet(wallet);
        }
        public static bool IsGenesisBlock(Block block)
        {
            return block.Index == 0;
        }
    }
}
