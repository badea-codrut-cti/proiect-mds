using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace proiect_mds.blockchain.exception
{
    internal class BlockException : Exception
    {
        public BlockException(string message) : base(message) { }
    }
    internal class TransactionException : BlockException
    {
        public TransactionException(string message) : base(message) { }
    }
}
