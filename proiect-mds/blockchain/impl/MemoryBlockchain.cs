using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace proiect_mds.blockchain.impl
{
    internal class MemoryBlockIterator : BlockIterator
    {
        private readonly List<Block> Blocks = [];
        private Int32 cIndex = 0;
        public MemoryBlockIterator()
        {
        }
        public override bool MoveNext()
        {
            cIndex++;
            return cIndex < Blocks.Count;
        }
        public override void Reset()
        {
            cIndex = 0;
        }
        public override void Dispose() { } 
        public override Block Current
        {
            get
            {
                if (cIndex >= Blocks.Count)
                {
                    throw new InvalidOperationException("Reached end of blockchain.");
                }
                return Blocks[cIndex];
            }
        }
        public override bool AddBlock(Block block)
        {
            Blocks.Add(block);
            return true;
        }
    }

    internal class MemoryWalletIterator : WalletIterator
    {
        private readonly List<PublicWallet> KeyChain = [];
        private Int32 cIndex = 0;
        public MemoryWalletIterator()
        {
        }
        public override bool MoveNext()
        {
            cIndex++;
            return cIndex < KeyChain.Count;
        }
        public override void Reset()
        {
            cIndex = 0;
        }
        public override void Dispose() { } 
        public override PublicWallet Current
        {
            get
            {
                if (cIndex >= KeyChain.Count)
                {
                    throw new InvalidOperationException("Iterator is positioned before the first element or after the last element.");
                }
                return KeyChain[cIndex];
            }
        }
        public override bool AddWallet(PublicWallet pWallet)
        { 
            KeyChain.Add(pWallet);
            return true;
        }
    }
}
