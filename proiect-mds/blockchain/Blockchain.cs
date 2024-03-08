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

    internal class Blockchain
    {
        private readonly BlockIterator blockIterator;
        public Blockchain(BlockIterator blockIterator)
        {
            this.blockIterator = blockIterator;
        }

        public UInt64 GetWalletBalance(WalletId walletId)
        {
            UInt64 received = 0;
            UInt64 sent = 0;
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
                }
            }

            if (received < sent)
                throw new InvalidDataException("Wallets cannot have negative balance. Maybe the blockchain is out of sync.");

            return received - sent;
        }
    }
}
