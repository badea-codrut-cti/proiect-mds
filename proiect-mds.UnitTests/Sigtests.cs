using proiect_mds.blockchain;
using System.Text;

namespace proiect_mds.UnitTests
{
    [TestClass]
    public class KeyBasedSignChecks
    {
        [TestMethod]
        public void SignTransaction()
        {
            var sender = new WalletId(Encoding.UTF8.GetBytes("0123456789abcdef"));
            var receiver = new WalletId(Encoding.UTF8.GetBytes("0123456789ABCDEF"));
            byte[] privateKey = Encoding.UTF8.GetBytes("MIHcAgEBBEIBVXuRAbqUrdyhqproIUtLNRTq2r2H4S9rv2WUPrvZYwAKLkgj+QxksOWXgvgkKKdEwHRkAWGTMvKbciMO/P/W4WqgBwYFK4EEACOhgYkDgYYABACoPhuCMVs8/IDPGG5gwswb23J1Q+fkYgl14spC3SD4VJ4y3rMf8pzU06za4nzYd1U+QdS368MP56tEXGWHUD4sKwG+8VanlwvHzCzod8qpXREZtjJ+2/kBjMuU1DtdyGKYpRZ+wX1KwFT9GwFLPLET75E02PDgVT0I8+FIVZLjE6SAKQ==");
            var key = new PrivateKey(privateKey);
            var transaction = key.SignTransaction(sender, receiver, 100, new DateTime(0xFFEEDDCCAA01));
            Assert.IsTrue(true);
        }
    }
}