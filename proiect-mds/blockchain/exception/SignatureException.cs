using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace proiect_mds.blockchain.exception
{
    internal class SignatureException : Exception
    {
        public SignatureException(string message) : base(message) { }
    }

    internal class PublicKeyException : SignatureException
    {
        public PublicKeyException(string message) : base(message) { }
    }

    internal class PrivateKeyException : SignatureException
    {
        public PrivateKeyException(string message) : base(message) { }
    }
}
